using Dapper;

namespace TechPos.Sync.Core;

public sealed record TablePolicy(string TableName, string GroupName, int IntervalSec, bool Enabled, int BatchSize);

public sealed record SyncCursor(string TableName, DateTime? LastModified, string? LastId, DateTime? LastSuccessAt, int LastCount);

public sealed record SyncRunResult(
    string TableName, int RowsRead, int RowsUpserted, int RowsSkipped, int Errors, TimeSpan Duration, string? Message = null);

/// <summary>Metadata sync MySQL (W_SYNC_CURSOR, W_SYNC_LOG, W_SYNC_ROW_ERROR, W_SYNC_HEARTBEAT).</summary>
public sealed class SyncMetaRepository
{
    private readonly MySqlConnectionFactoryLike _factory;

    /// <summary>Abstraction tối giản để Sync.Core không phụ thuộc trực tiếp Data.MySql runtime.</summary>
    public interface MySqlConnectionFactoryLike
    {
        Task<MySqlConnector.MySqlConnection> OpenAsync(CancellationToken ct = default);
    }

    public SyncMetaRepository(MySqlConnectionFactoryLike factory) => _factory = factory;

    public async Task<List<TablePolicy>> GetPoliciesAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<TablePolicy>(
            "SELECT TABLE_NAME as TableName, GROUP_NAME as GroupName, INTERVAL_SEC as IntervalSec, " +
            "ENABLED as Enabled, BATCH_SIZE as BatchSize FROM W_SYNC_TABLE_POLICY");
        return rows.ToList();
    }

    public async Task<SyncCursor?> GetCursorAsync(string table, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<SyncCursor>(
            "SELECT TABLE_NAME as TableName, LAST_MODIFIED as LastModified, LAST_ID as LastId, " +
            "LAST_SUCCESS_AT as LastSuccessAt, LAST_COUNT as LastCount FROM W_SYNC_CURSOR WHERE TABLE_NAME = @t",
            new { t = table });
    }

    public async Task SaveCursorAsync(string table, DateTime? lastModified, string? lastId, int count, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("""
            INSERT INTO W_SYNC_CURSOR (TABLE_NAME, LAST_MODIFIED, LAST_ID, LAST_SUCCESS_AT, LAST_COUNT)
            VALUES (@t, @lm, @lid, @now, @c)
            ON DUPLICATE KEY UPDATE LAST_MODIFIED = @lm, LAST_ID = @lid,
              LAST_SUCCESS_AT = @now, LAST_COUNT = @c
            """, new { t = table, lm = lastModified, lid = lastId, now = DateTime.UtcNow, c = count });
        await Task.CompletedTask;
    }

    public async Task LogRunAsync(SyncRunResult r, string runType, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("""
            INSERT INTO W_SYNC_LOG (ID, TABLE_NAME, RUN_TYPE, ROWS_READ, ROWS_UPSERTED, ROWS_SKIPPED, ERRORS, STARTED_AT, FINISHED_AT, MESSAGE)
            VALUES (@id, @t, @rt, @rr, @ru, @rs, @e, @s, @f, @m)
            """, new { id = Guid.NewGuid(), t = r.TableName, rt = runType, rr = r.RowsRead, ru = r.RowsUpserted, rs = r.RowsSkipped, e = r.Errors, s = DateTime.UtcNow - r.Duration, f = DateTime.UtcNow, m = r.Message });
    }

    public async Task LogRowErrorAsync(string table, string rowId, string error, object payload, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("""
            INSERT INTO W_SYNC_ROW_ERROR (ID, TABLE_NAME, ROW_ID, ERROR, PAYLOAD, RETRY_COUNT, NEXT_RETRY_AT, DEAD_LETTER, CREATED_AT)
            VALUES (@id, @t, @r, @e, @p, 0, @retry, 0, @now)
            """, new { id = Guid.NewGuid(), t = table, r = rowId, e = error, p = System.Text.Json.JsonSerializer.Serialize(payload), retry = DateTime.UtcNow.AddMinutes(1), now = DateTime.UtcNow });
    }

    public async Task HeartbeatAsync(string state, string? detail = null, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("""
            INSERT INTO W_SYNC_HEARTBEAT (ID, LAST_RUN_AT, STATE, DETAIL) VALUES (1, @now, @s, @d)
            ON DUPLICATE KEY UPDATE LAST_RUN_AT = @now, STATE = @s, DETAIL = @d
            """, new { now = DateTime.UtcNow, s = state, d = detail });
    }

    public async Task LogReconcileAsync(ReconcileTableResult r, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("""
            INSERT INTO W_SYNC_LOG (ID, TABLE_NAME, RUN_TYPE, ROWS_READ, ROWS_UPSERTED, ROWS_SKIPPED, ERRORS, STARTED_AT, FINISHED_AT, MESSAGE)
            VALUES (@id, @t, 'RECONCILE', @rr, @ru, @rs, @e, @s, @f, @m)
            """, new
        {
            id = Guid.NewGuid(), t = r.TableName, rr = r.FbRows,
            ru = r.Missing + r.Changed, rs = r.Tombstoned, e = r.Errors,
            s = DateTime.UtcNow - r.Duration, f = DateTime.UtcNow,
            m = $"fb={r.FbRows} my={r.MyRows} missing={r.Missing} changed={r.Changed} tombstone={r.Tombstoned}; {r.Message}"
        });
    }

    // ---------- Row error retry (mục 38) ----------

    public sealed record RowErrorItem(string Id, string TableName, string RowId, int RetryCount,
        DateTime? NextRetryAt, bool DeadLetter, string Error, DateTime CreatedAt);

    public async Task<List<RowErrorItem>> GetRowErrorsAsync(int limit, int deadOnly, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync("""
            SELECT ID, TABLE_NAME, ROW_ID, RETRY_COUNT, NEXT_RETRY_AT, DEAD_LETTER, ERROR, CREATED_AT
            FROM W_SYNC_ROW_ERROR
            WHERE DEAD_LETTER = @d
            ORDER BY CREATED_AT DESC
            LIMIT @lim
            """, new { d = deadOnly, lim = limit });
        return rows.Select(MapRowError).ToList();
    }

    private async Task<List<RowErrorItem>> GetRetryableInternalAsync(int limit, CancellationToken ct)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync("""
            SELECT ID, TABLE_NAME, ROW_ID, RETRY_COUNT, NEXT_RETRY_AT, DEAD_LETTER, ERROR, CREATED_AT
            FROM W_SYNC_ROW_ERROR
            WHERE DEAD_LETTER = 0 AND (NEXT_RETRY_AT IS NULL OR NEXT_RETRY_AT <= @now)
            ORDER BY CREATED_AT
            LIMIT @lim
            """, new { now = DateTime.UtcNow, lim = limit });
        return rows.Select(MapRowError).ToList();
    }

    /// <summary>Map tay — Dapper không materialize được record có DateTime? qua constructor (lỗi đã thấy ở heartbeat).</summary>
    private static RowErrorItem MapRowError(dynamic r)
    {
        DateTime? next = r.NEXT_RETRY_AT is null or DBNull ? null : (DateTime)r.NEXT_RETRY_AT;
        return new RowErrorItem(
            (string)r.ID, (string)r.TABLE_NAME, (string)r.ROW_ID,
            (int)r.RETRY_COUNT, next, Convert.ToBoolean(r.DEAD_LETTER),
            (string)r.ERROR, (DateTime)r.CREATED_AT);
    }

    public Task<List<RowErrorItem>> GetRetryableRowErrorsAsync(int limit, CancellationToken ct = default)
        => GetRetryableInternalAsync(limit, ct);

    public async Task ResolveRowErrorAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("DELETE FROM W_SYNC_ROW_ERROR WHERE ID = @id", new { id });
    }

    /// <summary>Backoff 1/5/15/30 phút (mục 23); quá 4 lần retry → DEAD_LETTER. Trả về 1 nếu dead-letter.</summary>
    public async Task<int> FailRowErrorAsync(string id, int retryCount, string error, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        int[] backoff = [1, 5, 15, 30];
        var minutes = backoff[Math.Min(Math.Max(retryCount - 1, 0), backoff.Length - 1)];
        var dead = retryCount >= 4 ? 1 : 0;
        await conn.ExecuteAsync("""
            UPDATE W_SYNC_ROW_ERROR
            SET RETRY_COUNT = @rc, NEXT_RETRY_AT = @next, ERROR = @e, DEAD_LETTER = @d
            WHERE ID = @id
            """, new { id, rc = retryCount, next = DateTime.UtcNow.AddMinutes(minutes), e = error, d = dead });
        return dead;
    }

    // ---------- Command queue: API → Sync Service (2 process tách biệt, giao tiếp qua MySQL) ----------

    public sealed record SyncCommandItem(string Id, string Command, string? Param, string Status,
        DateTime CreatedAt, DateTime? StartedAt, DateTime? FinishedAt, string? Result, string? CreatedBy);

    public async Task<string> EnqueueCommandAsync(string command, string? param, string? createdBy, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("""
            INSERT INTO W_SYNC_COMMAND (ID, COMMAND, PARAM, STATUS, CREATED_BY, CREATED_AT)
            VALUES (@id, @c, @p, 'PENDING', @by, @now)
            """, new { id, c = command.ToUpperInvariant(), p = param, by = createdBy, now = DateTime.UtcNow });
        return id;
    }

    public async Task<bool> HasPendingCommandAsync(string command, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<long?>(
            "SELECT 1 FROM W_SYNC_COMMAND WHERE COMMAND = @c AND STATUS IN ('PENDING','RUNNING') LIMIT 1",
            new { c = command.ToUpperInvariant() }) == 1;
    }

    public async Task<List<SyncCommandItem>> GetPendingCommandsAsync(int limit, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<SyncCommandItem>("""
            SELECT ID as Id, COMMAND as Command, PARAM as Param, STATUS as Status, CREATED_AT as CreatedAt,
                   STARTED_AT as StartedAt, FINISHED_AT as FinishedAt, RESULT as Result, CREATED_BY as CreatedBy
            FROM W_SYNC_COMMAND WHERE STATUS = 'PENDING' ORDER BY CREATED_AT LIMIT @lim
            """, new { lim = limit });
        return rows.ToList();
    }

    public async Task MarkCommandRunningAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("UPDATE W_SYNC_COMMAND SET STATUS = 'RUNNING', STARTED_AT = @now WHERE ID = @id",
            new { id, now = DateTime.UtcNow });
    }

    public async Task MarkCommandDoneAsync(string id, bool ok, string result, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("UPDATE W_SYNC_COMMAND SET STATUS = @s, RESULT = @r, FINISHED_AT = @now WHERE ID = @id",
            new { id, s = ok ? "DONE" : "FAILED", r = result, now = DateTime.UtcNow });
    }
}