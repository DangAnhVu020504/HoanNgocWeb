using Dapper;
using TechPos.Data.MySql;
using TechPos.Shared;
using TechPos.Sync.Core;

namespace TechPos.Sync.Service;

/// <summary>
/// Seed W_SYNC_TABLE_POLICY theo nhóm mục 8 (nghiệp vụ 30s, danh mục 2 phút, nhỏ 10 phút).
/// Không đè policy admin đã chỉnh (INSERT IGNORE).
/// </summary>
public sealed class SyncPolicySeeder(MySqlConnectionFactory factory)
{
    public async Task SeedDefaultPoliciesAsync(CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        var rows = new List<object>();
        rows.AddRange(SyncTables.Transaction.Select(t => new
        { t, g = "TRANSACTION", i = SyncTables.TransactionIntervalSeconds, p = 10 }));
        rows.AddRange(SyncTables.Dictionary.Select(t => new
        { t, g = "DICTIONARY", i = SyncTables.DictionaryIntervalSeconds, p = 200 }));
        await conn.ExecuteAsync("""
            INSERT IGNORE INTO W_SYNC_TABLE_POLICY (TABLE_NAME, GROUP_NAME, INTERVAL_SEC, ENABLED, PRIORITY, BATCH_SIZE)
            VALUES (@t, @g, @i, 1, @p, 2000)
            """, rows);
    }
}