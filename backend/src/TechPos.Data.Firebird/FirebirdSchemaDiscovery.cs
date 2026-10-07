using System.Data.Common;

namespace TechPos.Data.Firebird;

public sealed record DiscoveredTable(
    string Name, bool IsView, List<DiscoveredColumn> Columns,
    List<string> PrimaryKeys, List<DiscoveredIndex> Indexes, List<string> Triggers);

public sealed record DiscoveredIndex(string Name, string TableName, bool IsUnique, List<string> Columns);

public sealed record DiscoveredColumn(
    string Name, short FieldType, short SubType, int Length, int Precision, int Scale,
    bool Nullable, string? DefaultSource, int Position);

public sealed record DiscoveredGenerator(string Name);

/// <summary>
/// Phase 0 — Discovery schema Firebird thật từ RDB$ (mục 4).
/// KHÔNG dùng danh sách 86 bảng từ tài liệu làm nguồn sinh DDL.
/// </summary>
public sealed class FirebirdSchemaDiscovery(FirebirdConnectionFactory factory)
{
    public async Task<List<DiscoveredTable>> DiscoverAllAsync(CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);

        var tableRows = await ReadTableNamesAsync(conn, ct);
        var tables = new List<DiscoveredTable>();
        foreach (var (name, isView) in tableRows)
        {
            var columns = await DiscoverColumnsAsync(conn, name, ct);
            var pk = await DiscoverPrimaryKeysAsync(conn, name, ct);
            var indexes = await DiscoverIndexesAsync(conn, name, ct);
            var triggers = await DiscoverTriggersAsync(conn, name, ct);
            tables.Add(new DiscoveredTable(name, isView, columns, pk, indexes, triggers));
        }
        return tables;
    }

    public async Task<List<DiscoveredGenerator>> DiscoverGeneratorsAsync(CancellationToken ct = default)
    {
        await using var conn = await factory.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT RDB$GENERATOR_NAME FROM RDB$GENERATORS WHERE RDB$SYSTEM_FLAG = 0";
        var list = new List<DiscoveredGenerator>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) list.Add(new DiscoveredGenerator(Clean(r.GetString(0))));
        return list;
    }

    private async Task<List<(string Name, bool IsView)>> ReadTableNamesAsync(DbConnection conn, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT RDB$RELATION_NAME, CASE WHEN RDB$VIEW_BLR IS NULL THEN 0 ELSE 1 END AS IS_VIEW
            FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0 ORDER BY RDB$RELATION_NAME
            """;
        var list = new List<(string, bool)>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add((Clean(r.GetString(0)), r.GetInt32(1) == 1));
        return list;
    }

    internal static string Clean(string? s) => (s ?? string.Empty).TrimEnd('\0', ' ');

    private static void AddParam(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    private async Task<List<DiscoveredColumn>> DiscoverColumnsAsync(DbConnection conn, string table, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT RF.RDB$FIELD_NAME, F.RDB$FIELD_TYPE, F.RDB$FIELD_SUB_TYPE, F.RDB$FIELD_LENGTH,
                   F.RDB$FIELD_PRECISION, F.RDB$FIELD_SCALE, RF.RDB$NULL_FLAG,
                   RF.RDB$DEFAULT_SOURCE, RF.RDB$FIELD_POSITION
            FROM RDB$RELATION_FIELDS RF
            JOIN RDB$FIELDS F ON F.RDB$FIELD_NAME = RF.RDB$FIELD_SOURCE
            WHERE RF.RDB$RELATION_NAME = @t
            ORDER BY RF.RDB$FIELD_POSITION
            """;
        AddParam(cmd, "@t", table);
        var list = new List<DiscoveredColumn>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new DiscoveredColumn(
                Clean(r.GetString(0)),
                r.IsDBNull(1) ? (short)0 : r.GetInt16(1),
                r.IsDBNull(2) ? (short)0 : r.GetInt16(2),
                r.IsDBNull(3) ? 0 : r.GetInt32(3),
                r.IsDBNull(4) ? 0 : r.GetInt32(4),
                r.IsDBNull(5) ? 0 : r.GetInt32(5),
                r.IsDBNull(6) || r.GetInt16(6) == 0,
                r.IsDBNull(7) ? null : Clean(r.GetString(7)),
                r.IsDBNull(8) ? 0 : r.GetInt16(8)));
        }
        return list;
    }

    private async Task<List<string>> DiscoverPrimaryKeysAsync(DbConnection conn, string table, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT I.RDB$FIELD_NAME
            FROM RDB$RELATION_CONSTRAINTS C
            JOIN RDB$INDEX_SEGMENTS I ON I.RDB$INDEX_NAME = C.RDB$INDEX_NAME
            WHERE C.RDB$RELATION_NAME = @t AND C.RDB$CONSTRAINT_TYPE = 'PRIMARY KEY'
            ORDER BY I.RDB$FIELD_POSITION
            """;
        AddParam(cmd, "@t", table);
        var list = new List<string>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) list.Add(Clean(r.GetString(0)));
        return list;
    }

    private async Task<List<DiscoveredIndex>> DiscoverIndexesAsync(DbConnection conn, string table, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT I.RDB$INDEX_NAME, I.RDB$UNIQUE_FLAG, S.RDB$FIELD_NAME
            FROM RDB$INDICES I
            LEFT JOIN RDB$INDEX_SEGMENTS S ON S.RDB$INDEX_NAME = I.RDB$INDEX_NAME
            WHERE I.RDB$RELATION_NAME = @t AND I.RDB$SYSTEM_FLAG = 0
            ORDER BY I.RDB$INDEX_NAME, S.RDB$FIELD_POSITION
            """;
        AddParam(cmd, "@t", table);
        var dict = new Dictionary<string, DiscoveredIndex>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var idxName = Clean(r.GetString(0));
            var unique = !r.IsDBNull(1) && r.GetInt16(1) == 1;
            var col = r.IsDBNull(2) ? null : Clean(r.GetString(2));
            if (!dict.TryGetValue(idxName, out var idx))
            {
                idx = new DiscoveredIndex(idxName, table, unique, []);
                dict[idxName] = idx;
            }
            if (col is not null) idx.Columns.Add(col);
        }
        return [.. dict.Values];
    }

    private async Task<List<string>> DiscoverTriggersAsync(DbConnection conn, string table, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT RDB$TRIGGER_NAME FROM RDB$TRIGGERS
            WHERE RDB$RELATION_NAME = @t AND RDB$SYSTEM_FLAG = 0
            """;
        AddParam(cmd, "@t", table);
        var list = new List<string>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) list.Add(Clean(r.GetString(0)));
        return list;
    }
}