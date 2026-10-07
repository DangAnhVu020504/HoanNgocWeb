using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository Nhân sự (mục 5.3) — DNHANVIEN · DCALAMVIEC · TBANGLUONGCHITIET (chấm công) ·
/// TTHUONGPHAT (thưởng phạt) · TBANGLUONG + TBANGLUONGTONGHOP (tính lương) ·
/// TGIAYGIOITHIEU (giấy giới thiệu) · TTHUCHI.LATAMUNG=1 (tạm ứng lương).
/// Ghi mirror MySQL với W_ORIGIN='WEB' (quy tắc vàng 1 — Firebird là master ở GĐ1).
/// </summary>
public sealed class HrRepository
{
    private readonly MySqlConnectionFactory _factory;
    public HrRepository(MySqlConnectionFactory factory) => _factory = factory;

    // ---------- Nhân viên (DNHANVIEN) ----------

    private const string EmployeeCols =
        "e.ID, e.NAME, e.NOTE, e.STATUS, e.DIACHI AS DiaChi, e.DIENTHOAI AS DienThoai, " +
        "COALESCE(e.CACHTINHLUONG,0) AS CachTinhLuong, COALESCE(e.LUONGCA,0) AS LuongCa, " +
        "COALESCE(e.LUONGTHANG,0) AS LuongThang, COALESCE(e.PHUCAP,0) AS PhuCap, " +
        "COALESCE(e.NGHITHU7,0) <> 0 AS NghiThu7, COALESCE(e.NGHICHUNHAT,0) <> 0 AS NghiChuNhat, " +
        "e.TIMECREATED, e.TIMEMODIFIED";

    public async Task<(IReadOnlyList<EmployeeDto> Items, long Total)> SearchEmployeesAsync(
        EmployeeListQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "e.W_DELETED = 0" };
        var dp = new DynamicParameters();
        if (q.ActiveOnly == true) where.Add("e.STATUS = 30");
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(e.NAME LIKE @s OR e.DIENTHOAI LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }
        var whereSql = string.Join(" AND ", where);

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM DNHANVIEN e WHERE {whereSql}", dp);
        var rows = (await conn.QueryAsync<EmployeeDto>(
            $"SELECT {EmployeeCols} FROM DNHANVIEN e WHERE {whereSql} " +
            "ORDER BY e.NAME ASC LIMIT @limit OFFSET @offset",
            Merge(dp, new { limit = pageSize, offset }))).ToList();
        return (rows, total);
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<EmployeeDto>(
            $"SELECT {EmployeeCols} FROM DNHANVIEN e WHERE e.ID = @id AND e.W_DELETED = 0", new { id });
    }

    public async Task<string> InsertEmployeeAsync(EmployeeCreateRequest r, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO DNHANVIEN
            (ID, NAME, NOTE, STATUS, USERCREATEDID, TIMECREATED,
             CACHTINHLUONG, LUONGCA, LUONGTHANG, PHUCAP, NGHITHU7, NGHICHUNHAT,
             DIACHI, DIENTHOAI, W_SYNC_AT, W_SYNC_REV, W_ORIGIN, W_DELETED)
            VALUES
            (@id, @name, @note, 30, @uc, @tc, @ctl, @lca, @lthang, @phucap, @t7, @cn,
             @diachi, @dienthoai, NOW(3), 0, 'WEB', 0)
            """, new
            {
                id,
                r.Name,
                note = EmptyToNull(r.Note),
                uc = userId,
                tc = DateTime.Now,
                ctl = r.CachTinhLuong,
                lca = r.LuongCa,
                lthang = r.LuongThang,
                phucap = r.PhuCap,
                t7 = r.NghiThu7 ? 1 : 0,
                cn = r.NghiChuNhat ? 1 : 0,
                diachi = EmptyToNull(r.DiaChi),
                dienthoai = EmptyToNull(r.DienThoai)
            });
        return id;
    }

    public async Task<int> UpdateEmployeeAsync(string id, EmployeeUpdateRequest r, string userId, CancellationToken ct = default)
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
        Set("DIACHI", r.DiaChi);
        Set("DIENTHOAI", r.DienThoai);
        Set("CACHTINHLUONG", r.CachTinhLuong);
        Set("LUONGCA", r.LuongCa);
        Set("LUONGTHANG", r.LuongThang);
        Set("PHUCAP", r.PhuCap);
        Set("STATUS", r.Status);
        if (r.NghiThu7 is not null) Set("NGHITHU7", r.NghiThu7.Value ? 1 : 0);
        if (r.NghiChuNhat is not null) Set("NGHICHUNHAT", r.NghiChuNhat.Value ? 1 : 0);

        if (sets.Count == 0) return 0;
        var sql = $"""
            UPDATE DNHANVIEN
            SET {string.Join(", ", sets)}, USERMODIFIEDID = @uid, TIMEMODIFIED = @tm, W_SYNC_REV = W_SYNC_REV + 1
            WHERE ID = @id AND W_DELETED = 0
            """;
        dp.Add("@uid", userId);
        dp.Add("@tm", DateTime.Now);
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteAsync(sql, dp);
    }

    public async Task<int> SoftDeleteEmployeeAsync(string id, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteAsync(
            """
            UPDATE DNHANVIEN
            SET W_DELETED = 1, W_DELETED_AT = @now, USERMODIFIEDID = @uid, TIMEMODIFIED = @tm, W_SYNC_REV = W_SYNC_REV + 1
            WHERE ID = @id AND W_DELETED = 0
            """, new { id, uid = userId, tm = DateTime.Now, now = DateTime.UtcNow });
    }

    // ---------- Ca làm việc (DCALAMVIEC) ----------

    public async Task<IReadOnlyList<ShiftDto>> GetShiftsAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<ShiftDto>(
            "SELECT ID, NAME, COALESCE(TILELUONG,0) AS TileLuong, NOTE " +
            "FROM DCALAMVIEC WHERE W_DELETED = 0 AND STATUS = 30 ORDER BY NAME")).ToList();
    }

    // ---------- Chấm công (TBANGLUONGCHITIET) ----------

    public async Task<(IReadOnlyList<AttendanceDto> Items, long Total)> SearchAttendanceAsync(
        AttendanceQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "cc.W_DELETED = 0" };
        var dp = new DynamicParameters();
        if (q.FromDate.HasValue) { where.Add("cc.NGAY >= @from"); dp.Add("@from", q.FromDate.Value.Date); }
        if (q.ToDate.HasValue) { where.Add("cc.NGAY < DATE_ADD(@to, INTERVAL 1 DAY)"); dp.Add("@to", q.ToDate.Value.Date); }
        if (!string.IsNullOrWhiteSpace(q.NhanVienId)) { where.Add("cc.DNHANVIENID = @nv"); dp.Add("@nv", q.NhanVienId); }
        var whereSql = string.Join(" AND ", where);

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM TBANGLUONGCHITIET cc WHERE {whereSql}", dp);
        var rows = (await conn.QueryAsync<AttendanceDto>(
            $"""
            SELECT cc.ID AS Id, cc.NGAY AS Ngay, cc.DNHANVIENID AS NhanVienId, nv.NAME AS TenNhanVien,
                   cc.DCALAMVIECID AS CaLamViecId, ca.NAME AS TenCa,
                   COALESCE(ca.TILELUONG,0) AS TileLuong, COALESCE(cc.TRANGTHAI,0) AS TrangThai
            FROM TBANGLUONGCHITIET cc
            LEFT JOIN DNHANVIEN nv ON nv.ID = cc.DNHANVIENID
            LEFT JOIN DCALAMVIEC ca ON ca.ID = cc.DCALAMVIECID
            WHERE {whereSql}
            ORDER BY cc.NGAY DESC, nv.NAME
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }))).ToList();
        return (rows, total);
    }

    public async Task<string> InsertAttendanceAsync(AttendanceCreateRequest r, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO TBANGLUONGCHITIET
            (ID, STATUS, USERCREATEDID, TIMECREATED, DNHANVIENID, DCALAMVIECID, TRANGTHAI, NGAY,
             W_SYNC_AT, W_SYNC_REV, W_ORIGIN, W_DELETED)
            VALUES (@id, 30, @uc, @tc, @nv, @ca, @tt, @ngay, NOW(3), 0, 'WEB', 0)
            """, new
            {
                id, uc = userId, tc = DateTime.Now,
                nv = r.NhanVienId, ca = r.CaLamViecId, tt = r.TrangThai,
                ngay = r.Ngay.Date
            });
        return id;
    }

    public async Task<bool> AttendanceExistsAsync(string nhanVienId, DateTime ngay, string caLamViecId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM TBANGLUONGCHITIET WHERE DNHANVIENID=@nv AND DCALAMVIECID=@ca " +
            "AND NGAY=@ngay AND W_DELETED=0",
            new { nv = nhanVienId, ca = caLamViecId, ngay = ngay.Date }) > 0;
    }

    // ---------- Thưởng phạt (TTHUONGPHAT) ----------

    public async Task<IReadOnlyList<LookupItem>> GetBonusReasonsAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<LookupItem>(
            "SELECT ID AS Id, NAME AS Name FROM DLYDOTHUONGPHAT WHERE W_DELETED = 0 AND STATUS = 30 ORDER BY NAME")).ToList();
    }

    public async Task<(IReadOnlyList<BonusPenaltyDto> Items, long Total)> SearchBonusAsync(
        BonusPenaltyQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "tp.W_DELETED = 0" };
        var dp = new DynamicParameters();
        if (q.FromDate.HasValue) { where.Add("tp.NGAY >= @from"); dp.Add("@from", q.FromDate.Value.Date); }
        if (q.ToDate.HasValue) { where.Add("tp.NGAY < DATE_ADD(@to, INTERVAL 1 DAY)"); dp.Add("@to", q.ToDate.Value.Date); }
        if (!string.IsNullOrWhiteSpace(q.NhanVienId)) { where.Add("tp.DNHANVIENID = @nv"); dp.Add("@nv", q.NhanVienId); }
        var whereSql = string.Join(" AND ", where);

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM TTHUONGPHAT tp WHERE {whereSql}", dp);
        var rows = (await conn.QueryAsync<BonusPenaltyDto>(
            $"""
            SELECT tp.ID AS Id, tp.NGAY AS Ngay, tp.NAME AS Name, tp.DNHANVIENID AS NhanVienId,
                   nv.NAME AS TenNhanVien, l.NAME AS LyDo,
                   COALESCE(tp.THUONG,0) AS Thuong, COALESCE(tp.PHAT,0) AS Phat,
                   tp.NOTE AS Note, tp.TIMECREATED AS TimeCreated
            FROM TTHUONGPHAT tp
            LEFT JOIN DNHANVIEN nv ON nv.ID = tp.DNHANVIENID
            LEFT JOIN DLYDOTHUONGPHAT l ON l.ID = tp.DLYDOTHUONGPHATID
            WHERE {whereSql}
            ORDER BY tp.NGAY DESC, tp.TIMECREATED DESC
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }))).ToList();
        return (rows, total);
    }

    public async Task<string> InsertBonusAsync(BonusPenaltyCreateRequest r, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        var tenNv = await conn.ExecuteScalarAsync<string>(
            "SELECT NAME FROM DNHANVIEN WHERE ID = @id AND W_DELETED = 0", new { id = r.NhanVienId })
            ?? throw new NotFoundException($"Không tìm thấy nhân viên {r.NhanVienId}");
        var ngay = r.Ngay ?? DateTime.Now;
        await conn.ExecuteAsync(
            """
            INSERT INTO TTHUONGPHAT
            (ID, NAME, NOTE, STATUS, NGAY, USERCREATEDID, TIMECREATED, DNHANVIENID,
             THUONG, PHAT, DLYDOTHUONGPHATID, W_SYNC_AT, W_SYNC_REV, W_ORIGIN, W_DELETED)
            VALUES
            (@id, @name, @note, 30, @ngay, @uc, @tc, @nv, @thuong, @phat, @lydo,
             NOW(3), 0, 'WEB', 0)
            """, new
            {
                id,
                name = $"{(r.Thuong > 0 ? "Thưởng" : "Phạt")} {tenNv}".Trim(),
                note = EmptyToNull(r.GhiChu),
                ngay,
                uc = userId,
                tc = DateTime.Now,
                nv = r.NhanVienId,
                thuong = r.Thuong,
                phat = r.Phat,
                lydo = EmptyToNull(r.LyDoId)
            });
        return id;
    }

    // ---------- Tính lương (TBANGLUONG + TBANGLUONGTONGHOP) ----------

    public async Task<string> CreatePayrollAsync(PayrollCreateRequest r, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // 1 bảng lương/tháng — chặn tạo trùng (đã có thì báo conflict)
            var existing = await conn.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM TBANGLUONG WHERE THANG=@th AND NAM=@nam AND W_DELETED=0",
                new { th = r.Thang, nam = r.Nam }, tx);
            if (existing > 0)
                throw new ConflictException(
                    $"Bảng lương tháng {r.Thang:00}/{r.Nam} đã tồn tại — không tạo trùng.");

            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, "BL", ct);
            var id = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;

            await conn.ExecuteAsync(
                """
                INSERT INTO TBANGLUONG
                (ID, NAME, NOTE, STATUS, NGAY, USERCREATEDID, TIMECREATED, THANG, NAM,
                 W_SYNC_AT, W_SYNC_REV, W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @ngay, @uc, @tc, @thang, @nam, NOW(3), 0, 'WEB', 0)
                """, new
                {
                    id, sophieu = soPhieu, note = EmptyToNull(r.GhiChu), ngay,
                    uc = userId, tc = ngay, thang = r.Thang, nam = r.Nam
                }, tx);

            // Tổng hợp lương từng nhân viên từ chấm công + thưởng phạt (desktop công thức, §2.7)
            //   LUONGCA    = tổng (SL ca × TILELUONG × LUONGCA nhân viên) — nhân viên tính lương theo ca
            //   LUONGTHANG = LUONGTHANG danh mục — nhân viên tính lương theo tháng
            //   THUONG/PHAT/TAMUNG/PHUCAP từ TTHUONGPHAT + TTHUCHI.LATAMUNG + DNHANVIEN.PHUCAP
            //   TONGLUONG  = LUONGCA + LUONGTHANG + THUONG + PHUCAP
            //   THUCNHAN   = TONGLUONG − PHAT − TAMUNG
            await conn.ExecuteAsync(
                """
                INSERT INTO TBANGLUONGTONGHOP
                (ID, NOTE, STATUS, USERCREATEDID, TIMECREATED, DNHANVIENID,
                 LUONGCA, LUONGTHANG, THUONG, PHAT, TAMUNG, PHUCAP, TONGLUONG, THUCNHAN,
                 CACHTINHLUONG, TBANGLUONGID, W_SYNC_AT, W_SYNC_REV, W_ORIGIN, W_DELETED)
                SELECT UUID(), @note, 30, @uc, @tc, e.ID,
                    COALESCE(x.luongca, 0),
                    COALESCE(e.LUONGTHANG, 0),
                    COALESCE(b.thuong, 0),
                    COALESCE(b.phat, 0),
                    COALESCE(a.tamung, 0),
                    COALESCE(e.PHUCAP, 0),
                    COALESCE(x.luongca, 0) + COALESCE(e.LUONGTHANG, 0) + COALESCE(b.thuong, 0) + COALESCE(e.PHUCAP, 0),
                    COALESCE(x.luongca, 0) + COALESCE(e.LUONGTHANG, 0) + COALESCE(b.thuong, 0) + COALESCE(e.PHUCAP, 0)
                        - COALESCE(b.phat, 0) - COALESCE(a.tamung, 0),
                    COALESCE(e.CACHTINHLUONG, 0),
                    @id, NOW(3), 0, 'WEB', 0
                FROM DNHANVIEN e
                LEFT JOIN (
                    SELECT ct.DNHANVIENID, SUM(ca.TILELUONG / 100.0 * e2.LUONGCA) AS luongca
                    FROM TBANGLUONGCHITIET ct
                    JOIN DCALAMVIEC ca ON ca.ID = ct.DCALAMVIECID
                    JOIN DNHANVIEN e2 ON e2.ID = ct.DNHANVIENID
                    WHERE ct.W_DELETED = 0 AND COALESCE(ct.TRANGTHAI,0) <> 0
                      AND YEAR(ct.NGAY) = @nam AND MONTH(ct.NGAY) = @thang
                    GROUP BY ct.DNHANVIENID
                ) x ON x.DNHANVIENID = e.ID
                LEFT JOIN (
                    SELECT DNHANVIENID, SUM(COALESCE(THUONG,0)) AS thuong, SUM(COALESCE(PHAT,0)) AS phat
                    FROM TTHUONGPHAT
                    WHERE W_DELETED = 0 AND YEAR(NGAY) = @nam AND MONTH(NGAY) = @thang
                    GROUP BY DNHANVIENID
                ) b ON b.DNHANVIENID = e.ID
                LEFT JOIN (
                    SELECT DNHANVIENID, SUM(COALESCE(CHI,0)) AS tamung
                    FROM TTHUCHI
                    WHERE W_DELETED = 0 AND COALESCE(LATAMUNG,0) <> 0 AND DNHANVIENID IS NOT NULL
                      AND YEAR(NGAY) = @nam AND MONTH(NGAY) = @thang
                    GROUP BY DNHANVIENID
                ) a ON a.DNHANVIENID = e.ID
                WHERE e.W_DELETED = 0
                """, new
                {
                    note = EmptyToNull(r.GhiChu),
                    uc = userId,
                    tc = ngay,
                    id,
                    thang = r.Thang,
                    nam = r.Nam
                }, tx);

            await tx.CommitAsync(ct);
            return id;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<(IReadOnlyList<PayrollSummaryDto> Items, long Total)> SearchPayrollsAsync(
        PayrollListQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM TBANGLUONG WHERE W_DELETED = 0");
        var rows = (await conn.QueryAsync<PayrollSummaryDto>(
            $"""
            SELECT b.ID AS Id, b.NAME AS SoPhieu, COALESCE(b.THANG,0) AS Thang, COALESCE(b.NAM,0) AS Nam,
                   b.NGAY AS Ngay, b.NOTE AS Note,
                   COUNT(t.ID) AS SoNhanVien, COALESCE(SUM(t.THUCNHAN),0) AS TongThucNhan
            FROM TBANGLUONG b
            LEFT JOIN TBANGLUONGTONGHOP t ON t.TBANGLUONGID = b.ID AND t.W_DELETED = 0
            WHERE b.W_DELETED = 0
            GROUP BY b.ID, b.NAME, b.THANG, b.NAM, b.NGAY, b.NOTE
            ORDER BY b.NAM DESC, b.THANG DESC
            LIMIT @limit OFFSET @offset
            """, new { limit = pageSize, offset })).ToList();
        return (rows, total);
    }

    public async Task<PayrollDto?> GetPayrollAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var head = await conn.QueryFirstOrDefaultAsync<PayrollHeadRow>(
            "SELECT ID AS Id, NAME AS SoPhieu, COALESCE(THANG,0) AS Thang, COALESCE(NAM,0) AS Nam, " +
            "NGAY AS Ngay, NOTE AS Note FROM TBANGLUONG WHERE ID = @id AND W_DELETED = 0", new { id });
        if (head is null) return null;

        var lines = (await conn.QueryAsync<PayrollLineDto>(
            """
            SELECT t.ID AS Id, t.DNHANVIENID AS NhanVienId, nv.NAME AS TenNhanVien,
                   COALESCE(t.CACHTINHLUONG,0) AS CachTinhLuong,
                   COALESCE(t.LUONGCA,0) AS LuongCa, COALESCE(t.LUONGTHANG,0) AS LuongThang,
                   COALESCE(t.THUONG,0) AS Thuong, COALESCE(t.PHAT,0) AS Phat,
                   COALESCE(t.TAMUNG,0) AS TamUng, COALESCE(t.PHUCAP,0) AS PhuCap,
                   COALESCE(t.TONGLUONG,0) AS TongLuong, COALESCE(t.THUCNHAN,0) AS ThucNhan,
                   0 AS SoCong
            FROM TBANGLUONGTONGHOP t
            LEFT JOIN DNHANVIEN nv ON nv.ID = t.DNHANVIENID
            WHERE t.TBANGLUONGID = @id AND t.W_DELETED = 0
            ORDER BY nv.NAME
            """, new { id })).ToList();

        // Số công: đếm dòng chấm công thực tế trong tháng của bảng lương
        var cong = (await conn.QueryAsync<WorkDayRow>(
            """
            SELECT DNHANVIENID AS NhanVienId, COUNT(*) AS SoCong
            FROM TBANGLUONGCHITIET
            WHERE W_DELETED = 0 AND COALESCE(TRANGTHAI,0) <> 0
              AND YEAR(NGAY) = @nam AND MONTH(NGAY) = @thang
            GROUP BY DNHANVIENID
            """, new { nam = head.Nam, thang = head.Thang })).ToDictionary(r => r.NhanVienId, r => r.SoCong);

        foreach (var line in lines)
            line.SoCong = cong.TryGetValue(line.NhanVienId, out var c) ? c : 0;

        return new PayrollDto
        {
            Id = head.Id,
            SoPhieu = head.SoPhieu,
            Thang = head.Thang,
            Nam = head.Nam,
            Ngay = head.Ngay,
            Note = head.Note,
            Lines = lines,
            TongThucNhan = lines.Sum(l => l.ThucNhan)
        };
    }

    private sealed class PayrollHeadRow
    {
        public string Id { get; set; } = string.Empty;
        public string SoPhieu { get; set; } = string.Empty;
        public int Thang { get; set; }
        public int Nam { get; set; }
        public DateTime? Ngay { get; set; }
        public string? Note { get; set; }
    }

    private sealed class WorkDayRow
    {
        public string NhanVienId { get; set; } = string.Empty;
        public int SoCong { get; set; }
    }

    /// <summary>Tạm ứng lương — ghi TTHUCHI (LOAI=1 chi, LATAMUNG=1, DNHANVIENID).</summary>
    public async Task<SalaryAdvanceResult> CreateSalaryAdvanceAsync(
        SalaryAdvanceRequest r, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var tenNv = await conn.ExecuteScalarAsync<string>(
                "SELECT NAME FROM DNHANVIEN WHERE ID = @id AND W_DELETED = 0",
                new { id = r.NhanVienId }, tx)
                ?? throw new NotFoundException($"Không tìm thấy nhân viên {r.NhanVienId}");

            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, "PC", ct);
            var id = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;

            await conn.ExecuteAsync(
                """
                INSERT INTO TTHUCHI
                (ID, NAME, NOTE, STATUS, NGAY, USERCREATEDID, TIMECREATED,
                 TENDOITUONG, LOAI, LOAIDOITUONG, DIENGIAI, THU, CHI, CHUYENKHOAN,
                 LATAMUNG, LAPHIEUTHUCONGNO, KHONGTHAYDOICONGNO,
                 DNHANVIENID, W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @ngay, @uc, @tc,
                 @tennv, 1, 2, 'Tạm ứng lương', 0, @tien, 0,
                 1, 0, 1, @nv, 'WEB', 0)
                """, new
                {
                    id, sophieu = soPhieu, note = EmptyToNull(r.GhiChu), ngay,
                    uc = userId, tc = ngay, tennv = tenNv, tien = r.Tien, nv = r.NhanVienId
                }, tx);

            await tx.CommitAsync(ct);
            return new SalaryAdvanceResult(id, soPhieu, r.Tien);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ---------- Giấy giới thiệu (TGIAYGIOITHIEU) ----------

    public async Task<(IReadOnlyList<IntroLetterDto> Items, long Total)> SearchIntroLettersAsync(
        IntroLetterQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "g.W_DELETED = 0" };
        var dp = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(g.NAME LIKE @s OR g.ONGBA LIKE @s OR g.DUOCCUDEN LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }
        var whereSql = string.Join(" AND ", where);

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM TGIAYGIOITHIEU g WHERE {whereSql}", dp);
        var rows = (await conn.QueryAsync<IntroLetterDto>(
            $"""
            SELECT g.ID AS Id, g.NAME AS SoPhieu, g.NGAY AS Ngay, g.HIEULUCDEN AS HieuLucDen,
                   g.KINHGUI AS KinhGui, g.DIACHI AS DiaChi, g.NGUOILIENHE AS NguoiLienHe,
                   g.ONGBA AS OngBa, g.DUOCCUDEN AS DuocCuDen, g.VEVIEC AS VeViec,
                   g.NOTE AS Note, g.TIMECREATED AS TimeCreated
            FROM TGIAYGIOITHIEU g
            WHERE {whereSql}
            ORDER BY g.TIMECREATED DESC
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }))).ToList();
        return (rows, total);
    }

    public async Task<IntroLetterDto?> GetIntroLetterAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<IntroLetterDto>(
            """
            SELECT g.ID AS Id, g.NAME AS SoPhieu, g.NGAY AS Ngay, g.HIEULUCDEN AS HieuLucDen,
                   g.KINHGUI AS KinhGui, g.DIACHI AS DiaChi, g.NGUOILIENHE AS NguoiLienHe,
                   g.ONGBA AS OngBa, g.DUOCCUDEN AS DuocCuDen, g.VEVIEC AS VeViec,
                   g.NOTE AS Note, g.TIMECREATED AS TimeCreated
            FROM TGIAYGIOITHIEU g
            WHERE g.ID = @id AND g.W_DELETED = 0
            """, new { id });
    }

    public async Task<string> InsertIntroLetterAsync(IntroLetterCreateRequest r, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, "GS", ct);
            var ngay = r.Ngay ?? DateTime.Now;
            await conn.ExecuteAsync(
                """
                INSERT INTO TGIAYGIOITHIEU
                (ID, NAME, NOTE, STATUS, NGAY, USERCREATEDID, TIMECREATED,
                 HIEULUCDEN, KINHGUI, DIACHI, NGUOILIENHE, ONGBA, DUOCCUDEN, VEVIEC,
                 W_SYNC_AT, W_SYNC_REV, W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @ngay, @uc, @tc,
                 @hieulucden, @kinhgui, @diachi, @nguoilienhe, @ongba, @duoccuden, @veviec,
                 NOW(3), 0, 'WEB', 0)
                """, new
                {
                    id, sophieu = soPhieu, note = EmptyToNull(r.GhiChu), ngay,
                    uc = userId, tc = ngay,
                    hieulucden = (object?)r.HieuLucDen ?? DBNull.Value,
                    kinhgui = EmptyToNull(r.KinhGui) ?? (object)DBNull.Value,
                    diachi = EmptyToNull(r.DiaChi) ?? (object)DBNull.Value,
                    nguoilienhe = EmptyToNull(r.NguoiLienHe) ?? (object)DBNull.Value,
                    ongba = r.OngBa,
                    duoccuden = r.DuocCuDen,
                    veviec = EmptyToNull(r.VeViec) ?? (object)DBNull.Value
                }, tx);
            await tx.CommitAsync(ct);
            return id;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<int> SoftDeleteIntroLetterAsync(string id, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteAsync(
            """
            UPDATE TGIAYGIOITHIEU
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
