using Dapper;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository Ä‘á»c + ghi DMATHANG + DNHOMMATHANG + OTONKHO tá»« mirror MySQL.
/// Ghi: web táº¡o/sá»­a/xÃ³a Ä‘áº·t W_ORIGIN='WEB' â€” sync FB khÃ´ng ghi Ä‘Ã¨ dÃ²ng WEB.
/// DMATHANG cÃ³ 59 cá»™t nhÆ°ng API chá»‰ expose táº­p cá»™t nghiá»‡p vá»¥ Ä‘Ã£ xÃ¡c minh (má»¥c 60).
/// </summary>
public sealed class ProductRepository
{
    private readonly MySqlConnectionFactory _factory;
    public ProductRepository(MySqlConnectionFactory factory) => _factory = factory;

    private static readonly string[] SelectCols =
    [
        "p.ID", "p.CODE", "p.NAME", "p.NOTE", "p.GIABAN", "p.GIABAN2", "p.GIANHAP", "p.GIAVON",
        "p.TONTOITHIEU", "p.TONTOIDA", "p.HOAHONG", "p.STATUS", "p.XUATXU", "p.MAUSAC",
        "p.CHATLIEU", "p.COHANSUDUNG", "p.COSERI", "p.TIMECREATED", "p.TIMEMODIFIED"
    ];

    public async Task<(IReadOnlyList<ProductDto> Items, long Total)> SearchAsync(
        ProductListQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 200);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "p.W_DELETED = 0" };
        var dp = new DynamicParameters();

        if (q.ActiveOnly == true) where.Add("p.STATUS = 30");
        if (!string.IsNullOrWhiteSpace(q.GroupId)) { where.Add("p.DNHOMMATHANGID = @g"); dp.Add("@g", q.GroupId); }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(p.NAME LIKE @s OR p.CODE LIKE @s OR p.MASANCO LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }

        var whereSql = string.Join(" AND ", where);
        var joinSql =
            "LEFT JOIN DDONVITINH u ON u.ID = p.DDONVITINHID " +
            "LEFT JOIN DNHOMMATHANG g ON g.ID = p.DNHOMMATHANGID " +
            "LEFT JOIN DHANGSANXUAT h ON h.ID = p.DHANGSANXUATID";

        var colSql = string.Join(", ", SelectCols) + ", u.NAME AS DonViTinh, g.NAME AS NhomMatHang, h.NAME AS HangSanXuat";

        var sort = q.Sort?.Trim() switch
        {
            "CODE" => "p.CODE",
            "GIABAN" => "p.GIABAN",
            _ => "p.NAME"
        };

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM DMATHANG p WHERE {whereSql}", dp);
        var rows = await conn.QueryAsync<ProductDto>(
            $"""
            SELECT {colSql} FROM DMATHANG p {joinSql}
            WHERE {whereSql}
            ORDER BY {sort} ASC
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }));

        return (rows.ToList(), total);
    }

    public async Task<ProductDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var colSql = string.Join(", ", SelectCols) + ", u.NAME AS DonViTinh, g.NAME AS NhomMatHang, h.NAME AS HangSanXuat";
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<ProductDto>(
            $"""
            SELECT {colSql} FROM DMATHANG p
            LEFT JOIN DDONVITINH u ON u.ID = p.DDONVITINHID
            LEFT JOIN DNHOMMATHANG g ON g.ID = p.DNHOMMATHANGID
            LEFT JOIN DHANGSANXUAT h ON h.ID = p.DHANGSANXUATID
            WHERE p.ID = @id AND p.W_DELETED = 0
            """, new { id });
    }

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        var colSql = string.Join(", ", SelectCols) + ", u.NAME AS DonViTinh, g.NAME AS NhomMatHang, h.NAME AS HangSanXuat";
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<ProductDto>(
            $"""
            SELECT {colSql} FROM DMATHANG p
            LEFT JOIN DDONVITINH u ON u.ID = p.DDONVITINHID
            LEFT JOIN DNHOMMATHANG g ON g.ID = p.DNHOMMATHANGID
            LEFT JOIN DHANGSANXUAT h ON h.ID = p.DHANGSANXUATID
            WHERE (p.CODE = @b OR p.MASANCO = @b) AND p.STATUS = 30 AND p.W_DELETED = 0
            LIMIT 1
            """, new { b = barcode });
    }

    public async Task<IReadOnlyList<ProductGroupDto>> GetGroupsAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<ProductGroupDto>(
            "SELECT ID, NAME, STATUS FROM DNHOMMATHANG WHERE STATUS = 30 ORDER BY NAME");
        return rows.ToList();
    }

    public async Task<IReadOnlyList<ProductStockDto>> GetStockAsync(string productId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<ProductStockDto>(
            """
            SELECT o.DKHOHANGID AS WareHouseId, w.NAME AS WareHouseName, COALESCE(o.TON,0) AS Ton
            FROM OTONKHO o
            LEFT JOIN DKHOHANG w ON w.ID = o.DKHOHANGID
            WHERE o.DMATHANGID = @id AND o.W_DELETED = 0
            ORDER BY w.NAME
            """, new { id = productId });
        return rows.ToList();
    }

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }

    // ---------- Phase 4 CRUD (web-origin) ----------

    public async Task<bool> CodeExistsAsync(string code, string? excludeId = null, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var n = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM DMATHANG WHERE CODE = @c AND W_DELETED = 0 AND (@x IS NULL OR ID <> @x)",
            new { c = code, x = excludeId });
        return n > 0;
    }

    public async Task<bool> GroupsExistAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var idList = ids.Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        if (idList.Count == 0) return true;
        await using var conn = await _factory.OpenAsync(ct);
        var dp = new DynamicParameters();
        for (int i = 0; i < idList.Count; i++) dp.Add("@g" + i, idList[i]);
        var inSql = string.Join(", ", idList.Select((_, i) => "@g" + i));
        var n = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM DNHOMMATHANG WHERE ID IN ({inSql}) AND W_DELETED = 0", dp);
        return n == idList.Count;
    }

    public async Task<string> InsertAsync(ProductCreateRequest r, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO DMATHANG
            (ID, NAME, CODE, MASANCO, NOTE, STATUS, USERCREATEDID, TIMECREATED,
             GIABAN, GIANHAP, TONTOITHIEU, TONTOIDA,
             DNHOMMATHANGID, DDONVITINHID, DHANGSANXUATID,
             XUATXU, MAUSAC, CHATLIEU, COHANSUDUNG, COSERI,
             W_SYNC_AT, W_SYNC_REV, W_ORIGIN, W_ROW_HASH, W_DELETED)
            VALUES
            (@id, @name, @code, @masanco, @note, 30, @uc, @tc,
             @giaban, @gianhap, @tontoithieu, @tontoida,
             @nhom, @dvt, @hang,
             @xuatxu, @mausac, @chatlieu, @hsd, @seri,
             NOW(3), 0, 'WEB', NULL, 0)
            """, new
            {
                id,
                r.Name,
                code = EmptyToNull(r.Code),
                masanco = EmptyToNull(r.MaSanCo),
                note = EmptyToNull(r.Note),
                uc = userId,
                tc = DateTime.Now,
                giaban = r.GiaBan,
                gianhap = r.GiaNhap,
                tontoithieu = r.TonToiThieu,
                tontoida = r.TonToiDa,
                nhom = EmptyToNull(r.NhomMatHangId),
                dvt = EmptyToNull(r.DonViTinhId),
                hang = EmptyToNull(r.HangSanXuatId),
                xuatxu = EmptyToNull(r.XuatXu),
                mausac = EmptyToNull(r.MauSac),
                chatlieu = EmptyToNull(r.ChatLieu),
                hsd = r.CoHanSuDung ? 1 : 0,
                seri = r.CoSeri ? 1 : 0
            });
        return id;
    }

    public async Task<int> UpdateAsync(string id, ProductUpdateRequest r, string userId, CancellationToken ct = default)
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
        Set("GIABAN", r.GiaBan);
        Set("GIANHAP", r.GiaNhap);
        Set("TONTOITHIEU", r.TonToiThieu);
        Set("TONTOIDA", r.TonToiDa);
        Set("DNHOMMATHANGID", r.NhomMatHangId);
        Set("DDONVITINHID", r.DonViTinhId);
        Set("XUATXU", r.XuatXu);
        Set("MAUSAC", r.MauSac);
        Set("CHATLIEU", r.ChatLieu);
        if (r.CoHanSuDung is not null) Set("COHANSUDUNG", r.CoHanSuDung.Value ? 1 : 0);
        if (r.CoSeri is not null) Set("COSERI", r.CoSeri.Value ? 1 : 0);
        Set("STATUS", r.Status);

        if (sets.Count == 0) return 0;
        var sql = $"""
            UPDATE DMATHANG
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
            UPDATE DMATHANG
            SET W_DELETED = 1, W_DELETED_AT = @now, USERMODIFIEDID = @uid, TIMEMODIFIED = @tm, W_SYNC_REV = W_SYNC_REV + 1
            WHERE ID = @id AND W_DELETED = 0
            """, new { id, uid = userId, tm = DateTime.Now, now = DateTime.UtcNow });
    }

    private static object? EmptyToNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
