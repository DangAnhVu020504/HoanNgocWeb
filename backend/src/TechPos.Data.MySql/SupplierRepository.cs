using Dapper;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository Ä‘á»c + ghi DNHACUNGCAP tá»« mirror MySQL (Phase 4).
/// Ghi: web táº¡o/sá»­a/xÃ³a Ä‘áº·t W_ORIGIN='WEB' â€” sync FB khÃ´ng ghi Ä‘Ã¨ dÃ²ng WEB.
/// Cá»™t Ä‘Ã£ xÃ¡c minh qua discovery (docs/database/discovery/tables/DNHACUNGCAP.json).
/// </summary>
public sealed class SupplierRepository
{
    private readonly MySqlConnectionFactory _factory;
    public SupplierRepository(MySqlConnectionFactory factory) => _factory = factory;

    private const string ColSql =
        "n.ID, n.NAME, n.MANHACUNGCAP AS MaNhaCungCap, n.DIACHI AS DiaChi, n.DIENTHOAI AS DienThoai, " +
        "n.EMAIL, n.WEBSITE, n.NOTE, n.STATUS, n.TIMECREATED, n.TIMEMODIFIED";

    private const string JoinSql = "LEFT JOIN DNHOMNHACUNGCAP g ON g.ID = n.DNHOMNHACUNGCAPID";
    private const string ExtraCols = ", g.NAME AS NhomNhaCungCap";

    public async Task<(IReadOnlyList<SupplierDto> Items, long Total)> SearchAsync(
        SupplierListQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "n.W_DELETED = 0" };
        var dp = new DynamicParameters();

        if (q.ActiveOnly == true) where.Add("n.STATUS = 30");
        if (!string.IsNullOrWhiteSpace(q.GroupId)) { where.Add("n.DNHOMNHACUNGCAPID = @g"); dp.Add("@g", q.GroupId); }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(n.NAME LIKE @s OR n.MANHACUNGCAP LIKE @s OR n.DIENTHOAI LIKE @s OR n.EMAIL LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }

        var whereSql = string.Join(" AND ", where);
        var sort = q.Sort?.Trim() switch
        {
            "MANHACUNGCAP" => "n.MANHACUNGCAP",
            "DIENTHOAI" => "n.DIENTHOAI",
            _ => "n.NAME"
        };

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM DNHACUNGCAP n WHERE {whereSql}", dp);
        var rows = await conn.QueryAsync<SupplierDto>(
            $"""
            SELECT {ColSql}{ExtraCols} FROM DNHACUNGCAP n {JoinSql}
            WHERE {whereSql}
            ORDER BY {sort} ASC
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }));

        return (rows.ToList(), total);
    }

    public async Task<SupplierDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<SupplierDto>(
            $"""
            SELECT {ColSql}{ExtraCols} FROM DNHACUNGCAP n {JoinSql}
            WHERE n.ID = @id AND n.W_DELETED = 0
            """, new { id });
    }

    public async Task<IReadOnlyList<SupplierGroupDto>> GetGroupsAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<SupplierGroupDto>(
            "SELECT ID, NAME, STATUS FROM DNHOMNHACUNGCAP WHERE STATUS = 30 ORDER BY NAME");
        return rows.ToList();
    }

    // ---------- Phase 4 CRUD (web-origin) ----------

    public async Task<bool> MaNccExistsAsync(string maNhaCungCap, string? excludeId = null, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var n = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM DNHACUNGCAP WHERE MANHACUNGCAP = @m AND W_DELETED = 0 AND (@x IS NULL OR ID <> @x)",
            new { m = maNhaCungCap, x = excludeId });
        return n > 0;
    }

    public async Task<string> InsertAsync(SupplierCreateRequest r, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO DNHACUNGCAP
            (ID, NAME, NOTE, STATUS, USERCREATEDID, TIMECREATED, MANHACUNGCAP,
             DIACHI, DIENTHOAI, EMAIL, WEBSITE, DNHOMNHACUNGCAPID,
             W_SYNC_AT, W_SYNC_REV, W_ORIGIN, W_ROW_HASH, W_DELETED)
            VALUES
            (@id, @name, @note, 30, @uc, @tc, @mancc,
             @diachi, @dthoai, @email, @website, @nhom,
             NOW(3), 0, 'WEB', NULL, 0)
            """, new
            {
                id,
                r.Name,
                note = EmptyToNull(r.Note),
                uc = userId,
                tc = DateTime.Now,
                mancc = EmptyToNull(r.MaNhaCungCap),
                diachi = EmptyToNull(r.DiaChi),
                dthoai = EmptyToNull(r.DienThoai),
                email = EmptyToNull(r.Email),
                website = EmptyToNull(r.Website),
                nhom = EmptyToNull(r.NhomNhaCungCapId)
            });
        return id;
    }

    public async Task<int> UpdateAsync(string id, SupplierUpdateRequest r, string userId, CancellationToken ct = default)
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
        Set("DIACHI", r.DiaChi);
        Set("DIENTHOAI", r.DienThoai);
        Set("EMAIL", r.Email);
        Set("WEBSITE", r.Website);
        Set("NOTE", r.Note);
        Set("DNHOMNHACUNGCAPID", r.NhomNhaCungCapId);
        Set("STATUS", r.Status);

        if (sets.Count == 0) return 0;
        var sql = $"""
            UPDATE DNHACUNGCAP
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
            UPDATE DNHACUNGCAP
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

/// <summary>DTO nhÃ³m nhÃ  cung cáº¥p (DNHOMNHACUNGCAP).</summary>
public sealed class SupplierGroupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Status { get; set; }
}
