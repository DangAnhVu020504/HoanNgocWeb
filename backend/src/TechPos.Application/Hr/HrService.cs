using FluentValidation;
using Microsoft.Extensions.Options;
using TechPos.Application.Catalog;
using TechPos.Data.MySql;
using TechPos.Shared;
using ValidationException = TechPos.Shared.ValidationException;

namespace TechPos.Application.Hr;

/// <summary>Validator tạo/sửa nhân viên (mục 5.3).</summary>
public sealed class EmployeeValidator : AbstractValidator<EmployeeCreateRequest>
{
    public EmployeeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(340).WithMessage("Tên nhân viên bắt buộc");
        RuleFor(x => x)
            .Must(x => x.CachTinhLuong is 0 or 1)
            .WithMessage("Cách tính lương chỉ nhận 0 (theo ca) hoặc 1 (theo tháng)")
            .WithName(nameof(EmployeeCreateRequest.CachTinhLuong));
        RuleFor(x => x.LuongCa).GreaterThanOrEqualTo(0)
            .When(x => x.CachTinhLuong == 0).WithMessage("Lương ca phải ≥ 0");
        RuleFor(x => x.LuongThang).GreaterThanOrEqualTo(0)
            .When(x => x.CachTinhLuong == 1).WithMessage("Lương tháng phải ≥ 0");
        RuleFor(x => x.PhuCap).GreaterThanOrEqualTo(0).WithMessage("Phụ cấp phải ≥ 0");
    }
}

/// <summary>Validator thưởng phạt — nhân viên bắt buộc, tổng thưởng/phát > 0, không thưởng và phạt cùng lúc.</summary>
public sealed class BonusPenaltyValidator : AbstractValidator<BonusPenaltyCreateRequest>
{
    public BonusPenaltyValidator()
    {
        RuleFor(x => x.NhanVienId).NotEmpty().WithMessage("Chưa chọn nhân viên");
        RuleFor(x => x)
            .Must(x => x.Thuong > 0 ^ x.Phat > 0)
            .WithMessage("Chọn hoặc thưởng hoặc phạt, không nhập cả hai")
            .WithName("Loai");
        RuleFor(x => x).Must(x => x.Thuong >= 0 && x.Phat >= 0)
            .WithMessage("Số tiền thưởng/phạt không được âm")
            .WithName("SoTien");
    }
}

/// <summary>Validator tạm ứng lương — tiền > 0.</summary>
public sealed class SalaryAdvanceValidator : AbstractValidator<SalaryAdvanceRequest>
{
    public SalaryAdvanceValidator()
    {
        RuleFor(x => x.NhanVienId).NotEmpty().WithMessage("Chưa chọn nhân viên");
        RuleFor(x => x.Tien).GreaterThan(0).WithMessage("Số tiền tạm ứng phải > 0");
    }
}

/// <summary>Validator giấy giới thiệu — họ tên + nội dung đến đâu bắt buộc.</summary>
public sealed class IntroLetterValidator : AbstractValidator<IntroLetterCreateRequest>
{
    public IntroLetterValidator()
    {
        RuleFor(x => x.OngBa).NotEmpty().MaximumLength(300).WithMessage("Họ tên người được giới thiệu bắt buộc");
        RuleFor(x => x.DuocCuDen).NotEmpty().MaximumLength(300).WithMessage("Nơi đến bắt buộc");
    }
}

/// <summary>
/// Công thức tính lương thuần (không DB) — dùng khi build preview và khi test (mục 5.3, §2.7):
///   TILELUONG trong DCALAMVIEC lưu dạng phần trăm (100 = lương nguyên ca — đã xác minh mirror DEMO)
///   Lương ca    = số công × (tỉ lệ ca / 100) × lương ca của nhân viên
///   Tồng lương  = lương ca + lương tháng + thưởng + phụ cấp
///   Thực nhận   = tổng lương − phạt − tạm ứng
/// </summary>
public static class PayrollCalculator
{
    public static decimal LuongCa(int soCong, decimal tileLuong, decimal luongCaNhanVien)
        => soCong * (tileLuong / 100m) * luongCaNhanVien;

    public static decimal TongLuong(decimal luongCa, decimal luongThang, decimal thuong, decimal phuCap)
        => luongCa + luongThang + thuong + phuCap;

    public static decimal ThucNhan(decimal tongLuong, decimal phat, decimal tamUng)
        => tongLuong - phat - tamUng;
}

/// <summary>Use-case Nhân sự (mục 5.3). KHÔNG chứa SQL.</summary>
public sealed class HrService(HrRepository repo, IOptions<FeatureFlagOptions> flags)
{
    private readonly EmployeeValidator _employeeValidator = new();
    private readonly BonusPenaltyValidator _bonusValidator = new();
    private readonly SalaryAdvanceValidator _advanceValidator = new();
    private readonly IntroLetterValidator _introValidator = new();

    // ---------- Nhân viên ----------

    public async Task<PageResult<EmployeeDto>> SearchEmployeesAsync(EmployeeListQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchEmployeesAsync(q, ct);
        return new PageResult<EmployeeDto>
        {
            Items = items, Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize), Total = total
        };
    }

    public async Task<EmployeeDto> GetEmployeeAsync(string id, CancellationToken ct = default)
    {
        var e = await repo.GetEmployeeByIdAsync(id, ct);
        return e ?? throw new NotFoundException($"Không tìm thấy nhân viên {id}");
    }

    public async Task<CatalogCreateResult> CreateEmployeeAsync(EmployeeCreateRequest req, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        var vr = _employeeValidator.Validate(req);
        if (!vr.IsValid)
            throw new ValidationException("Dữ liệu nhân viên không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        var id = await repo.InsertEmployeeAsync(req, userId, ct);
        return new CatalogCreateResult(id, req.Name);
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(string id, EmployeeUpdateRequest req, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        _ = await GetEmployeeAsync(id, ct);
        var updated = await repo.UpdateEmployeeAsync(id, req, userId, ct);
        if (updated == 0) throw new NotFoundException($"Không tìm thấy nhân viên {id}");
        return await GetEmployeeAsync(id, ct);
    }

    public async Task DeleteEmployeeAsync(string id, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        _ = await GetEmployeeAsync(id, ct);
        var deleted = await repo.SoftDeleteEmployeeAsync(id, userId, ct);
        if (deleted == 0) throw new NotFoundException($"Không tìm thấy nhân viên {id}");
    }

    // ---------- Chấm công ----------

    public async Task<IReadOnlyList<ShiftDto>> GetShiftsAsync(CancellationToken ct = default)
        => await repo.GetShiftsAsync(ct);

    public async Task<PageResult<AttendanceDto>> SearchAttendanceAsync(AttendanceQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchAttendanceAsync(q, ct);
        return new PageResult<AttendanceDto>
        {
            Items = items, Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize), Total = total
        };
    }

    public async Task<AttendanceDto> CreateAttendanceAsync(AttendanceCreateRequest req, string userId, CancellationToken ct = default)
    {
        _ = await GetEmployeeAsync(req.NhanVienId, ct);
        var shifts = await repo.GetShiftsAsync(ct);
        if (shifts.All(s => s.Id != req.CaLamViecId))
            throw new ValidationException("Ca làm việc không tồn tại", []);
        if (req.Ngay.Date > DateTime.Today)
            throw new ValidationException("Không thể chấm công cho tương lai",
                [new ApiError(nameof(req.Ngay), "NGAY_TUONG_LAI", "Ngày chấm công không được ở tương lai")]);
        if (await repo.AttendanceExistsAsync(req.NhanVienId, req.Ngay.Date, req.CaLamViecId, ct))
            throw new ConflictException("Nhân viên này đã chấm ca đó trong ngày — không ghi trùng");
        var id = await repo.InsertAttendanceAsync(req, userId, ct);
        return new AttendanceDto
        {
            Id = id,
            Ngay = req.Ngay.Date,
            NhanVienId = req.NhanVienId,
            CaLamViecId = req.CaLamViecId,
            TrangThai = req.TrangThai
        };
    }

    // ---------- Thưởng phạt ----------

    public async Task<IReadOnlyList<LookupItem>> GetBonusReasonsAsync(CancellationToken ct = default)
        => await repo.GetBonusReasonsAsync(ct);

    public async Task<PageResult<BonusPenaltyDto>> SearchBonusAsync(BonusPenaltyQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchBonusAsync(q, ct);
        return new PageResult<BonusPenaltyDto>
        {
            Items = items, Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize), Total = total
        };
    }

    public async Task<CatalogCreateResult> CreateBonusAsync(BonusPenaltyCreateRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _bonusValidator.Validate(req);
        if (!vr.IsValid)
            throw new ValidationException("Dữ liệu thưởng phạt không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        var id = await repo.InsertBonusAsync(req, userId, ct);
        return new CatalogCreateResult(id, req.NhanVienId);
    }

    // ---------- Tính lương ----------

    public async Task<PageResult<PayrollSummaryDto>> SearchPayrollsAsync(PayrollListQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchPayrollsAsync(q, ct);
        return new PageResult<PayrollSummaryDto>
        {
            Items = items, Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize), Total = total
        };
    }

    public async Task<PayrollDto> GetPayrollAsync(string id, CancellationToken ct = default)
    {
        var p = await repo.GetPayrollAsync(id, ct);
        return p ?? throw new NotFoundException($"Không tìm thấy bảng lương {id}");
    }

    public async Task<PayrollDto> CreatePayrollAsync(PayrollCreateRequest req, string userId, CancellationToken ct = default)
    {
        var now = DateTime.Now;
        if (req.Thang is < 1 or > 12)
            throw new ValidationException("Tháng không hợp lệ (1–12)",
                [new ApiError(nameof(req.Thang), "THANG_KHONG_HOP_LE", "Tháng phải từ 1 đến 12")]);
        if (req.Nam < 2000 || req.Nam > now.Year + 1)
            throw new ValidationException("Năm không hợp lệ",
                [new ApiError(nameof(req.Nam), "NAM_KHONG_HOP_LE", $"Năm phải trong khoảng 2000–{now.Year + 1}")]);
        var id = await repo.CreatePayrollAsync(req, userId, ct);
        return await GetPayrollAsync(id, ct);
    }

    // ---------- Tạm ứng lương ----------

    public async Task<SalaryAdvanceResult> CreateSalaryAdvanceAsync(SalaryAdvanceRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _advanceValidator.Validate(req);
        if (!vr.IsValid)
            throw new ValidationException("Dữ liệu tạm ứng không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        return await repo.CreateSalaryAdvanceAsync(req, userId, ct);
    }

    // ---------- Giấy giới thiệu ----------

    public async Task<PageResult<IntroLetterDto>> SearchIntroLettersAsync(IntroLetterQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchIntroLettersAsync(q, ct);
        return new PageResult<IntroLetterDto>
        {
            Items = items, Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize), Total = total
        };
    }

    public async Task<IntroLetterDto> GetIntroLetterAsync(string id, CancellationToken ct = default)
    {
        var g = await repo.GetIntroLetterAsync(id, ct);
        return g ?? throw new NotFoundException($"Không tìm thấy giấy giới thiệu {id}");
    }

    public async Task<CatalogCreateResult> CreateIntroLetterAsync(IntroLetterCreateRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _introValidator.Validate(req);
        if (!vr.IsValid)
            throw new ValidationException("Dữ liệu giấy giới thiệu không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        var id = await repo.InsertIntroLetterAsync(req, userId, ct);
        return new CatalogCreateResult(id, req.OngBa);
    }

    public async Task DeleteIntroLetterAsync(string id, string userId, CancellationToken ct = default)
    {
        _ = await GetIntroLetterAsync(id, ct);
        var deleted = await repo.SoftDeleteIntroLetterAsync(id, userId, ct);
        if (deleted == 0) throw new NotFoundException($"Không tìm thấy giấy giới thiệu {id}");
    }
}
