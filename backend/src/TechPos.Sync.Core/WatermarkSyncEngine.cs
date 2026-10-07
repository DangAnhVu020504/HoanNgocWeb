using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using MySqlConnector;

namespace TechPos.Sync.Core;

/// <summary>Source Firebird tối giản cho sync (READ-ONLY).</summary>
public interface IFirebirdSyncSource
{
    Task<DbConnection> OpenAsync(CancellationToken ct = default);
}

public interface IMySqlSyncTarget
{
    Task<MySqlConnection> OpenAsync(CancellationToken ct = default);
}

/// <summary>
/// W1 — Watermark incremental sync engine (mục 7).
/// Pull Firebird theo (TIMEMODIFIED, ID) → upsert mirror MySQL → per-row error → cursor advance.
/// 1 row lỗi KHÔNG chết batch (W_SYNC_ROW_ERROR + dead letter).
/// </summary>
public sealed class WatermarkSyncEngine(
    IFirebirdSyncSource firebird,
    IMySqlSyncTarget mysql,
    SyncMetaRepository meta,
    Serilog.ILogger logger)
{
    public async Task<SyncRunResult> SyncTableAsync(string table, int batchSize, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int read = 0, upserted = 0, skipped = 0, errors = 0;
        string? message = null;
        Dictionary<string, object?>? lastRow = null;

        // Chuẩn hóa: bảng Firebird luôn UPPERCASE; information_schema có thể trả lowercase
        table = table.Trim().ToUpperInvariant();

        try
        {
            if (!await MirrorTableExistsAsync(table, ct))
                return await SkipAsync(table, sw, "Mirror table chưa được sinh — bỏ qua (chờ SchemaGenerator)", ct);

            var (columns, hasTimeModified) = await DiscoverFirebirdColumnsAsync(table, ct);
            if (columns.Count == 0)
                return await SkipAsync(table, sw, "Không đọc được cột của bảng Firebird", ct);

            // Mirror chỉ có cột nào tồn tại thật ở MySQL — mirror schema có thể chưa đầy đủ
            var mirrorColumns = await GetMirrorColumnsAsync(table, ct);
            var syncColumns = columns.Where(c => mirrorColumns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
            if (syncColumns.Count == 0)
                return await SkipAsync(table, sw, "Mirror chưa có cột nào khớp — bỏ qua", ct);

            await using var fbConn = await firebird.OpenAsync(ct);
            await using var myConn = await mysql.OpenAsync(ct);   // 1 connection MySQL cho cả batch
            await using var fbCmd = fbConn.CreateCommand();

            string sql;
            if (hasTimeModified)
            {
                var cursor = await meta.GetCursorAsync(table, ct);
                if (cursor is null)
                {
                    // Lần chạy đầu (initial full load): pagination theo ID — an toàn cả khi TIMEMODIFIED toàn NULL
                    sql = $"""
                        SELECT {string.Join(", ", syncColumns.Select(QuoteFb))} FROM {QuoteFb(table)}
                        ORDER BY ID
                        ROWS 1 TO {batchSize}
                        """;
                }
                else if (cursor.LastModified is null)
                {
                    // Chưa có watermark (bảng có TIMEMODIFIED toàn NULL) → tiếp tục phân trang theo ID
                    sql = $"""
                        SELECT {string.Join(", ", syncColumns.Select(QuoteFb))} FROM {QuoteFb(table)}
                        WHERE ID > @lid
                        ORDER BY ID
                        ROWS 1 TO {batchSize}
                        """;
                    AddFbParam(fbCmd, "@lid", cursor.LastId ?? string.Empty);
                }
                else
                {
                    // Incremental chuẩn theo watermark (mục 7 — W1)
                    sql = $"""
                        SELECT {string.Join(", ", syncColumns.Select(QuoteFb))} FROM {QuoteFb(table)}
                        WHERE TIMEMODIFIED > @lm
                           OR (TIMEMODIFIED = @lm AND ID > @lid)
                        ORDER BY TIMEMODIFIED, ID
                        ROWS 1 TO {batchSize}
                        """;
                    AddFbParam(fbCmd, "@lm", cursor.LastModified);
                    AddFbParam(fbCmd, "@lid", cursor.LastId ?? string.Empty);
                }
                fbCmd.CommandText = sql;
            }
            else
            {
                // Bảng nhỏ không TIMEMODIFIED: full reload theo batch (hash compare bỏ row không đổi)
                sql = $"SELECT {string.Join(", ", syncColumns.Select(QuoteFb))} FROM {QuoteFb(table)} ROWS 1 TO {batchSize}";
                fbCmd.CommandText = sql;
            }

            await using var reader = await fbCmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                read++;
                var row = ReadRow(reader, syncColumns);
                lastRow = row;
                try
                {
                    var changed = await MirrorWriter.UpsertAsync(myConn, table, syncColumns, row, ct);
                    if (changed) upserted++; else skipped++;
                }
                catch (Exception ex)
                {
                    errors++;
                    var rowId = row.TryGetValue("ID", out var id) ? id?.ToString() ?? "" : "";
                    await meta.LogRowErrorAsync(table, rowId, ex.Message, row, ct);
                    logger.Warning(ex, "Row error {Table} {RowId}", table, rowId);
                }
            }

            // Advance cursor theo row cuối (chỉ khi batch 0 lỗi — cursor là commit point)
            if (errors == 0 && lastRow is not null)
            {
                DateTime? lm = lastRow.TryGetValue("TIMEMODIFIED", out var t) && t is DateTime dt ? dt : null;
                string? lid = lastRow.TryGetValue("ID", out var id2) ? id2?.ToString() : null;
                await meta.SaveCursorAsync(table, lm, lid, read, ct);
            }
            else if (errors == 0 && hasTimeModified && read == 0)
            {
                // Chế độ phân trang theo ID đã hết dữ liệu → reset cursor để vòng sau quét lại từ đầu.
                // ID là GUID ngẫu nhiên: row mới có thể < LastId, không reset sẽ bị bỏ sót.
                var cur = await meta.GetCursorAsync(table, ct);
                if (cur is not null && cur.LastModified is null)
                    await meta.SaveCursorAsync(table, null, null, 0, ct);
            }
        }
        catch (Exception ex)
        {
            errors++;
            message = ex.Message;
            logger.Error(ex, "Sync table failed: {Table}", table);
        }

        var result = new SyncRunResult(table, read, upserted, skipped, errors, sw.Elapsed, message);
        await meta.LogRunAsync(result, "INCREMENTAL", ct);
        return result;
    }

    // ---------- helpers ----------

    /// <summary>Early-return khi bảng bỏ qua — vẫn ghi W_SYNC_LOG để theo dõi được trên Sync Admin.</summary>
    private async Task<SyncRunResult> SkipAsync(string table, System.Diagnostics.Stopwatch sw, string reason, CancellationToken ct)
    {
        var r = new SyncRunResult(table, 0, 0, 0, 0, sw.Elapsed, reason);
        await meta.LogRunAsync(r, "INCREMENTAL", ct);
        logger.Information("Skip {Table}: {Reason}", table, reason);
        return r;
    }

    private async Task<bool> MirrorTableExistsAsync(string table, CancellationToken ct)
        => await MirrorWriter.MirrorTableExistsAsync(mysql, table, ct);

    private Task<List<string>> GetMirrorColumnsAsync(string table, CancellationToken ct)
        => MirrorWriter.GetMirrorColumnsAsync(mysql, table, ct);

    private Task<(List<string> Columns, bool HasTimeModified)> DiscoverFirebirdColumnsAsync(string table, CancellationToken ct)
        => MirrorWriter.DiscoverFirebirdColumnsAsync(firebird, table, ct);

    private static Dictionary<string, object?> ReadRow(DbDataReader reader, List<string> columns)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < columns.Count; i++)
            row[columns[i].ToUpperInvariant()] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        return row;
    }

    private static string QuoteFb(string name) => "\"" + name.TrimEnd('\0', ' ') + "\"";

    private static void AddFbParam(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = (object?)value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    // ---------- W3 phụ: Retry row lỗi (mục 38) — đọc lại từ Firebird theo ID, KHÔNG dùng payload JSON (tránh sai kiểu tiền) ----------

    public sealed record RetryRowErrorsResult(int Processed, int Recovered, int DeadLettered);

    private sealed record RetryableRowError(Guid Id, string TableName, string RowId, int RetryCount);

    /// <summary>
    /// Xử lý các row lỗi đến hạn: đọc lại nguyên row từ Firebird theo ID → upsert.
    /// Thành công → xóa khỏi W_SYNC_ROW_ERROR. Thất bại → backoff 1/5/15/30 phút, quá 4 lần → DEAD_LETTER.
    /// </summary>
    public async Task<RetryRowErrorsResult> RetryRowErrorsAsync(int maxRows, CancellationToken ct = default)
    {
        int processed = 0, recovered = 0, dead = 0;
        var items = await meta.GetRetryableRowErrorsAsync(maxRows, ct);
        foreach (var e in items)
        {
            processed++;
            try
            {
                var table = e.TableName.Trim().ToUpperInvariant();
                var (cols, _) = await DiscoverFirebirdColumnsAsync(table, ct);
                var mirrorCols = await GetMirrorColumnsAsync(table, ct);
                var syncCols = cols.Where(c => mirrorCols.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();

                await using var fbConn = await firebird.OpenAsync(ct);
                await using var cmd = fbConn.CreateCommand();
                cmd.CommandText = $"SELECT {string.Join(", ", syncCols.Select(QuoteFb))} FROM {QuoteFb(table)} WHERE ID = @id ROWS 1 TO 1";
                AddFbParam(cmd, "@id", e.RowId);

                Dictionary<string, object?>? row = null;
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                    row = ReadRow(reader, syncCols);

                if (row is null)
                {
                    // Row đã bị xóa hẳn bên Firebird → dead letter (reconcile sẽ tombstone mirror)
                    dead += await meta.FailRowErrorAsync(e.Id, 4, "Row không còn tồn tại bên Firebird (hard delete)", ct);
                    continue;
                }

                await using var myConn = await mysql.OpenAsync(ct);
                await MirrorWriter.UpsertAsync(myConn, table, syncCols, row, ct);
                await meta.ResolveRowErrorAsync(e.Id, ct);
                recovered++;
            }
            catch (Exception ex)
            {
                dead += await meta.FailRowErrorAsync(e.Id, e.RetryCount + 1, ex.Message, ct);
                logger.Warning(ex, "Retry row error {Table} {RowId} failed", e.TableName, e.RowId);
            }
        }
        return new RetryRowErrorsResult(processed, recovered, dead);
    }
}