namespace TechPos.Shared;

// =====================================================================
// Phase 5.3 — Nhân sự (mục 5.3):
//   Danh mục nhân viên (DNHANVIEN) · Ca làm việc (DCALAMVIEC) ·
//   Chấm công (TBANGLUONGCHITIET) · Tính lương (TBANGLUONG + TONGHOP) ·
//   Thưởng phạt (TTHUONGPHAT + DLYDOTHUONGPHAT) · Giấy giới thiệu (TGIAYGIOITHIEU)
// Ghi mirror MySQL với W_ORIGIN='WEB' (quy tắc vàng 1 — Firebird là master).
// Tạm ứng lương: TTHUCHI.LATAMUNG=1 (chi cho nhân viên) — tính lương trừ vào TAMUNG.
// =====================================================================

/// <summary>DTO nhân viên (DNHANVIEN).</summary>
public sealed class EmployeeDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DiaChi { get; set; }
    public string? DienThoai { get; set; }
    /// <summary>Cách tính lương (desktop CACHTINHLUONG).</summary>
    public int CachTinhLuong { get; set; }
    public decimal LuongCa { get; set; }
    public decimal LuongThang { get; set; }
    public decimal PhuCap { get; set; }
    public bool NghiThu7 { get; set; }
    public bool NghiChuNhat { get; set; }
    public string? Note { get; set; }
    public int Status { get; set; }
    public DateTime? TimeCreated { get; set; }
    public DateTime? TimeModified { get; set; }
}

public sealed record EmployeeListQuery(
    string? Search = null,
    bool? ActiveOnly = true,
    int Page = 1,
    int PageSize = 20,
    string Sort = "NAME");

public sealed record EmployeeCreateRequest(
    string Name,
    string? DiaChi,
    string? DienThoai,
    int CachTinhLuong = 0,          // 0 = theo ca, 1 = theo tháng (desktop)
    decimal LuongCa = 0,
    decimal LuongThang = 0,
    decimal PhuCap = 0,
    bool NghiThu7 = false,
    bool NghiChuNhat = true,
    string? Note = null);

/// <summary>Sửa nhân viên (partial — null = giữ nguyên).</summary>
public sealed record EmployeeUpdateRequest(
    string? Name,
    string? DiaChi,
    string? DienThoai,
    int? CachTinhLuong,
    decimal? LuongCa,
    decimal? LuongThang,
    decimal? PhuCap,
    bool? NghiThu7,
    bool? NghiChuNhat,
    string? Note,
    int? Status);

/// <summary>Ca làm việc (DCALAMVIEC) — TILELUONG = tỉ lệ lương ca.</summary>
public sealed class ShiftDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal TileLuong { get; set; }
    public string? Note { get; set; }
}

// ---------- Chấm công (TBANGLUONGCHITIET) ----------

public sealed record AttendanceQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? NhanVienId = null,
    int Page = 1,
    int PageSize = 50);

/// <summary>1 dòng chấm công = 1 nhân viên đi 1 ca trong 1 ngày (TRANGTHAI đã xác minh qua UI desktop).</summary>
public sealed class AttendanceDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime Ngay { get; set; }
    public string NhanVienId { get; set; } = string.Empty;
    public string? TenNhanVien { get; set; }
    public string? CaLamViecId { get; set; }
    public string? TenCa { get; set; }
    public decimal TileLuong { get; set; }
    public int TrangThai { get; set; }
}

/// <summary>Ghi chấm công — 1 nhân viên, 1 ngày, 1 ca.</summary>
public sealed record AttendanceCreateRequest(
    string NhanVienId,
    DateTime Ngay,
    string CaLamViecId,
    int TrangThai = 1);

// ---------- Thưởng phạt (TTHUONGPHAT) ----------

public sealed record BonusPenaltyQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? NhanVienId = null,
    int Page = 1,
    int PageSize = 50);

public sealed class BonusPenaltyDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime? Ngay { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NhanVienId { get; set; }
    public string? TenNhanVien { get; set; }
    public string? LyDo { get; set; }
    public decimal Thuong { get; set; }
    public decimal Phat { get; set; }
    public string? Note { get; set; }
    public DateTime? TimeCreated { get; set; }
}

public sealed record BonusPenaltyCreateRequest(
    string NhanVienId,
    decimal Thuong = 0,
    decimal Phat = 0,
    string? LyDoId = null,
    string? GhiChu = null,
    DateTime? Ngay = null);

// ---------- Tính lương (TBANGLUONG + TBANGLUONGTONGHOP) ----------

/// <summary>Yêu cầu tính lương 1 tháng cho toàn bộ nhân viên.</summary>
public sealed record PayrollCreateRequest(
    int Thang,
    int Nam,
    string? GhiChu = null);

/// <summary>Dòng tổng hợp lương 1 nhân viên 1 tháng (TBANGLUONGTONGHOP).</summary>
public sealed class PayrollLineDto
{
    public string Id { get; set; } = string.Empty;
    public string NhanVienId { get; set; } = string.Empty;
    public string? TenNhanVien { get; set; }
    public int CachTinhLuong { get; set; }
    public decimal LuongCa { get; set; }
    public decimal LuongThang { get; set; }
    public decimal Thuong { get; set; }
    public decimal Phat { get; set; }
    public decimal TamUng { get; set; }
    public decimal PhuCap { get; set; }
    public decimal TongLuong { get; set; }
    public decimal ThucNhan { get; set; }
    public int SoCong { get; set; }
}

/// <summary>Kết quả 1 bảng lương tháng.</summary>
public sealed class PayrollDto
{
    public string Id { get; set; } = string.Empty;
    public string SoPhieu { get; set; } = string.Empty;
    public int Thang { get; set; }
    public int Nam { get; set; }
    public DateTime? Ngay { get; set; }
    public string? Note { get; set; }
    public IReadOnlyList<PayrollLineDto> Lines { get; set; } = [];
    public decimal TongThucNhan { get; set; }
}

public sealed record PayrollListQuery(int Page = 1, int PageSize = 20);

public sealed class PayrollSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string SoPhieu { get; set; } = string.Empty;
    public int Thang { get; set; }
    public int Nam { get; set; }
    public DateTime? Ngay { get; set; }
    public string? Note { get; set; }
    public int SoNhanVien { get; set; }
    public decimal TongThucNhan { get; set; }
}

/// <summary>Tạm ứng lương 1 nhân viên — ghi TTHUCHI.LATAMUNG=1 (mục 5.3, DOI-CHIEU §2.7).</summary>
public sealed record SalaryAdvanceRequest(
    string NhanVienId,
    decimal Tien,
    string? GhiChu = null);

public sealed record SalaryAdvanceResult(string Id, string SoPhieu, decimal Tien);

// ---------- Giấy giới thiệu (TGIAYGIOITHIEU) ----------

public sealed record IntroLetterQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20);

public sealed class IntroLetterDto
{
    public string Id { get; set; } = string.Empty;
    public string SoPhieu { get; set; } = string.Empty;
    public DateTime? Ngay { get; set; }
    public DateTime? HieuLucDen { get; set; }
    public string? KinhGui { get; set; }
    public string? DiaChi { get; set; }
    public string? NguoiLienHe { get; set; }
    public string? OngBa { get; set; }
    public string? DuocCuDen { get; set; }
    public string? VeViec { get; set; }
    public string? Note { get; set; }
    public DateTime? TimeCreated { get; set; }
}

public sealed record IntroLetterCreateRequest(
    DateTime? Ngay,
    DateTime? HieuLucDen,
    string? KinhGui,
    string? DiaChi,
    string? NguoiLienHe,
    string OngBa,
    string DuocCuDen,
    string? VeViec,
    string? GhiChu = null);
