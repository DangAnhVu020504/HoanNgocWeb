using System.Data.Common;
using Dapper;

namespace TechPos.Sync.Core;

/// <summary>
/// W2 — Reconcile engine (mục 7): đối chiếu COUNT/ID/CHECKSUM Firebird ↔ MySQL.
/// - Thiếu ở MySQL  → upsert bổ sung.
/// - Hash khác      → resync row (bắt update không cập nhật watermark).
/// - Dư ở MySQL     → hard delete bên Firebird → soft delete mirror + W_SYNC_TOMBSTONE.
/// Không đụng W_SYNC_CURSOR (kênh incremental độc lập).
/// </summary>
public sealed record ReconcileTableResult(
    string TableName, int FbRows, int MyRows, int Missing, int Changed, int Tombstoned,
    int Errors, TimeSpan Duration, string? Message = null);

public sealed class ReconcileEngine(
    IFirebirdSyncSource firebird,
    IMySqlSyncTarget mysql,
    SyncMetaRepository meta,
    Serilog.ILogger logger)
{
    public async Task<List<ReconcileTableResult>> ReconcileAllAsync(CancellationToken ct = default)
    {
        var results = new List<ReconcileTableResult>();
        var policies = await meta.GetPoliciesAsync(ct);
        foreach (var p in policies.Where(x => x.Enabled))
        {
            try
            {
                results.Add(await ReconcileTableAsync(p.TableName, p.BatchSize, ct));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Reconcile failed: {Table}", p.TableName);
                results.Add(new ReconcileTableResult(p.TableName.ToUpperInvariant(), 0, 0, 0, 0, 0, 1,
                    TimeSpan.Zero, ex.Message));
            }
        }

        var totalMissing = results.Sum(r => r.Missing);
        var totalChanged = results.Sum(r => r.Changed);
        var totalTomb = results.Sum(r => r.Tombstoned);
        var totalErr = results.Sum(r => r.Errors);
        await meta.HeartbeatAsync("RECONCILED",
            $"missing={totalMissing} changed={totalChanged} tombstone={totalTomb} errors={totalErr}", ct);
        return results;
    }

    private readonly Dictionary<string, string> myHash = new(StringComparer.OrdinalIgnoreCase);

    public async Task<ReconcileTableResult> ReconcileTableAsync(string table, int batchRows, CancellationToken ct = default)
    {
        table = table.Trim().ToUpperInvariant();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int fbRows = 0, myRows = 0, missing = 0, changed = 0, tombstoned = 0, errors = 0;
        string? message = null;

        try
        {
            if (!await MirrorWriter.MirrorTableExistsAsync(mysql, table, ct))
            {
                var skip = new ReconcileTableResult(table, 0, 0, 0, 0, 0, 0, sw.Elapsed, "Mirror table chưa có — bỏ qua");
                await meta.LogReconcileAsync(skip, ct);
                return skip;
            }

            var (fbColumns, _) = await MirrorWriter.DiscoverFirebirdColumnsAsync(firebird, table, ct);
            var mirrorColumns = await MirrorWriter.GetMirrorColumnsAsync(mysql, table, ct);
            var syncColumns = fbColumns.Where(c => mirrorColumns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
            if (fbColumns.Count == 0 || syncColumns.Count == 0)
            {
                var skip = new ReconcileTableResult(table, 0, 0, 0, 0, 0, 0, sw.Elapsed, "Không khớp cột — bỏ qua");
                await meta.LogReconcileAsync(skip, ct);
                return skip;
            }

            // 1) Load hash map MySQL: ID → W_ROW_HASH (chỉ row nguồn FB)
            myHash.Clear();
            await using (var myConn = await mysql.OpenAsync(ct))
            {
                var hashes = await myConn.QueryAsync<(string Id, string? Hash)>(
                    $"SELECT ID AS Id, W_ROW_HASH AS Hash FROM {MirrorWriter.QuoteMy(table)} WHERE W_ORIGIN = 'FB'");
                foreach (var h in hashes)
                    if (h.Hash is not null)
                        myHash[h.Id.ToUpperInvariant()] = h.Hash;
                myRows = myHash.Count;
            }

            // 2) Stream Firebird theo phân trang ID, so hash từng row
            var fbSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? lastId = null;
            var colSql = string.Join(", ", syncColumns.Select(q => "\"" + q.TrimEnd('\0', ' ') + "\""));
            await using (var fbConn = await firebird.OpenAsync(ct))
            {
                while (true)
                {
                    await using var cmd = fbConn.CreateCommand();
                    cmd.CommandText = lastId is null
                        ? $"SELECT {colSql} FROM \"{table}\" ORDER BY ID ROWS 1 TO {batchRows}"
                        : $"SELECT {colSql} FROM \"{table}\" WHERE ID > @lid ORDER BY ID ROWS 1 TO {batchRows}";
                    if (lastId is not null) AddParam(cmd, "@lid", lastId);

                    var batch = new List<Dictionary<string, object?>>();
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        batch.Add(ReadRow(reader, syncColumns));

                    if (batch.Count == 0) break;
                    lastId = batch[^1].TryGetValue("ID", out var lid) ? lid?.ToString() : null;

                    await using (var myConn = await mysql.OpenAsync(ct))
                    {
                        foreach (var row in batch)
                        {
                            fbRows++;
                            var id = row.TryGetValue("ID", out var v) ? v?.ToString() ?? "" : "";
                            if (id.Length == 0) continue;
                            fbSeen.Add(id.ToUpperInvariant());
                            try
                            {
                                var hash = MirrorWriter.ComputeHash(row);
                                if (!myHash.TryGetValue(id.ToUpperInvariant(), out var existing))
                                {
                                    await MirrorWriter.UpsertAsync(myConn, table, syncColumns, row, ct);
                                    missing++;
                                }
                                else if (!string.Equals(existing, hash, StringComparison.OrdinalIgnoreCase))
                                {
                                    await MirrorWriter.UpsertAsync(myConn, table, syncColumns, row, ct);
                                    changed++;
                                }
                            }
                            catch (Exception ex)
                            {
                                errors++;
                                await meta.LogRowErrorAsync(table, id, "RECONCILE: " + ex.Message, row, ct);
                                logger.Warning(ex, "Reconcile row error {Table} {Id}", table, id);
                            }
                        }
                    }

                    if (batch.Count < batchRows) break;
                }
            }

            // 3) Hard delete bên Firebird → soft delete mirror + tombstone (mục 31)
            var gone = myHash.Keys.Where(k => !fbSeen.Contains(k)).ToList();
            if (gone.Count > 0)
            {
                await using var myConn = await mysql.OpenAsync(ct);
                foreach (var chunk in gone.Chunk(500))
                {
                    var dp = new DynamicParameters();
                    for (int i = 0; i < chunk.Length; i++) dp.Add("@g" + i, chunk[i]);
                    var idList = chunk.Select((_, i) => "@g" + i).ToArray();

                    await myConn.ExecuteAsync(
                        $"UPDATE {MirrorWriter.QuoteMy(table)} SET W_DELETED = 1, W_DELETED_AT = @now " +
                        $"WHERE ID IN ({string.Join(", ", idList)})",
                        Merge(dp, new { now = DateTime.UtcNow }));
                    await myConn.ExecuteAsync(
                        "INSERT INTO W_SYNC_TOMBSTONE (ID, TABLE_NAME, DETECTED_AT, ACTION) " +
                        $"VALUES ({string.Join(", ", idList)}, @t, @now, 'SOFT_DELETE') " +
                        "ON DUPLICATE KEY UPDATE DETECTED_AT = VALUES(DETECTED_AT)",
                        Merge(dp, new { t = table, now = DateTime.UtcNow }));
                    tombstoned += chunk.Length;
                }
                logger.Warning("Reconcile {Table}: {Count} row biến mất bên Firebird → tombstone", table, gone.Count);
            }
        }
        catch (Exception ex)
        {
            errors++;
            message = ex.Message;
            logger.Error(ex, "Reconcile table failed: {Table}", table);
        }

        var result = new ReconcileTableResult(table, fbRows, myRows, missing, changed, tombstoned, errors, sw.Elapsed, message);
        await meta.LogReconcileAsync(result, ct);
        return result;
    }

    private static Dictionary<string, object?> ReadRow(DbDataReader reader, List<string> columns)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < columns.Count; i++)
            row[columns[i].ToUpperInvariant()] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        return row;
    }

    private static void AddParam(DbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = (object?)value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }
}
