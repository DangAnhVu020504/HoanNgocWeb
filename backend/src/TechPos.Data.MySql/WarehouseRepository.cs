using Dapper;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository Ä‘á»c + ghi DKHOHANG tá»« mirror MySQL (Phase 4).
/// DKHOHANG lÃ  cÃ¢y (PARENTID/PARENTDIR, ItemType phÃ¢n loáº¡i kho thÃ nh pháº©m/váº­t tÆ°).
/// Ghi: web táº¡o/sá»­a/xÃ³a Ä‘áº·t W_ORIGIN='WEB'.
/// </summary>
public sealed class WarehouseRepository
{
    private readonly MySqlConnectionFactory _factory;
    public WarehouseRepository(MySqlConnectionFactory factory) => _factory = factory;

    private const string ColSql =
        "k.ID, k.NAME, k.NOTE, k.STATUS, k.PARENTID AS ParentId, pk.NAME AS ParentName, " +
        "ch.NAME AS CuaHang, COALESCE(k.CHOPHEPAMKHO,0) AS ChoPhepAmKho, k.TIMECREATED, k.TIMEMODIFIED";

    public async Task<(IReadOnlyList<WarehouseDto> Items, long Total)> SearchAsync(
        WarehouseListQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "k.W_DELETED = 0" };
        var dp = new DynamicParameters();

        if (q.ActiveOnly == true) where.Add("k.STATUS = 30");
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(k.NAME LIKE @s OR k.NOTE LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }

        var whereSql = string.Join(" AND ", where);
        var sort = q.Sort?.Trim() switch
        {
            "STATUS" => "k.STATUS",
            _ => "k.NAME"
        };

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM DKHOHANG k WHERE {whereSql}", dp);
        var rows = await conn.QueryAsync<WarehouseDto>(
            $"""
            SELECT {ColSql} FROM DKHOHANG k
            LEFT JOIN DKHOHANG pk ON pk.ID = k.PARENTID
            LEFT JOIN DCUAHANG ch ON ch.ID = k.DCUAHANGID
            WHERE {whereSql}
            ORDER BY {sort} ASC
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }));

        return (rows.ToList(), total);
    }

    public async Task<WarehouseDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<WarehouseDto>(
            $"""
            SELECT {ColSql} FROM DKHOHANG k
            LEFT JOIN DKHOHANG pk ON pk.ID = k.PARENTID
            LEFT JOIN DCUAHANG ch ON ch.ID = k.DCUAHANGID
            WHERE k.ID = @id AND k.W_DELETED = 0
            """, new { id });
    }

    // ---------- Phase 4 CRUD (web-origin) ----------

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM DKHOHANG WHERE ID = @id AND W_DELETED = 0", new { id }) > 0;
    }

    public async Task<string> InsertAsync(WarehouseCreateRequest r, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO DKHOHANG
            (ID, NAME, NOTE, STATUS, USERCREATEDID, TIMECREATED, PARENTID,
             CHOPHEPAMKHO, DCUAHANGID, W_SYNC_AT, W_SYNC_REV, W_ORIGIN, W_ROW_HASH, W_DELETED)
            VALUES
            (@id, @name, @note, 30, @uc, @tc, @parent,
             @amkho, @cua, NOW(3), 0, 'WEB', NULL, 0)
            """, new
            {
                id,
                r.Name,
                note = EmptyToNull(r.Note),
                uc = userId,
                tc = DateTime.Now,
                parent = EmptyToNull(r.ParentId),
                amkho = r.ChoPhepAmKho ? 1 : 0,
                cua = EmptyToNull(r.CuaHangId)
            });
        return id;
    }

    public async Task<int> UpdateAsync(string id, WarehouseUpdateRequest r, string userId, CancellationToken ct = default)
    {
        var sets = new List<string>();
        var dp = new DynamicParameters(); dp.Add("@id", id);

        void Set(string col, object? value)
        {
            if (value is null) return;
            sets.Add($"{col} = @{col}");
            dp.Add($"@{col}", value is string s ? EmptyToNull(s) : value);
        }

        Set("NAME", r.Name);
        Set("NOTE", r.Note);
        Set("PARENTID", r.ParentId);
        Set("DCUAHANGID", r.CuaHangId);
        if (r.ChoPhepAmKho is not null) Set("CHOPHEPAMKHO", r.ChoPhepAmKho.Value ? 1 : 0);
        Set("STATUS", r.Status);

        if (sets.Count == 0) return 0;
        var sql = $"""
            UPDATE DKHOHANG
            SET {string.Join(", ", sets)}, USERMODIFIEDID = @uid, TIMEMODIFIED = @tm, W_SYNC_REV = W_SYNC_REV + 1
            WHERE ID = @id AND W_DELETED = 0
            """;
        dp.Add("@uid", userId);
        dp.Add("@tm", DateTime.Now);
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteAsync(sql, dp);
    }

    public async Task<int> SoftDeleteAsync(string id, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteAsync(
            """
            UPDATE DKHOHANG
            SET W_DELETED = 1, W_DELETED_AT = @now, USERMODIFIEDID = @uid, TIMEMODIFIED = @tm, W_SYNC_REV = W_SYNC_REV + 1
            WHERE ID = @id AND W_DELETED = 0
            """, new { id, uid = userId, tm = DateTime.Now, now = DateTime.UtcNow });
    }

    private static object? EmptyToNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }
}
