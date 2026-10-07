using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository Report Center (Phase 6) — READ ONLY trên mirror MySQL:
///   SalesByDay (TDONHANG bán hàng), SalesByItem (TDONHANGCHITIET),
///   CashByDay/CashByReason (TTHUCHI), Xnt (W_KHOLEDGER),
///   PayrollMonth (TBANGLUONG + TONGHOP), GetInvoice, GetCashVoucherPrint.
/// Bán hàng = phiếu có DKHOXUATID (xuất từ kho bán) — không hard-code LOAI (quy tắc vàng 5).
/// </summary>
public sealed class ReportRepository
{
    private readonly MySqlConnectionFactory _factory;
    public ReportRepository(MySqlConnectionFactory factory) => _factory = factory;

    // ---------- Bán hàng theo ngày ----------
    public async Task<IReadOnlyList<SalesByDayRow>> SalesByDayAsync(ReportQuery q, CancellationToken ct = default)
    {
        var dp = new DynamicParameters();
        var dateFilter = BuildDateFilter("h.NGAY", q, dp);

        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<SalesByDayRow>(
            $"""
            SELECT DATE(h.NGAY) AS Ngay,
                   COUNT(*) AS SoHoaDon,
                   SUM(COALESCE(h.TIENHANG,0)) AS TienHang,
                   SUM(COALESCE(h.TIENGIAMGIA,0)) AS TienGiamGia,
                   SUM(COALESCE(h.TONGCONG,0)) AS TongCong,
                   SUM(COALESCE(h.CONNO,0)) AS ConNo
            FROM TDONHANG h
            WHERE h.W_DELETED = 0 {dateFilter}
              AND h.DKHOXUATID IS NOT NULL AND h.DKHONHAPID IS NULL
              AND h.LOAI IN (2, 3)   -- chỉ bán lẻ (2) + bán sỉ (3): XK(6)/XT(8) cũng có kho xuất nhưng không phải doanh thu
            GROUP BY DATE(h.NGAY)
            ORDER BY Ngay
            """, dp)).ToList();
    }

    // ---------- Mặt hàng bán chạy ----------
    public async Task<IReadOnlyList<SalesByItemRow>> SalesByItemAsync(ReportQuery q, CancellationToken ct = default)
    {
        var dp = new DynamicParameters();
        var dateFilter = BuildDateFilter("h.NGAY", q, dp);
        var topSql = q.Top > 0 ? "LIMIT @top" : "";
        if (q.Top > 0) dp.Add("@top", Math.Clamp(q.Top, 1, 500));

        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<SalesByItemRow>(
            $"""
            SELECT c.DMATHANGID AS MatHangId, m.CODE, m.NAME, u.NAME AS DonViTinh,
                   SUM(COALESCE(c.SLXUAT,0) - COALESCE(c.SLNHAP,0)) AS SoLuong,
                   SUM(COALESCE(c.THANHTIEN,0)) AS ThanhTien
            FROM TDONHANGCHITIET c
            JOIN TDONHANG h ON h.ID = c.TDONHANGID AND h.W_DELETED = 0
            LEFT JOIN DMATHANG m ON m.ID = c.DMATHANGID
            LEFT JOIN DDONVITINH u ON u.ID = m.DDONVITINHID
            WHERE c.W_DELETED = 0 {dateFilter}
              AND h.LOAI IN (2, 3, 4)   -- chỉ bán lẻ/bán sỉ/trả lại KH: PN/XK/KTL không phải hàng bán
            GROUP BY c.DMATHANGID, m.CODE, m.NAME, u.NAME
            HAVING SoLuong <> 0 OR ThanhTien <> 0
            ORDER BY ThanhTien DESC
            {topSql}
            """, dp)).ToList();
    }

    // ---------- Thu chi theo ngày ----------
    public async Task<IReadOnlyList<CashByDayRow>> CashByDayAsync(ReportQuery q, CancellationToken ct = default)
    {
        var dp = new DynamicParameters();
        var dateFilter = BuildDateFilter("NGAY", q, dp);

        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<CashByDayRow>(
            $"""
            SELECT DATE(NGAY) AS Ngay, SUM(COALESCE(THU,0)) AS Thu, SUM(COALESCE(CHI,0)) AS Chi
            FROM TTHUCHI
            WHERE W_DELETED = 0 {dateFilter}
            GROUP BY DATE(NGAY)
            ORDER BY Ngay
            """, dp)).ToList();
    }

    // ---------- Thu chi theo lý do ----------
    public async Task<IReadOnlyList<CashByReasonRow>> CashByReasonAsync(ReportQuery q, CancellationToken ct = default)
    {
        var dp = new DynamicParameters();
        var dateFilter = BuildDateFilter("t.NGAY", q, dp);

        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<CashByReasonRow>(
            $"""
            SELECT COALESCE(l.NAME, '(chưa phân loại)') AS LyDo,
                   SUM(COALESCE(t.THU,0)) AS Thu,
                   SUM(COALESCE(t.CHI,0)) AS Chi
            FROM TTHUCHI t
            LEFT JOIN DLYDOTHUCHI l ON l.ID = t.DLYDOTHUCHIID
            WHERE t.W_DELETED = 0 {dateFilter}
            GROUP BY l.NAME
            ORDER BY Chi DESC, Thu DESC
            """, dp)).ToList();
    }

    // ---------- Xuất – nhập – tồn (W_KHOLEDGER) ----------
    public async Task<IReadOnlyList<XntRow>> XntAsync(ReportQuery q, CancellationToken ct = default)
    {
        var dp = new DynamicParameters();
        var khoFilter = "";
        if (!string.IsNullOrWhiteSpace(q.KhoId)) { khoFilter = "AND l.DKhoHangId = @kho"; dp.Add("@kho", q.KhoId); }
        var hasFrom = q.FromDate.HasValue;
        var hasTo = q.ToDate.HasValue;
        if (hasFrom) dp.Add("@from", q.FromDate!.Value.Date);
        if (hasTo) dp.Add("@to", q.ToDate!.Value.Date);

        var openingExpr = hasFrom
            ? "SUM(CASE WHEN l.ENTRY_DATE < @from THEN l.SL_NHAP - l.SL_XUAT ELSE 0 END)"
            : "0";
        var periodCond = PeriodCondition(hasFrom, hasTo);
        var nhapExpr = periodCond is null ? "SUM(l.SL_NHAP)" : $"SUM(CASE WHEN {periodCond} THEN l.SL_NHAP ELSE 0 END)";
        var xuatExpr = periodCond is null ? "SUM(l.SL_XUAT)" : $"SUM(CASE WHEN {periodCond} THEN l.SL_XUAT ELSE 0 END)";

        await using var conn = await _factory.OpenAsync(ct);
        var rows = (await conn.QueryAsync<XntRow>(
            $"""
            SELECT l.DMathangId AS MatHangId, m.CODE, m.NAME, u.NAME AS DonViTinh,
                   {openingExpr} AS TonDauKy,
                   {nhapExpr} AS SlNhap,
                   {xuatExpr} AS SlXuat,
                   COALESCE(m.GIAVON,0) AS GiaVon
            FROM W_KHOLEDGER l
            LEFT JOIN DMATHANG m ON m.ID = l.DMathangId
            LEFT JOIN DDONVITINH u ON u.ID = m.DDONVITINHID
            WHERE l.W_ORIGIN IN ('FB','WEB') {khoFilter}
            GROUP BY l.DMathangId, m.CODE, m.NAME, u.NAME, m.GIAVON
            ORDER BY m.NAME
            """, dp)).ToList();

        foreach (var r in rows)
        {
            r.TonCuoi = r.TonDauKy + r.SlNhap - r.SlXuat;
            r.GiaTriTon = r.TonCuoi * r.GiaVon;
        }
        return rows;
    }

    private static string? PeriodCondition(bool hasFrom, bool hasTo) => (hasFrom, hasTo) switch
    {
        (true, true) => "l.ENTRY_DATE >= @from AND l.ENTRY_DATE < DATE_ADD(@to, INTERVAL 1 DAY)",
        (true, false) => "l.ENTRY_DATE >= @from",
        (false, true) => "l.ENTRY_DATE < DATE_ADD(@to, INTERVAL 1 DAY)",
        _ => null
    };

    // ---------- Bảng lương tháng ----------
    public async Task<PayrollDto?> PayrollMonthAsync(int thang, int nam, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var head = await conn.QueryFirstOrDefaultAsync<PayrollHeadRow>(
            "SELECT ID AS Id, NAME AS SoPhieu, THANG AS Thang, NAM AS Nam, NOTE AS Note " +
            "FROM TBANGLUONG WHERE THANG = @th AND NAM = @nam AND W_DELETED = 0 LIMIT 1",
            new { th = thang, nam });
        if (head is null) return null;

        var lines = (await conn.QueryAsync<PayrollLineDto>(
            """
            SELECT t.ID AS Id, t.DNHANVIENID AS NhanVienId, nv.NAME AS TenNhanVien,
                   COALESCE(nv.CACHTINHLUONG,0) AS CachTinhLuong,
                   COALESCE(t.LUONGCA,0) AS LuongCa, COALESCE(t.LUONGTHANG,0) AS LuongThang,
                   COALESCE(t.THUONG,0) AS Thuong, COALESCE(t.PHAT,0) AS Phat,
                   COALESCE(t.TAMUNG,0) AS TamUng, COALESCE(t.PHUCAP,0) AS PhuCap,
                   COALESCE(t.TONGCONG,0) AS TongLuong, COALESCE(t.THUCNHAN,0) AS ThucNhan,
                   COALESCE(t.SOCONG,0) AS SoCong
            FROM TBANGLUONGTONGHOP t
            LEFT JOIN DNHANVIEN nv ON nv.ID = t.DNHANVIENID
            WHERE t.TBANGLUONGID = @id AND t.W_DELETED = 0
            ORDER BY TenNhanVien
            """, new { id = head.Id })).ToList();

        return new PayrollDto
        {
            Id = head.Id, SoPhieu = head.SoPhieu, Thang = head.Thang, Nam = head.Nam,
            Note = head.Note, Lines = lines, TongThucNhan = lines.Sum(l => l.ThucNhan)
        };
    }

    // ---------- Hoá đơn in ----------
    public async Task<InvoiceDto?> GetInvoiceAsync(string donHangId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var head = await conn.QueryFirstOrDefaultAsync<InvoiceDto>(
            """
            SELECT h.ID AS DonHangId, h.NAME AS SoPhieu, h.NGAY AS Ngay,
                   k.NAME AS KhachHang, k.DIACHI AS DiaChi, k.DIENTHOAI AS DienThoai,
                   nv.NAME AS NhanVien, kx.NAME AS Kho,
                   COALESCE(h.TIENHANG,0) AS TienHang, COALESCE(h.TIENGIAMGIA,0) AS TienGiamGia,
                   COALESCE(h.TONGCONG,0) AS TongCong, COALESCE(h.KHACHDUA,0) AS KhachDua,
                   COALESCE(h.TRALAI,0) AS TraLai, COALESCE(h.CONNO,0) AS ConNo, h.NOTE AS GhiChu
            FROM TDONHANG h
            LEFT JOIN DKHACHHANG k ON k.ID = h.DKHACHHANGID
            LEFT JOIN DNHANVIEN nv ON nv.ID = h.DNHANVIENXUATID
            LEFT JOIN DKHOHANG kx ON kx.ID = h.DKHOXUATID
            WHERE h.ID = @id AND h.W_DELETED = 0
            """, new { id = donHangId });
        if (head is null) return null;

        head.Lines = (await conn.QueryAsync<InvoiceLineDto>(
            """
            SELECT m.NAME AS MatHang, u.NAME AS DonViTinh,
                   COALESCE(c.SLXUAT,0) - COALESCE(c.SLNHAP,0) AS SoLuong,
                   COALESCE(c.DONGIA,0) AS DonGia, COALESCE(c.THANHTIEN,0) AS ThanhTien
            FROM TDONHANGCHITIET c
            LEFT JOIN DMATHANG m ON m.ID = c.DMATHANGID
            LEFT JOIN DDONVITINH u ON u.ID = c.DDONVITINHID
            WHERE c.TDONHANGID = @id AND c.W_DELETED = 0
            ORDER BY c.TIMECREATED
            """, new { id = donHangId })).ToList();
        return head;
    }

    // ---------- Phiếu thu/chi in ----------
    public async Task<CashVoucherPrintDto?> GetCashVoucherPrintAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<CashVoucherPrintDto>(
            """
            SELECT t.NAME AS SoPhieu, t.NGAY AS Ngay, t.TENDOITUONG AS TenDoiTuong,
                   t.DIENGIAI AS DienGiai, COALESCE(t.THU,0) AS Thu, COALESCE(t.CHI,0) AS Chi,
                   l.NAME AS LyDo, t.NOTE AS GhiChu
            FROM TTHUCHI t
            LEFT JOIN DLYDOTHUCHI l ON l.ID = t.DLYDOTHUCHIID
            WHERE t.ID = @id AND t.W_DELETED = 0
            """, new { id });
    }

    // ---------- Phiếu kho in (nhập/xuất/chuyển/kiểm kê/xuất trả NCC) ----------
    public async Task<StockVoucherPrintDto?> GetStockVoucherAsync(string donHangId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var head = await conn.QueryFirstOrDefaultAsync<StockVoucherHeadRow>(
            """
            SELECT h.NAME AS SoPhieu, h.NGAY AS Ngay, h.LOAI AS Loai, h.MODE AS ModeVal,
                   kn.NAME AS KhoNhap, kx.NAME AS KhoXuat,
                   COALESCE(NULLIF(ncc.NAME,''), NULLIF(kh.NAME,'')) AS DoiTac, h.DIENGIAI AS DienGiai,
                   COALESCE(h.TIENHANG,0) AS TienHang, COALESCE(h.TIENGIAMGIA,0) AS TienGiamGia,
                   COALESCE(h.TONGCONG,0) AS TongCong, h.NOTE AS GhiChu
            FROM TDONHANG h
            LEFT JOIN DKHOHANG kn ON kn.ID = h.DKHONHAPID
            LEFT JOIN DKHOHANG kx ON kx.ID = h.DKHOXUATID
            LEFT JOIN DNHACUNGCAP ncc ON ncc.ID = h.DNHACUNGCAPID
            LEFT JOIN DKHACHHANG kh ON kh.ID = h.DKHACHHANGID
            WHERE h.ID = @id AND h.W_DELETED = 0
            """, new { id = donHangId });
        if (head is null) return null;

        var lines = (await conn.QueryAsync<StockVoucherLineDto>(
            """
            SELECT m.NAME AS MatHang, u.NAME AS DonViTinh,
                   COALESCE(c.SLNHAP,0) AS SlNhap, COALESCE(c.SLXUAT,0) AS SlXuat,
                   COALESCE(c.DONGIA,0) AS DonGia, COALESCE(c.THANHTIEN,0) AS ThanhTien
            FROM TDONHANGCHITIET c
            LEFT JOIN DMATHANG m ON m.ID = c.DMATHANGID
            LEFT JOIN DDONVITINH u ON u.ID = c.DDONVITINHID
            WHERE c.TDONHANGID = @id AND c.W_DELETED = 0
            ORDER BY c.TIMECREATED
            """, new { id = donHangId })).ToList();

        return new StockVoucherPrintDto
        {
            DocType = DocTypeResolver.ResolveByWarehouses(head.Loai, head.ModeVal,
                head.KhoNhap != null, head.KhoXuat != null),
            SoPhieu = head.SoPhieu, Ngay = head.Ngay,
            KhoNhap = head.KhoNhap, KhoXuat = head.KhoXuat, DoiTac = head.DoiTac,
            DienGiai = head.DienGiai, GhiChu = head.GhiChu,
            TienHang = head.TienHang, TienGiamGia = head.TienGiamGia, TongCong = head.TongCong,
            Lines = lines
        };
    }

    // ---------- Công nợ khách hàng (tổng hợp cho báo cáo) ----------
    public async Task<IReadOnlyList<CustomerDebtRow>> CustomerDebtsAsync(ReportQuery q, CancellationToken ct = default)
    {
        var dp = new DynamicParameters();
        var search = "";
        if (!string.IsNullOrWhiteSpace(q.KhoId)) { search = " AND (k.NAME LIKE @s OR k.MAKHACH LIKE @s)"; dp.Add("@s", $"%{q.KhoId.Trim()}%"); }

        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<CustomerDebtRow>(
            $"""
            SELECT h.DKHACHHANGID AS KhachHangId, k.MAKHACH AS MaKhach, k.NAME AS TenKhach,
                   k.DIENTHOAI AS DienThoai, COALESCE(k.SONGAYCHONO,0) AS SongayChoNo,
                   SUM(CASE WHEN COALESCE(h.CONNO,0) <> 0 THEN 1 ELSE 0 END) AS SoHoaDon,
                   SUM(COALESCE(h.CONNO,0)) AS CongNo
            FROM TDONHANG h
            JOIN DKHACHHANG k ON k.ID = h.DKHACHHANGID
            WHERE h.W_DELETED = 0 {search}
            GROUP BY h.DKHACHHANGID, k.MAKHACH, k.NAME, k.DIENTHOAI, k.SONGAYCHONO
            HAVING SUM(COALESCE(h.CONNO,0)) <> 0
            ORDER BY CongNo DESC
            """, dp)).ToList();
    }

    // ---------- Công nợ nhà cung cấp (tổng hợp cho báo cáo) ----------
    public async Task<IReadOnlyList<SupplierDebtRow>> SupplierDebtsAsync(ReportQuery q, CancellationToken ct = default)
    {
        var dp = new DynamicParameters();
        var search = "";
        if (!string.IsNullOrWhiteSpace(q.KhoId)) { search = " AND (n.NAME LIKE @s OR n.MANHACUNGCAP LIKE @s)"; dp.Add("@s", $"%{q.KhoId.Trim()}%"); }

        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<SupplierDebtRow>(
            $"""
            SELECT h.DNHACUNGCAPID AS NhaCungCapId, n.MANHACUNGCAP AS MaNhaCungCap, n.NAME AS TenNhaCungCap,
                   n.DIENTHOAI AS DienThoai,
                   SUM(CASE WHEN COALESCE(h.CONLAI,0) <> 0 THEN 1 ELSE 0 END) AS SoPhieu,
                   SUM(COALESCE(h.CONLAI,0)) AS CongNo
            FROM TDONHANG h
            JOIN DNHACUNGCAP n ON n.ID = h.DNHACUNGCAPID
            WHERE h.W_DELETED = 0 {search}
            GROUP BY h.DNHACUNGCAPID, n.MANHACUNGCAP, n.NAME, n.DIENTHOAI
            HAVING SUM(COALESCE(h.CONLAI,0)) <> 0
            ORDER BY CongNo DESC
            """, dp)).ToList();
    }

    // ---------- Tồn quỹ theo ngày (luỹ kế) ----------
    public async Task<IReadOnlyList<CashBalanceRow>> CashBalanceAsync(ReportQuery q, CancellationToken ct = default)
    {
        var dp = new DynamicParameters();
        var dateFilter = BuildDateFilter("NGAY", q, dp);

        await using var conn = await _factory.OpenAsync(ct);
        var rows = (await conn.QueryAsync<CashBalanceRow>(
            $"""
            SELECT DATE(NGAY) AS Ngay, SUM(COALESCE(THU,0)) AS Thu, SUM(COALESCE(CHI,0)) AS Chi
            FROM TTHUCHI
            WHERE W_DELETED = 0 {dateFilter}
            GROUP BY DATE(NGAY)
            ORDER BY Ngay
            """, dp)).ToList();

        var running = 0m;
        foreach (var r in rows)
        {
            running += r.Thu - r.Chi;
            r.TonCuoiNgay = running;
        }
        return rows;
    }

    // ---------- Kết quả kinh doanh trong kỳ ----------
    public async Task<KqkdRow> KqkdAsync(ReportQuery q, CancellationToken ct = default)
    {
        var dp = new DynamicParameters();
        var dateFilter = BuildDateFilter("h.NGAY", q, dp);

        await using var conn = await _factory.OpenAsync(ct);
        var revenue = await conn.QueryFirstOrDefaultAsync<KqkdRow>(
            $"""
            SELECT COALESCE(SUM(h.TIENHANG),0) AS DoanhThu,
                   COALESCE(SUM(h.TIENGIAMGIA),0) AS GiamGia
            FROM TDONHANG h
            WHERE h.W_DELETED = 0 {dateFilter}
              AND h.DKHOXUATID IS NOT NULL AND h.DKHONHAPID IS NULL
              AND h.LOAI IN (2, 3)   -- chỉ bán lẻ (2) + bán sỉ (3): XK(6)/XT(8) cũng có kho xuất nhưng không phải doanh thu
            """, dp) ?? new KqkdRow();

        var dp2 = new DynamicParameters();
        var dateFilter2 = BuildDateFilter("l.ENTRY_DATE", q, dp2);
        var cost = await conn.QueryFirstOrDefaultAsync<KqkdRow>(
            $"""
            SELECT COALESCE(SUM(l.GIA_VON * l.SL_XUAT),0) AS GiaVon
            FROM W_KHOLEDGER l
            WHERE 1=1 {dateFilter2} AND l.SL_XUAT > 0
            """, dp2) ?? new KqkdRow();

        var dp3 = new DynamicParameters();
        var dateFilter3 = BuildDateFilter("NGAY", q, dp3);
        var expense = await conn.QueryFirstOrDefaultAsync<KqkdRow>(
            $"""
            SELECT COALESCE(SUM(CHI),0) AS ChiPhi, COALESCE(SUM(THU),0) AS GiamGia
            FROM TTHUCHI
            WHERE W_DELETED = 0 {dateFilter3}
            """, dp3) ?? new KqkdRow();

        var row = new KqkdRow
        {
            DoanhThu = revenue.DoanhThu,
            GiamGia = revenue.GiamGia + cost.GiamGia,   // (chiếm chỗ: THU phiếu thu khác bán hàng)
            GiaVon = cost.GiaVon,
            ChiPhi = expense.ChiPhi
        };
        row.LoiNhuan = row.DoanhThu - row.GiamGia - row.GiaVon - row.ChiPhi;
        return row;
    }

    private static string BuildDateFilter(string column, ReportQuery q, DynamicParameters dp)
    {
        var parts = new List<string>();
        if (q.FromDate.HasValue) { parts.Add($"{column} >= @from"); dp.Add("@from", q.FromDate.Value.Date); }
        if (q.ToDate.HasValue) { parts.Add($"{column} < DATE_ADD(@to, INTERVAL 1 DAY)"); dp.Add("@to", q.ToDate.Value.Date); }
        return parts.Count > 0 ? " AND " + string.Join(" AND ", parts) : "";
    }

    private sealed class PayrollHeadRow
    {
        public string Id { get; set; } = string.Empty;
        public string SoPhieu { get; set; } = string.Empty;
        public int Thang { get; set; }
        public int Nam { get; set; }
        public string? Note { get; set; }
    }

    private sealed class StockVoucherHeadRow
    {
        public string SoPhieu { get; set; } = string.Empty;
        public DateTime? Ngay { get; set; }
        public int? Loai { get; set; }
        public int? ModeVal { get; set; }
        public string? KhoNhap { get; set; }
        public string? KhoXuat { get; set; }
        public string? DoiTac { get; set; }
        public string? DienGiai { get; set; }
        public string? GhiChu { get; set; }
        public decimal TienHang { get; set; }
        public decimal TienGiamGia { get; set; }
        public decimal TongCong { get; set; }
    }
}
