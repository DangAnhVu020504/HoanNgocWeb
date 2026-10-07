namespace TechPos.Shared;

/// <summary>Dòng hàng trong giỏ POS bán lẻ (mục 4.1).</summary>
public sealed record PosCartItemDto(
    string MatHangId,
    string TenMatHang,
    string? DonViTinh,
    decimal DonGia,
    decimal SoLuong,
    decimal TileGiamGia = 0,
    decimal TienGiamGia = 0);

/// <summary>Yêu cầu tạo hoá đơn bán lẻ.</summary>
public sealed record PosSaleRequest(
    string KhoId,
    string? KhachHangId,
    string? NhanVienId,
    IReadOnlyList<PosSaleLine> Lines,
    decimal TienGiamGia = 0,
    decimal TileGiamGia = 0,
    string? GhiChu = null);

public sealed record PosPaymentLine(string PhuongThuc, decimal SoTien, string? Note = null);

public sealed record PosSaleLine(
    string MatHangId,
    string? KhoHangId,
    decimal SoLuong,
    decimal DonGia,
    decimal TileGiamGia = 0,
    decimal GiamTheoTien = 0,
    string? Note = null);

/// <summary>Kết quả tạo hoá đơn bán lẻ.</summary>
public sealed record PosSaleResult(
    string DonHangId,
    string SoPhieu,
    decimal TienHang,
    decimal TienGiamGia,
    decimal TongCong,
    decimal KhachDua,
    decimal TraLai,
    decimal ConNo);

/// <summary>Tổng tiền hoá đơn bán lẻ (PosCalculator output).</summary>
public sealed record SaleTotals(
    decimal TienHang,
    decimal TienGiamGia,
    decimal TongCong,
    decimal KhachDua,
    decimal TraLai,
    decimal ConNo);

// ===================== Phase 4.2 — Bán sỉ =====================
// Bán sỉ dùng lại PosSaleRequest/PosSaleLine/PosSaleResult/PosCalculator —
// chỉ khác DOC_TYPE='BAN_SI' (W_DOC_TYPE_MAP) + tiền tố số phiếu BS (BS26/00001).

/// <summary>Yêu cầu tạo phiếu khách trả lại hàng (mục 4.3) — hàng về kho, tiền trả lại khách.</summary>
public sealed record CustomerReturnRequest(
    string KhoNhanId,
    string? KhachHangId,
    string? NhanVienId,
    IReadOnlyList<CustomerReturnLine> Lines,
    decimal TienGiamGia = 0,
    decimal TienTraLai = 0,
    string? GhiChu = null);

public sealed record CustomerReturnLine(
    string MatHangId,
    decimal SoLuong,
    decimal DonGia,
    string? Note = null);

/// <summary>Kết quả tạo phiếu khách trả lại.</summary>
public sealed record CustomerReturnResult(
    string DonHangId,
    string SoPhieu,
    decimal TienHang,
    decimal TienGiamGia,
    decimal TongCong,
    decimal TienTraLai);

/// <summary>Query danh sách hoá đơn bán lẻ web.</summary>
public sealed record PosSaleListQuery(
    string? Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>DTO hoá đơn bán lẻ (từ W_POS_SALE join TDONHANG).</summary>
public sealed class PosSaleDto
{
    public string Id { get; set; } = string.Empty;
    public string SoPhieu { get; set; } = string.Empty;
    public string DonHangId { get; set; } = string.Empty;
    public DateTime Ngay { get; set; }
    public string? KhachHang { get; set; }
    public string? NhanVien { get; set; }
    public string? Kho { get; set; }
    public decimal TienHang { get; set; }
    public decimal TienGiamGia { get; set; }
    public decimal TongCong { get; set; }
    public decimal KhachDua { get; set; }
    public decimal TraLai { get; set; }
    public decimal ConNo { get; set; }
    public int SoDong { get; set; }
    public DateTime TimeCreated { get; set; }
}

public sealed class PosSaleDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string SoPhieu { get; set; } = string.Empty;
    public DateTime Ngay { get; set; }
    public string? KhachHang { get; set; }
    public string? Kho { get; set; }
    public decimal TienHang { get; set; }
    public decimal TienGiamGia { get; set; }
    public decimal TongCong { get; set; }
    public decimal KhachDua { get; set; }
    public decimal TraLai { get; set; }
    public decimal ConNo { get; set; }
    public IReadOnlyList<PosSaleLineDto> Lines { get; set; } = [];
}

public sealed class PosSaleLineDto
{
    public string MatHangId { get; set; } = string.Empty;
    public string? MatHang { get; set; }
    public string? DonViTinh { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
}
