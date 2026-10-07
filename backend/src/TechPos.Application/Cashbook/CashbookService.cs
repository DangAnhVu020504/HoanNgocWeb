using FluentValidation;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Cashbook;

/// <summary>Validator phiếu thu/chi (mục 5.1).</summary>
public sealed class CashVoucherRequestValidator : AbstractValidator<CashVoucherRequest>
{
    public CashVoucherRequestValidator()
    {
        RuleFor(x => x.DoiTuong).NotEmpty()
            .Must(v => v == CashTargetNames.KhachHang || v == CashTargetNames.NhaCungCap)
            .WithMessage("Đối tượng phải là KH hoặc NCC");
        RuleFor(x => x.Tien).GreaterThan(0).WithMessage("Số tiền phải > 0");
        RuleFor(x => x.KhachHangId).NotEmpty()
            .When(x => x.DoiTuong == CashTargetNames.KhachHang)
            .WithMessage("Chưa chọn khách hàng");
        RuleFor(x => x.NhaCungCapId).NotEmpty()
            .When(x => x.DoiTuong == CashTargetNames.NhaCungCap)
            .WithMessage("Chưa chọn nhà cung cấp");
        RuleForEach(x => x.Allocations!).ChildRules(a =>
        {
            a.RuleFor(x => x.DonHangId).NotEmpty();
            a.RuleFor(x => x.SoTien).GreaterThan(0).WithMessage("Số tiền phân bổ phải > 0");
        });
        RuleFor(x => x)
            .Must(x => (x.Allocations ?? []).Sum(a => a.SoTien) <= x.Tien)
            .WithMessage("Tổng tiền phân bổ vượt số tiền phiếu")
            .When(x => x.Allocations is { Count: > 0 });
    }
}

/// <summary>
/// Use-case quỹ (mục 5.1): phiếu thu/chi + tồn quỹ + lý do thu chi. KHÔNG chứa SQL.
/// Phân bổ nợ chỉ hợp lệ với THU_KH (trả nợ KH) và CHI_NCC (trả NCC).
/// </summary>
public sealed class CashbookService(CashbookRepository repo)
{
    private readonly CashVoucherRequestValidator _validator = new();

    public async Task<IReadOnlyList<CashReasonDto>> GetReasonsAsync(CancellationToken ct = default)
        => await repo.GetReasonsAsync(ct);

    public async Task<CashBalanceDto> GetBalanceAsync(CashBalanceQuery q, CancellationToken ct = default)
        => await repo.GetBalanceAsync(q, ct);

    public async Task<PageResult<CashVoucherDto>> SearchAsync(CashVoucherListQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchAsync(q, ct);
        return new PageResult<CashVoucherDto>
        {
            Items = items,
            Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize),
            Total = total
        };
    }

    public async Task<CashVoucherResult> CreateReceiptAsync(CashVoucherRequest req, string userId, CancellationToken ct = default)
        => await CreateCoreAsync(req, loaiThu: true, "phiếu thu", userId, ct);

    public async Task<CashVoucherResult> CreatePaymentAsync(CashVoucherRequest req, string userId, CancellationToken ct = default)
        => await CreateCoreAsync(req, loaiThu: false, "phiếu chi", userId, ct);

    private async Task<CashVoucherResult> CreateCoreAsync(
        CashVoucherRequest req, bool loaiThu, string label, string userId, CancellationToken ct)
    {
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException($"Dữ liệu {label} không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());

        // Phân bổ nợ chỉ áp dụng: thu từ KH (giảm CONNO) hoặc chi trả NCC (giảm CONLAI)
        if (req.Allocations is { Count: > 0 })
        {
            var allowed = (loaiThu && req.DoiTuong == CashTargetNames.KhachHang)
                       || (!loaiThu && req.DoiTuong == CashTargetNames.NhaCungCap);
            if (!allowed)
                throw new TechPos.Shared.ValidationException(
                    $"Phân bổ trả nợ chỉ áp dụng cho phiếu thu khách hàng hoặc phiếu chi trả nhà cung cấp",
                    [new ApiError(nameof(req.Allocations), "KHONG_HO_TRO_PHAN_BO", "Loại phiếu này không hỗ trợ phân bổ nợ")]);
        }

        return await repo.CreateAsync(req, loaiThu, userId, ct);
    }
}

/// <summary>Use-case công nợ (mục 5.2). KHÔNG chứa SQL.</summary>
public sealed class DebtService(DebtRepository repo)
{
    public async Task<PageResult<CustomerDebtDto>> SearchCustomerDebtsAsync(CustomerDebtQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchCustomerDebtsAsync(q, ct);
        return new PageResult<CustomerDebtDto>
        {
            Items = items,
            Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize),
            Total = total
        };
    }

    public async Task<PageResult<SupplierDebtDto>> SearchSupplierDebtsAsync(SupplierDebtQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchSupplierDebtsAsync(q, ct);
        return new PageResult<SupplierDebtDto>
        {
            Items = items,
            Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize),
            Total = total
        };
    }
}