using Dapper;
using MySqlConnector;

namespace TechPos.Sync.Core;

/// <summary>
/// Outbox 2 chiều (GĐ7 — KE-HOACH §7.2): đọc W_OUTBOX PENDING, ghi ngược Firebird
/// (embedded READ-WRITE, chỉ bảng trong W_SYNC_OUTBOX_WHITELIST), cập nhật mirror ORIGIN.
/// Chỉ chạy khi flag ENABLE_TWO_WAY_SYNC=true (quy tắc vàng 6).
/// </summary>
public sealed class OutboxRepository(SyncMetaRepository.MySqlConnectionFactoryLike factory)
{
    public sealed record OutboxItem(string Id, string TableName, string RowId, string Op, string? Payload, int RetryCount);

    public sealed record WhitelistRow(string TableName, string? WriteCols, byte IsEnabled);

    public async Task<List<WhitelistRow>> GetWhitelistAsync(CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        return (await conn.QueryAsync<WhitelistRow>(
            "SELECT TABLE_NAME AS TableName, WRITE_COLS AS WriteCols, IS_ENABLED AS IsEnabled " +
            "FROM W_SYNC_OUTBOX_WHITELIST WHERE IS_ENABLED = 1")).ToList();
    }

    /// <summary>Claim atomic tối đa @limit outbox PENDING.</summary>
    public async Task<List<OutboxItem>> ClaimPendingAsync(int limit, CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        var ids = (await conn.QueryAsync<string>(
            """
            SELECT ID FROM W_OUTBOX
            WHERE STATUS = 'PENDING' AND (NEXT_RETRY_AT IS NULL OR NEXT_RETRY_AT <= @now)
            ORDER BY CREATED_AT LIMIT @lim
            """, new { now = DateTime.UtcNow, lim = limit })).ToList();
        if (ids.Count == 0) return [];

        return (await conn.QueryAsync<OutboxItem>(
            """
            UPDATE W_OUTBOX SET STATUS = 'RUNNING' WHERE ID IN @ids AND STATUS = 'PENDING';
            SELECT ID, TABLE_NAME AS TableName, ROW_ID AS RowId, OP AS Op, CAST(PAYLOAD AS CHAR) AS Payload, RETRY_COUNT AS RetryCount
            FROM W_OUTBOX WHERE ID IN @ids AND STATUS = 'RUNNING'
            """, new { ids })).ToList();
    }

    public async Task MarkDoneAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE W_OUTBOX SET STATUS = 'DONE', SENT_AT = @now WHERE ID = @id",
            new { id, now = DateTime.UtcNow });
    }

    /// <summary>Backoff 1/5/15/30 phút; ≥4 retry → FAILED + W_CONFLICT nếu FB mới hơn (§7.3).</summary>
    public async Task<bool> MarkFailedAsync(string id, int retryCount, string error, CancellationToken ct = default)
    {
        int[] backoff = [1, 5, 15, 30];
        var minutes = backoff[Math.Min(Math.Max(retryCount - 1, 0), backoff.Length - 1)];
        await using var conn = await factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE W_OUTBOX
            SET RETRY_COUNT = @rc, NEXT_RETRY_AT = @next, LAST_ERROR = @e,
                STATUS = CASE WHEN @dead THEN 'FAILED' ELSE 'PENDING' END
            WHERE ID = @id
            """, new { rc = retryCount, next = DateTime.UtcNow.AddMinutes(minutes), e = error, dead = retryCount >= 4, id });
        return retryCount >= 4;
    }

    public async Task MarkConflictAsync(string id, string tableName, string rowId,
        DateTime? fbModified, DateTime? myModified, string detail, CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE W_OUTBOX SET STATUS = 'CONFLICT' WHERE ID = @id", new { id });
        await conn.ExecuteAsync(
            """
            INSERT INTO W_CONFLICT (ID, TABLE_NAME, ROW_ID, FB_MODIFIED, MY_MODIFIED, DETAIL, STATUS, DETECTED_AT)
            VALUES (@id, @t, @r, @fb, @my, @d, 'OPEN', @now)
            """, new { id = Guid.NewGuid().ToString(), t = tableName, r = rowId, fb = fbModified, my = myModified, d = detail, now = DateTime.UtcNow });
    }

    /// <summary>Đếm outbox pending/failed cho /health/sync.</summary>
    public async Task<(long Pending, long Failed, long Conflicts)> StatsAsync(CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        var pending = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM W_OUTBOX WHERE STATUS = 'PENDING'");
        var failed = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM W_OUTBOX WHERE STATUS = 'FAILED'");
        var conflicts = await conn.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM W_CONFLICT WHERE STATUS = 'OPEN'");
        return (pending, failed, conflicts);
    }
}
