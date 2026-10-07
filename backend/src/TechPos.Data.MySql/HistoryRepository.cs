using Dapper;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Lịch sử chứng từ (mục mới): hợp nhất toàn bộ hoá đơn / phiếu / hợp đồng / báo giá...
/// đã tạo trên web (W_ORIGIN='WEB') từ các bảng mirror, kèm tên người tạo (W_USER).
/// Chỉ đọc — không ghi.
/// </summary>
public sealed class HistoryRepository(MySqlConnectionFactory factory)
{
    private const string LoaiBanLe = "Hóa đơn bán lẻ";
    private const string LoaiBanSi = "Hóa đơn bán sỉ";
    private const string LoaiTraLaiKh = "Khách trả lại hàng";

    private static string LoaiFromLoaiMode(int loai, int? mode) => loai switch
    {
        1 => mode == -1 ? LoaiBanLe : "Hóa đơn",
        2 => mode == -1 ? LoaiBanLe : "Hóa đơn",
        3 => LoaiBanSi,
        4 => "Phiếu nhập kho",
        5 => "Phiếu xuất kho",
        6 => "Phiếu chuyển kho",
        7 => "Kiểm kê",
        8 => LoaiTraLaiKh,
        _ => $"Chứng từ (LOAI={loai})"
    };

    public sealed class HistoryItem
    {
        public string Id { get; set; } = string.Empty;
        public string SoPhieu { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public string Nhom { get; set; } = string.Empty;
        public DateTime? Ngay { get; set; }
        public DateTime? TimeCreated { get; set; }
        public string? NguoiTao { get; set; }
        public decimal? Tien { get; set; }
        public string? DienGiai { get; set; }
        public int Status { get; set; }
    }

    private sealed class RawRow
    {
        public string Id { get; set; } = string.Empty;
        public string SoPhieu { get; set; } = string.Empty;
        public int LoaiNum { get; set; }
        public int? Mode { get; set; }
        public string? NguoiTao { get; set; }
        public decimal? Tien { get; set; }
        public string? DienGiai { get; set; }
        public DateTime? Ngay { get; set; }
        public DateTime TimeCreated { get; set; }
        public int Status { get; set; }
    }

    private async Task<IReadOnlyList<HistoryItem>> QueryAsync(string sql, object prm, CancellationToken ct)
    {
        await using var conn = await factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<RawRow>(sql, prm);
        return rows.Select(r => new HistoryItem
        {
            Id = r.Id,
            SoPhieu = r.SoPhieu,
            Loai = r.LoaiNum > 0 ? LoaiFromLoaiMode(r.LoaiNum, r.Mode) : string.Empty,
            NguoiTao = r.NguoiTao,
            Tien = r.Tien,
            DienGiai = r.DienGiai,
            Ngay = r.Ngay,
            TimeCreated = r.TimeCreated,
            Status = r.Status
        }).ToList();
    }

    // Mỗi bảng một nhánh UNION ALL — riêng TDONHANG phân loại theo LOAI/MODE.
    // Cột luôn prefix alias bảng để không ambiguous khi join W_USER.
    private const string SqlUnion = """
        SELECT d.ID AS Id, d.NAME AS SoPhieu, d.LOAI AS LoaiNum, d.`MODE` AS Mode,
               u.USERNAME AS NguoiTao, d.TONGCONG AS Tien, d.DIENGIAI AS DienGiai,
               d.NGAY AS Ngay, d.TIMECREATED AS TimeCreated, d.STATUS AS Status
        FROM TDONHANG d LEFT JOIN W_USER u ON u.ID = d.USERCREATEDID
        WHERE d.W_ORIGIN = 'WEB' AND d.W_DELETED = 0 AND d.NAME LIKE '%26/%'

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, CASE WHEN COALESCE(t.THU,0) > 0 THEN t.THU ELSE t.CHI END,
               t.DIENGIAI, t.NGAY, t.TIMECREATED, t.STATUS
        FROM TTHUCHI t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, t.TONGCONG, NULL, t.NGAY, t.TIMECREATED, t.STATUS
        FROM TBAOGIA t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, t.TONGCONG, NULL, t.NGAY, t.TIMECREATED, t.STATUS
        FROM TDATHANG t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, t.TONGCONG, NULL, t.NGAY, t.TIMECREATED, t.STATUS
        FROM TDATHANGNCC t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, t.TONGCONG, NULL, t.NGAYKY, t.TIMECREATED, t.STATUS
        FROM THOPDONG t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, NULL, NULL, t.NGAY, t.TIMECREATED, t.STATUS
        FROM TGUIHANGCOD t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, NULL, NULL, t.NGAY, t.TIMECREATED, t.STATUS
        FROM TKETQUAGUICOD t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, NULL, NULL, t.NGAY, t.TIMECREATED, t.STATUS
        FROM TGIAOHANGTHEOXE t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, NULL, NULL, t.NGAY, t.TIMECREATED, t.STATUS
        FROM TBANGLUONG t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, NULL, NULL, t.NGAY, t.TIMECREATED, t.STATUS
        FROM TGIAYGIOITHIEU t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0

        UNION ALL

        SELECT t.ID, t.NAME, 0, NULL, u.USERNAME, COALESCE(t.THUONG,0) + COALESCE(t.PHAT,0), NULL, t.NGAY, t.TIMECREATED, t.STATUS
        FROM TTHUONGPHAT t LEFT JOIN W_USER u ON u.ID = t.USERCREATEDID
        WHERE t.W_ORIGIN = 'WEB' AND t.W_DELETED = 0
        """;

    // Phân loại từ prefix số phiếu — nguồn sự thật (TDONHANG đa năng, LOAI trùng nhau giữa các loại)
    private static string LoaiTuPrefix(string prefix, string? dienGiai) => prefix switch
    {
        "BL" => LoaiBanLe,
        "BS" => LoaiBanSi,
        "TL" => LoaiTraLaiKh,
        "PN" => "Phiếu nhập kho",
        "XK" => "Phiếu xuất kho",
        "CK" => "Phiếu chuyển kho",
        "XT" => "Xuất trả NCC",
        "KTL" => "Phiếu kiểm kê",
        "PT" => "Phiếu thu",
        "PC" => dienGiai == "Tạm ứng lương" ? "Tạm ứng lương" : "Phiếu chi",
        "BG" => "Báo giá",
        "DH" => "Đặt hàng KH",
        "DN" => "Đặt mua NCC",
        "HD" => "Hợp đồng",
        "COD" => "Gửi hàng COD",
        "KQ" => "Kết quả COD",
        "XE" => "Giao hàng theo xe",
        "GS" => "Giấy giới thiệu",
        _ => "Chứng từ khác"
    };

    public async Task<(IReadOnlyList<HistoryItem> Items, long Total)> SearchAsync(
        HistoryQuery q, CancellationToken ct = default)
    {
        // WHERE phải bọc ngoài UNION (không HAVING trực tiếp trên union — MySQL không cho alias)
        var where = "";
        var prm = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where = " WHERE SoPhieu LIKE @s OR NguoiTao LIKE @s OR DienGiai LIKE @s";
            prm.Add("s", $"%{q.Search.Trim()}%");
        }
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);

        var listSql = $"SELECT * FROM ({SqlUnion}) h {where} ORDER BY h.TimeCreated DESC LIMIT @limit OFFSET @offset";
        var countSql = $"SELECT COUNT(*) FROM ({SqlUnion}) h {where}";
        prm.Add("limit", pageSize);
        prm.Add("offset", (page - 1) * pageSize);

        await using var conn = await factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(countSql, prm);
        var items = await QueryAsync(listSql, prm, ct);

        // Gán nhóm + loại hiển thị theo số phiếu (prefix là nguồn sự thật — TDONHANG đa năng)
        foreach (var it in items)
        {
            var prefix = it.SoPhieu.StartsWith("COD") || it.SoPhieu.StartsWith("KTL")
                ? it.SoPhieu[..3] : it.SoPhieu[..2];
            it.Nhom = prefix switch
            {
                "BL" => "Bán hàng",
                "BS" => "Bán hàng",
                "TL" => "Bán hàng",
                "PN" => "Kho hàng",
                "XK" => "Kho hàng",
                "CK" => "Kho hàng",
                "XT" => "Kho hàng",
                "KTL" => "Kho hàng",
                "PT" => "Quỹ",
                "PC" => "Quỹ",
                "BG" => "Bán hàng",
                "DH" => "Bán hàng",
                "DN" => "Mua hàng",
                "HD" => "Bán hàng",
                "COD" => "Vận chuyển",
                "KQ" => "Vận chuyển",
                "XE" => "Vận chuyển",
                _ => "Nhân sự"
            };
            if (it.SoPhieu.StartsWith("GS")) it.Nhom = "Nhân sự";
            if (it.Nhom == "Nhân sự" && !it.SoPhieu.StartsWith("GS")) it.Loai = "Bảng lương";
            it.Loai = LoaiTuPrefix(prefix, it.DienGiai);
        }
        return (items, total);
    }
}
