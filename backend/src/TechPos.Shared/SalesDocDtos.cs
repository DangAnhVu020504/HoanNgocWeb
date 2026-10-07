namespace TechPos.Shared;

/// <summary>Mục tra cứu đơn giản (id + name) — đơn vị chuyển phát, xe giao hàng.</summary>
public sealed class LookupItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>Đơn hàng web gắn vào phiếu COD / giao xe (TDONHANG W_ORIGIN='WEB').</summary>
public sealed class WebOrderItem
{
    public string Id { get; set; } = string.Empty;
    public string SoPhieu { get; set; } = string.Empty;
    public DateTime? Ngay { get; set; }
    public decimal TongCong { get; set; }
    public string? KhachHang { get; set; }
}

// =====================================================================
// Phase 4.9 — Báo giá / Đặt hàng KH / Đặt mua NCC / Hợp đồng
// Phase 4.10 — Giao COD / Giao hàng theo xe
// Các chứng từ dùng bảng riêng (không qua TDONHANG), ghi W_ORIGIN='WEB'.
// =====================================================================

/// <summary>Dòng hàng dùng chung cho báo giá / đặt hàng / hợp đồng.</summary>
public sealed record QuoteLine(
    string MatHangId,
    decimal SoLuong,
    decimal DonGia,
    decimal TileGiamGia = 0,
    string? Note = null);

/// <summary>Head-level dùng chung cho báo giá / đặt hàng KH (TBAOGIA/TDATHANG cùng cấu trúc).</summary>
public sealed record CustomerDocRequest(
    string? KhachHangId,
    string? TenKhach,
    string? DiaChi,
    string? DienThoai,
    string? Email,
    IReadOnlyList<QuoteLine> Lines,
    decimal TienThue = 0,
    decimal TienGiamGia = 0,
    decimal PhiVanChuyen = 0,
    string? GhiChu = null);

public sealed record QuoteResult(string Id, string SoPhieu, decimal TienHang, decimal TongCong, int SoDong);
public sealed record OrderResult(string Id, string SoPhieu, decimal TienHang, decimal TongCong, int SoDong);

// ---------- Đặt mua nhà cung cấp (TDATHANGNCC) ----------

public sealed record PurchaseOrderLine(
    string MatHangId,
    decimal SoLuong,
    decimal DonGia,
    decimal TileGiamGia = 0,
    string? Note = null);

public sealed record PurchaseOrderRequest(
    string NhaCungCapId,
    IReadOnlyList<PurchaseOrderLine> Lines,
    decimal TienThue = 0,
    decimal TienGiamGia = 0,
    decimal PhiVanChuyen = 0,
    string? GhiChu = null);

public sealed record PurchaseOrderResult(
    string Id, string SoPhieu, decimal TienHang, decimal TongCong, int SoDong);

// ---------- Hợp đồng (THOPDONG) ----------

public sealed record ContractLine(
    string MatHangId,
    decimal SoLuong,
    decimal DonGia,
    decimal TileGiamGia = 0,
    string? Note = null);

/// <summary>Yêu cầu tạo hợp đồng (mục 4.9) — trạng thái hợp đồng lưu DTRANGTHAIHOPDONGID.</summary>
public sealed record ContractRequest(
    string? KhachHangId,
    string? TenKhach,
    string? NhanVienId,
    string? LoaiHopDongId,
    string? TrangThaiHopDongId,
    DateTime? NgayKy,
    DateTime? TuNgay,
    DateTime? DenNgay,
    IReadOnlyList<ContractLine> Lines,
    decimal DatCoc = 0,
    decimal TienGiamGia = 0,
    string? GhiChu = null);

public sealed record ContractResult(
    string Id, string SoPhieu, decimal TienHang, decimal TongCong, int SoDong);

// ---------- Giao COD (TGUIHANGCOD / TKETQUAGUICOD) ----------

/// <summary>Yêu cầu tạo phiếu gửi hàng COD (mục 4.10). DonHangIds = các đơn web (TDONHANG) đưa vào phiếu.</summary>
public sealed record CodShipmentRequest(
    string DonViChuyenPhatId,
    string? NhanVienId,
    IReadOnlyList<string> DonHangIds,
    string? GhiChu = null);

public sealed record CodShipmentResult(string Id, string SoPhieu, int SoDonHang);

public sealed record CodResultRequest(
    string? DonViChuyenPhatId,
    string? NhanVienId,
    IReadOnlyList<string> DonHangIds,
    string? GhiChu = null);

public sealed record CodResultEntry(string Id, string SoPhieu, int SoDonHang);

/// <summary>Yêu cầu tạo phiếu giao hàng theo xe (mục 4.10).</summary>
public sealed record VehicleDeliveryRequest(
    string XeGiaoHangId,
    string? NhanVienId,
    IReadOnlyList<string> DonHangIds,
    string? GhiChu = null);

public sealed record VehicleDeliveryResult(
    string Id, string SoPhieu, int SoDonHang);