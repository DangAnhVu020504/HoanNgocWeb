using Serilog;
using Dapper;
using TechPos.Data.Firebird;
using TechPos.Shared;
using TechPos.Sync.Core;

namespace TechPos.Sync.Service;

/// <summary>
/// GĐ7 — Two-way outbox worker (KE-HOACH §7.2–7.3): đọc W_OUTBOX PENDING
/// và ghi ngược Firebird theo whitelist W_SYNC_OUTBOX_WHITELIST.
/// CHỈ chạy khi flag ENABLE_TWO_WAY_SYNC=true (quy tắc vàng 6 — mặc định OFF).
/// Xung đột: so TIMEMODIFIED — FB sửa sau ⇒ ghi W_CONFLICT, giữ bản FB (§7.3).
/// Ghi cột whitelist cột (WRITE_COLS); UPDATE mirror giữ nguyên.
/// </summary>
public sealed class TwoWaySyncEngine(
    OutboxRepository outbox,
    SyncMetaRepository.MySqlConnectionFactoryLike mySql,
    FirebirdConnectionFactory fbFactory,
    FeatureFlagOptions flags)
{
    public async Task<(int Sent, int Conflicts, int Failed)> ProcessPendingAsync(CancellationToken ct = default)
    {
        if (!flags.EnableTwoWaySync) return (0, 0, 0);   // flag OFF — no-op (vàng 6)

        var whitelist = (await outbox.GetWhitelistAsync(ct))
            .ToDictionary(w => w.TableName, w => w, StringComparer.OrdinalIgnoreCase);
        var batch = await outbox.ClaimPendingAsync(20, ct);
        var sent = 0; var conflicts = 0; var failed = 0;

        foreach (var item in batch)
        {
            try
            {
                if (!whitelist.TryGetValue(item.TableName, out var wl))
                {
                    Log.Warning("Outbox {Table} không trong whitelist — đánh dấu FAILED", item.TableName);
                    var deadWl = await outbox.MarkFailedAsync(item.Id, 4, "Bảng không trong whitelist ghi ngược", ct);
                    if (deadWl) failed++;
                    continue;
                }

                var allowedCols = string.IsNullOrWhiteSpace(wl.WriteCols)
                    ? null : wl.WriteCols.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                await using var fb = await fbFactory.OpenAsync(ct);

                if (string.Equals(item.Op, "INSERT", StringComparison.OrdinalIgnoreCase))
                {
                    await UpsertFirebirdAsync(fb, item, allowedCols, ct);
                }
                else // UPDATE
                {
                    // Xung đột (§7.3): so TIMEMODIFIED 2 phía
                    var fbMod = await fb.ExecuteScalarAsync<DateTime?>(
                        $"SELECT TIMEMODIFIED FROM \"{item.TableName}\" WHERE ID = @id", new { id = item.RowId });
                    var myMod = await GetMirrorModifiedAsync(item.TableName, item.RowId, ct);
                    if (fbMod.HasValue && myMod.HasValue && fbMod.Value > myMod.Value.AddSeconds(1))
                    {
                        Log.Warning("Conflict {Table}/{Id}: FB modified {Fb} > mirror {My} — giữ bản FB, ghi W_CONFLICT",
                            item.TableName, item.RowId, fbMod, myMod);
                        await outbox.MarkConflictAsync(item.Id, item.TableName, item.RowId,
                            fbMod, myMod, $"FB modified {fbMod:O} vs mirror {myMod:O}", ct);
                        conflicts++;
                        continue;
                    }
                    await UpsertFirebirdAsync(fb, item, allowedCols, ct);
                }

                await outbox.MarkDoneAsync(item.Id, ct);
                sent++;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Outbox {Id} ({Table}) lỗi — retry backoff", item.Id, item.TableName);
                var dead = await outbox.MarkFailedAsync(item.Id, item.RetryCount + 1, ex.Message, ct);
                if (dead) failed++;
            }
        }
        return (sent, conflicts, failed);
    }

    /// <summary>Upsert Firebird từ PAYLOAD JSON — chỉ ghi cột whitelist. UUIDv7 kiểm tra trùng (§7.5).</summary>
    private static async Task UpsertFirebirdAsync(
        FirebirdSql.Data.FirebirdClient.FbConnection fb, OutboxRepository.OutboxItem item,
        string[]? allowedCols, CancellationToken ct)
    {
        var payload = System.Text.Json.JsonDocument.Parse(item.Payload ?? "{}");
        var cols = new List<string>();
        var values = new List<string>();
        foreach (var p in payload.RootElement.EnumerateObject())
        {
            var col = p.Name.ToUpperInvariant();
            if (allowedCols != null && !allowedCols.Contains(col)) continue;   // whitelist cột
            cols.Add(col);
            values.Add(p.Value.ToString() switch
            {
                null or "" => "NULL",
                "true" => "1",
                "false" => "0",
                _ => "'" + p.Value.ToString().Replace("'", "''") + "'"
            });
        }
        if (cols.Count == 0) return;

        var sql = $"UPDATE \"{item.TableName}\" SET " +
                  string.Join(", ", cols.Where(c => c != "ID").Zip(values.Where((_, i) => cols[i] != "ID")).Select(z => $"{z.First} = {z.Second}")) +
                  $" WHERE ID = '{item.RowId}'";
        var affected = await fb.ExecuteAsync(sql);
        if (affected == 0 && string.Equals(item.Op, "INSERT", StringComparison.OrdinalIgnoreCase))
        {
            var insert = $"INSERT INTO \"{item.TableName}\" (ID, {string.Join(", ", cols.Where(c => c != "ID"))}) " +
                         $"VALUES ('{item.RowId}', {string.Join(", ", values.Where((_, i) => cols[i] != "ID"))})";
            await fb.ExecuteAsync(insert);
        }
    }

    private async Task<DateTime?> GetMirrorModifiedAsync(string tableName, string rowId, CancellationToken ct)
    {
        await using var conn = await mySql.OpenAsync(ct);
        // Bảng mirror luôn có TIMEMODIFIED (86/86 có watermark — SCHEMA_DISCOVERY)
        return await conn.ExecuteScalarAsync<DateTime?>(
            $"SELECT TIMEMODIFIED FROM `{tableName}` WHERE ID = @id", new { id = rowId });
    }
}
