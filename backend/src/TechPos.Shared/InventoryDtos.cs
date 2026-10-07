namespace TechPos.Shared;

/// <summary>DTO tồn kho (OTONKHO join DMATHANG/DKHOHANG) — mục 4.8.</summary>
public sealed class InventoryDto
{
    public string MatHangId { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DonViTinh { get; set; }
    public string? KhoId { get; set; }
    public string? Kho { get; set; }
    public decimal Ton { get; set; }
    public decimal GiaVon { get; set; }
    public decimal GiaBan { get; set; }
    public decimal GiaTriTon { get; set; }
    public int Status { get; set; }
}

public sealed record InventoryListQuery(
    string? Search = null,
    string? KhoId = null,
    bool? NonZeroOnly = true,
    int Page = 1,
    int PageSize = 50,
    string Sort = "NAME");

/// <summary>Dòng thẻ kho (từ W_KHOLEDGER).</summary>
public sealed class LedgerEntryDto
{
    public DateTime EntryDate { get; set; }
    public string? SoPhieu { get; set; }
    public string? DocType { get; set; }
    public string? DienGiai { get; set; }
    public decimal SlNhap { get; set; }
    public decimal SlXuat { get; set; }
    public decimal TonLuyKe { get; set; }
    public decimal? GiaVon { get; set; }
}

public sealed record LedgerQuery(
    string MatHangId,
    string? KhoId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 50);

/// <summary>Yêu cầu tạo phiếu nhập kho (mục 4.4).</summary>
public sealed record ReceiptRequest(
    string KhoNhapId,
    string? NhaCungCapId,
    string? NhanVienId,
    IReadOnlyList<ReceiptLine> Lines,
    decimal TienGiamGia = 0,
    decimal TienThue = 0,
    decimal PhiVanChuyen = 0,
    decimal DaThanhToan = 0,
    string? GhiChu = null);

public sealed record ReceiptLine(
    string MatHangId,
    decimal SlNhap,
    decimal DonGia,
    string? Note = null);

/// <summary>Kết quả tạo phiếu nhập kho.</summary>
public sealed record ReceiptResult(
    string DonHangId,
    string SoPhieu,
    decimal TienHang,
    decimal TienGiamGia,
    decimal TongCong,
    decimal DaThanhToan,
    decimal ConLai);

/// <summary>Yêu cầu tạo phiếu kiểm kê (mục 4.7) — SL hệ thống lấy từ OTONKHO, client chỉ gửi SL thực tế.</summary>
public sealed record StocktakeRequest(
    string KhoId,
    IReadOnlyList<StocktakeLine> Lines,
    string? GhiChu = null);

public sealed record StocktakeLine(
    string MatHangId,
    decimal SlThucTe);

/// <summary>Kết quả tạo phiếu kiểm kê — 1 dòng = 1 chênh lệch đã xử lý (SLNhập(+)/SLXuất(−) theo mẫu KTL).</summary>
public sealed record StocktakeResult(
    string DonHangId,
    string SoPhieu,
    decimal TienTang,
    decimal TienGiam,
    int LinesAdjusted);

// ===================== Phase 4.5 — Xuất kho =====================

/// <summary>Yêu cầu tạo phiếu xuất kho (mục 4.5) — xuất nội bộ, không qua bán hàng.</summary>
public sealed record StockIssueRequest(
    string KhoXuatId,
    string? NhanVienId,
    string? DienGiai,
    IReadOnlyList<StockIssueLine> Lines,
    string? GhiChu = null);

public sealed record StockIssueLine(
    string MatHangId,
    decimal SoLuong,
    decimal DonGia,
    string? Note = null);

/// <summary>Kết quả tạo phiếu xuất kho.</summary>
public sealed record StockIssueResult(
    string DonHangId,
    string SoPhieu,
    decimal TienHang,
    int SoDong);

// ===================== Phase 4.6 — Chuyển kho =====================

/// <summary>Yêu cầu tạo phiếu chuyển kho (mục 4.6) — kho xuất → kho nhập.</summary>
public sealed record StockTransferRequest(
    string KhoXuatId,
    string KhoNhapId,
    string? NhanVienXuatId,
    string? NhanVienNhapId,
    IReadOnlyList<StockTransferLine> Lines,
    string? GhiChu = null);

public sealed record StockTransferLine(
    string MatHangId,
    decimal SoLuong,
    string? Note = null);

/// <summary>Kết quả tạo phiếu chuyển kho.</summary>
public sealed record StockTransferResult(
    string DonHangId,
    string SoPhieu,
    int SoDong,
    decimal TongSoLuong);

// ===================== Phase 4.8 — Tồn tất cả các kho =====================

/// <summary>Tồn 1 mặt hàng tại 1 kho (ô của ma trận tồn tất cả các kho).</summary>
public sealed class WarehouseStockCell
{
    public string KhoId { get; set; } = string.Empty;
    public string? Kho { get; set; }
    public decimal Ton { get; set; }
}

/// <summary>DTO tồn tất cả các kho — mỗi dòng 1 mặt hàng, tồn chi tiết theo từng kho.</summary>
public sealed class AllWarehouseStockDto
{
    public string MatHangId { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DonViTinh { get; set; }
    public decimal TongTon { get; set; }
    public decimal GiaVon { get; set; }
    public decimal GiaTriTon { get; set; }
    public List<WarehouseStockCell> Stocks { get; set; } = [];
}

public sealed record AllWarehouseStockQuery(
    string? Search = null,
    bool? NonZeroOnly = true,
    int Page = 1,
    int PageSize = 50);

// ===================== Phase 4.8 — Tính lại giá vốn =====================

/// <summary>
/// Phương pháp tính giá vốn (dialog desktop "Tính lại giá vốn"):
/// FIFO (nhập trước xuất trước) · BQ_LIEN_HOAN (bình quân gia quyền liên hoàn)
/// · BQ_CUOI_KY (bình quân cuối kỳ) · DICH_DANH (đích danh — thủ công, chưa hỗ trợ web).
/// </summary>
public static class CostMethodNames
{
    public const string Fifo = "FIFO";
    public const string MovingAverage = "BQ_LIEN_HOAN";
    public const string PeriodicAverage = "BQ_CUOI_KY";
    public const string Specific = "DICH_DANH";
}

/// <summary>Yêu cầu tính lại giá vốn từ ngày, theo phương pháp.</summary>
public sealed record CostRecalcRequest(
    string PhuongPhap,
    DateTime TuNgay);

/// <summary>Kết quả tính lại giá vốn.</summary>
public sealed record CostRecalcResult(
    string PhuongPhap,
    DateTime TuNgay,
    int SoMatHang,
    int SoDongXuat,
    decimal TongGiaTriXuat);

/// <summary>1 dòng ledger chuẩn hoá cho tính giá vốn: SL nhập, SL xuất, đơn giá nhập.</summary>
public sealed record CostMovement(
    string LedgerId,
    decimal SlNhap,
    decimal SlXuat,
    decimal GiaVonNhap);

// ===================== Phase 4.5 — Xuất trả NCC =====================

/// <summary>Yêu cầu tạo phiếu xuất trả lại nhà cung cấp (mục 4.5).</summary>
public sealed record SupplierReturnRequest(
    string KhoXuatId,
    string NhaCungCapId,
    string? NhanVienId,
    IReadOnlyList<SupplierReturnLine> Lines,
    decimal TienGiamGia = 0,
    decimal DaThanhToan = 0,
    string? GhiChu = null);

public sealed record SupplierReturnLine(
    string MatHangId,
    decimal SoLuong,
    decimal DonGia,
    string? Note = null);

/// <summary>Kết quả tạo phiếu xuất trả NCC.</summary>
public sealed record SupplierReturnResult(
    string DonHangId,
    string SoPhieu,
    decimal TienHang,
    decimal TienGiamGia,
    decimal TongCong,
    decimal DaThanhToan,
    decimal ConLai);
