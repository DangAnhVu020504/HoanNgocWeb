using Dapper;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository Ä‘á»c + ghi DKHACHHANG tá»« mirror MySQL (Phase 4).
/// Ghi: web táº¡o/sá»­a/xÃ³a Ä‘áº·t W_ORIGIN='WEB' â€” reconcile/sync khÃ´ng Ä‘á»¥ng dÃ²ng WEB (ReconcileEngine chá»‰ so hash row FB).
/// Chá»‰ expose táº­p cá»™t nghiá»‡p vá»¥ Ä‘Ã£ xÃ¡c minh qua discovery (docs/database/discovery/tables/DKHACHHANG.json).
/// </summary>
public sealed class CustomerRepository
{
    private readonly MySqlConnectionFactory _factory;
    public CustomerRepository(MySqlConnectionFactory factory) => _factory = factory;

    private const string ColSql =
        "c.ID, c.NAME, c.MAKHACH AS MaKhach, c.DIACHI AS DiaChi, c.DIACHIGIAOHANG AS DiaChiGiaoHang, " +
        "c.DIENTHOAI AS DienThoai, c.EMAIL, c.MASOTHUE AS MaSoThue, c.NGUOIDAIDIEN AS NguoiDaiDien, " +
        "c.CHUCVU AS ChucVu, c.FACEBOOK, COALESCE(c.SONGAYCHONO,0) AS SongayChoNo, " +
        "COALESCE(c.DIEMTICHLUYBANDAU,0) AS DiemTichLuy, c.STATUS, c.TIMECREATED, c.TIMEMODIFIED";

    private const string JoinSql =
        "LEFT JOIN DNHOMKHACHHANG g ON g.ID = c.DNHOMKHACHHANGID " +
        "LEFT JOIN DNHANVIEN nv ON nv.ID = c.DNHANVIENID";

    private const string ExtraCols = ", g.NAME AS NhomKhachHang, nv.NAME AS NhanVienPhuTrach";

    public async Task<(IReadOnlyList<CustomerDto> Items, long Total)> SearchAsync(
        CustomerListQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "c.W_DELETED = 0" };
        var dp = new DynamicParameters();

        if (q.ActiveOnly == true) where.Add("c.STATUS = 30");
        if (!string.IsNullOrWhiteSpace(q.GroupId)) { where.Add("c.DNHOMKHACHHANGID = @g"); dp.Add("@g", q.GroupId); }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(c.NAME LIKE @s OR c.MAKHACH LIKE @s OR c.DIENTHOAI LIKE @s OR c.MASOTHUE LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }

        var whereSql = string.Join(" AND ", where);
        var sort = q.Sort?.Trim() switch
        {
            "MAKHACH" => "c.MAKHACH",
            "DIENTHOAI" => "c.DIENTHOAI",
            _ => "c.NAME"
        };

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM DKHACHHANG c WHERE {whereSql}", dp);
        var rows = await conn.QueryAsync<CustomerDto>(
            $"""
            SELECT {ColSql}{ExtraCols} FROM DKHACHHANG c {JoinSql}
            WHERE {whereSql}
            ORDER BY {sort} ASC
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }));

        return (rows.ToList(), total);
    }

    public async Task<CustomerDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<CustomerDto>(
            $"""
            SELECT {ColSql}{ExtraCols} FROM DKHACHHANG c {JoinSql}
            WHERE c.ID = @id AND c.W_DELETED = 0
            """, new { id });
    }

    public async Task<IReadOnlyList<CustomerGroupDto>> GetGroupsAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<CustomerGroupDto>(
            "SELECT ID, NAME, STATUS FROM DNHOMKHACHHANG WHERE STATUS = 30 ORDER BY NAME");
        return rows.ToList();
    }

    // ---------- Phase 4 CRUD (web-origin) ----------

    public async Task<bool> MaKhachExistsAsync(string maKhach, string? excludeId = null, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var n = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM DKHACHHANG WHERE MAKHACH = @m AND W_DELETED = 0 AND (@x IS NULL OR ID <> @x)",
            new { m = maKhach, x = excludeId });
        return n > 0;
    }

    public async Task<string> InsertAsync(CustomerCreateRequest r, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        var now = DateTime.Now;
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO DKHACHHANG
            (ID, NAME, STATUS, USERCREATEDID, TIMECREATED, MAKHACH,
             DIACHI, DIACHIGIAOHANG, DIENTHOAI, EMAIL, MASOTHUE, NGUOIDAIDIEN, CHUCVU, FACEBOOK,
             DNHOMKHACHHANGID, SONGAYCHONO, DIEMTICHLUYBANDAU,
             W_SYNC_AT, W_SYNC_REV, W_ORIGIN, W_ROW_HASH, W_DELETED)
            VALUES
            (@id, @name, 30, @uc, @tc, @makh,
             @diachi, @diachigh, @dienthoai, @email, @masothue, @nguoidd, @chucvu, @facebook,
             @nhom, @songay, @diem,
             NOW(3), 0, 'WEB', NULL, 0)
            """, new
            {
                id, r.Name, uc = userId, tc = now,
                makh = EmptyToNull(r.MaKhach),
                diachi = EmptyToNull(r.DiaChi),
                diachigh = EmptyToNull(r.DiaChiGiaoHang),
                dthoai = EmptyToNull(r.DienThoai),
                email = EmptyToNull(r.Email),
                masothue = EmptyToNull(r.MaSoThue),
                nguoidd = EmptyToNull(r.NguoiDaiDien),
                chucvu = EmptyToNull(r.ChucVu),
                facebook = EmptyToNull(r.Facebook),
                nhom = EmptyToNull(r.NhomKhachHangId),
                r.SongayChoNo,
                r.DiemTichLuy
            });
        return id;
    }

    public async Task<int> UpdateAsync(string id, CustomerUpdateRequest r, string userId, CancellationToken ct = default)
    {
        var sets = new List<string>();
        var dp = new DynamicParameters(); dp.Add("@id", id);

        void Set(string col, string prm, object? value)
        {
            if (value is null) return;
            sets.Add($"{col} = @{col}");
            dp.Add($"@{col}", value is string s ? EmptyToNull(s) : value);
        }

        Set("NAME", "NAME", r.Name);
        Set("DIACHI", "DIACHI", r.DiaChi);
        Set("DIACHIGIAOHANG", "DIACHIGIAOHANG", r.DiaChiGiaoHang);
        Set("DIENTHOAI", "DIENTHOAI", r.DienThoai);
        Set("EMAIL", "EMAIL", r.Email);
        Set("MASOTHUE", "MASOTHUE", r.MaSoThue);
        Set("NGUOIDAIDIEN", "NGUOIDAIDIEN", r.NguoiDaiDien);
        Set("CHUCVU", "CHUCVU", r.ChucVu);
        Set("FACEBOOK", "FACEBOOK", r.Facebook);
        Set("DNHOMKHACHHANGID", "NHOM", r.NhomKhachHangId);
        Set("SONGAYCHONO", "SONGAYCHONO", r.SongayChoNo);
        Set("DIEMTICHLUYBANDAU", "DIEMTICHLUY", r.DiemTichLuy);
        Set("STATUS", "STATUS", r.Status);

        if (sets.Count == 0) return 0;
        var sql = $"""
            UPDATE DKHACHHANG
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
            UPDATE DKHACHHANG
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

/// <summary>DTO nhÃ³m khÃ¡ch hÃ ng (DNHOMKHACHHANG).</summary>
public sealed class CustomerGroupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Status { get; set; }
}
