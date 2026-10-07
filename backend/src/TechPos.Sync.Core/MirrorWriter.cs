using System.Data.Common;
using Dapper;
using MySqlConnector;

namespace TechPos.Sync.Core;

/// <summary>
/// Helper dùng chung cho W1 (WatermarkSyncEngine) và W2 (ReconcileEngine):
/// phát hiện cột, hash row, upsert mirror. Giữ logic hash thống nhất để reconcile
/// so sánh W_ROW_HASH với hash Firebird tính tại chỗ.
/// </summary>
public static class MirrorWriter
{
    public static async Task<bool> MirrorTableExistsAsync(IMySqlSyncTarget mysql, string table, CancellationToken ct)
    {
        await using var conn = await mysql.OpenAsync(ct);
        var exists = await conn.ExecuteScalarAsync<long?>(
            "SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND UPPER(TABLE_NAME) = @t",
            new { t = table.ToUpperInvariant() });
        return exists == 1;
    }

    public static async Task<List<string>> GetMirrorColumnsAsync(IMySqlSyncTarget mysql, string table, CancellationToken ct)
    {
        await using var conn = await mysql.OpenAsync(ct);
        var cols = await conn.QueryAsync<string>(
            "SELECT COLUMN_NAME FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND UPPER(TABLE_NAME) = @t",
            new { t = table.ToUpperInvariant() });
        return cols.ToList();
    }

    public static async Task<(List<string> Columns, bool HasTimeModified)> DiscoverFirebirdColumnsAsync(
        IFirebirdSyncSource firebird, string table, CancellationToken ct)
    {
        await using var conn = await firebird.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT RF.RDB$FIELD_NAME FROM RDB$RELATION_FIELDS RF
            WHERE RF.RDB$RELATION_NAME = @t ORDER BY RF.RDB$FIELD_POSITION
            """;
        var p = cmd.CreateParameter();
        p.ParameterName = "@t";
        p.Value = table;
        cmd.Parameters.Add(p);
        var cols = new List<string>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            cols.Add(r.GetString(0).TrimEnd('\0', ' '));

        var hasTimeModified = cols.Any(c => c.Equals("TIMEMODIFIED", StringComparison.OrdinalIgnoreCase));
        return (cols, hasTimeModified);
    }

    /// <summary>Upsert mirror theo PK ID; trả về true nếu có thay đổi (so W_ROW_HASH).</summary>
    public static async Task<bool> UpsertAsync(MySqlConnection conn, string table, List<string> columns,
        Dictionary<string, object?> row, CancellationToken ct)
    {
        var hash = ComputeHash(row);
        var id = row.TryGetValue("ID", out var v) ? v?.ToString() : null;
        if (string.IsNullOrEmpty(id)) return false;

        var paramValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var col in columns)
            paramValues[col] = row.TryGetValue(col, out var rv) ? NormalizeValue(rv) : null;
        paramValues["W_SYNC_AT"] = DateTime.UtcNow;
        paramValues["W_ORIGIN"] = "FB";
        paramValues["W_ROW_HASH"] = hash;
        paramValues["__ID"] = id;

        var existingHash = await conn.ExecuteScalarAsync<string?>(
            $"SELECT W_ROW_HASH FROM {QuoteMy(table)} WHERE ID = @__ID", paramValues);
        if (existingHash == hash) return false;

        var allCols = columns.Concat(new[] { "W_SYNC_AT", "W_ORIGIN", "W_ROW_HASH" })
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var insertCols = new[] { "ID" }.Concat(allCols.Where(c => !c.Equals("ID", StringComparison.OrdinalIgnoreCase)));

        var colList = string.Join(", ", insertCols.Select(QuoteMy));
        var valueList = string.Join(", ", insertCols.Select(c => c == "ID" ? "@__ID" : "@" + c));
        var updateList = string.Join(", ", insertCols.Where(c => !c.Equals("ID", StringComparison.OrdinalIgnoreCase))
            .Select(c => $"{QuoteMy(c)} = @{c}"));

        var sql = $"INSERT INTO {QuoteMy(table)} ({colList}) VALUES ({valueList}) " +
                  $"ON DUPLICATE KEY UPDATE {updateList}";
        await conn.ExecuteAsync(sql, paramValues);
        return true;
    }

    public static object? NormalizeValue(object? v) => v switch
    {
        null => null,
        DateTime dt => dt,
        byte[] b => b,
        decimal d => d,
        _ => v.ToString() == string.Empty && v is byte[] ? null : v
    };

    public static string ComputeHash(Dictionary<string, object?> row)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var (k, v) in row.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            sb.Append(k).Append('=')
              .Append(v switch
              {
                  null => "\u0001",
                  byte[] b => Convert.ToBase64String(b),
                  _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)
              }).Append(';');
        }
        return Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(sb.ToString())));
    }

    public static string QuoteMy(string name) => "`" + name + "`";
}
