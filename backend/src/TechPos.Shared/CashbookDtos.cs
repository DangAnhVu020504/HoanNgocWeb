namespace TechPos.Shared;

// =====================================================================
// Phase 5.1 — Quỹ: lý do thu chi, phiếu thu/chi (TTHUCHI), tồn quỹ
// Phase 5.2 — Công nợ: KH (TDONHANG.CONNO) / NCC (TDONHANG.CONLAI)
// =====================================================================

/// <summary>DTO lý do thu chi (DLYDOTHUCHI) — LALYDOTHU = đúng là lý do thu.</summary>
public sealed class CashReasonDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool LaLyDoThu { get; set; }
}

/// <summary>Đối tượng phiếu thu/chi.</summary>
public static class CashTargetNames
{
    public const string KhachHang = "KH";
    public const string NhaCungCap = "NCC";
}

/// <summary>Phân bổ tiền phiếu vào từng hóa đơn — giảm CONNO (thu KH) / CONLAI (chi NCC).</summary>
public sealed record CashVoucherAllocation(string DonHangId, decimal SoTien);

/// <summary>
/// Yêu cầu tạo phiếu thu/chi (mục 5.1). Allocations chỉ áp dụng THU_KH (trả nợ KH)
/// và CHI_NCC (trả NCC) trên chứng từ W_ORIGIN='WEB'.
/// </summary>
public sealed record CashVoucherRequest(
    string DoiTuong,               // KH | NCC
    string? KhachHangId,
    string? NhaCungCapId,
    string? NhanVienId,
    string? LyDoId,
    decimal Tien,
    bool ChuyenKhoan = false,
    string? GhiChu = null,
    IReadOnlyList<CashVoucherAllocation>? Allocations = null);

public sealed record CashVoucherResult(
    string Id, string SoPhieu, decimal Tien, decimal DaGanNo);

/// <summary>Query danh sách phiếu thu/chi.</summary>
public sealed record CashVoucherListQuery(
    bool? IsThu = null,             // true = chỉ phiếu thu, false = chỉ chi, null = tất cả
    string? Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>DTO phiếu thu/chi (TTHUCHI).</summary>
public sealed class CashVoucherDto
{
    public string Id { get; set; } = string.Empty;
    public string SoPhieu { get; set; } = string.Empty;
    public DateTime? Ngay { get; set; }
    public int Loai { get; set; }                  // 0 = thu, 1 = chi (đã xác minh §4)
    public string? TenDoiTuong { get; set; }
    public string? DienGiai { get; set; }
    public decimal Thu { get; set; }
    public decimal Chi { get; set; }
    public bool ChuyenKhoan { get; set; }
    public bool LaPhieuThuCongNo { get; set; }
    public string? GhiChu { get; set; }
    public string? KhachHang { get; set; }
    public string? NhaCungCap { get; set; }
    public string? LyDo { get; set; }
    public DateTime? TimeCreated { get; set; }
}

/// <summary>Query tồn quỹ theo kỳ.</summary>
public sealed record CashBalanceQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null);

/// <summary>Tồn quỹ: tiền mặt (CHUYENKHOAN=0) tách riêng chuyển khoản.</summary>
public sealed class CashBalanceDto
{
    public decimal TongThu { get; set; }
    public decimal TongChi { get; set; }
    public decimal TonQuy { get; set; }            // tiền mặt: thu − chi
    public decimal TongThuChuyenKhoan { get; set; }
    public decimal TongChiChuyenKhoan { get; set; }
    public decimal TonChuyenKhoan { get; set; }
}

// ---------- Phase 5.2 — Công nợ ----------

public sealed record CustomerDebtQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 50);

/// <summary>Công nợ 1 khách hàng = SUM(TDONHANG.CONNO) của các hóa đơn chưa trả hết.</summary>
public sealed class CustomerDebtDto
{
    public string KhachHangId { get; set; } = string.Empty;
    public string? MaKhach { get; set; }
    public string TenKhach { get; set; } = string.Empty;
    public string? DienThoai { get; set; }
    public decimal CongNo { get; set; }
    public int SoHoaDon { get; set; }
}

public sealed record SupplierDebtQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 50);

/// <summary>Công nợ 1 NCC = SUM(TDONHANG.CONLAI) của các phiếu nhập chưa trả hết.</summary>
public sealed class SupplierDebtDto
{
    public string NhaCungCapId { get; set; } = string.Empty;
    public string? MaNhaCungCap { get; set; }
    public string TenNhaCungCap { get; set; } = string.Empty;
    public string? DienThoai { get; set; }
    public decimal CongNo { get; set; }
    public int SoPhieu { get; set; }
}