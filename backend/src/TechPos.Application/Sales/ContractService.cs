using FluentValidation;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Sales;

/// <summary>Validator hợp đồng (mục 4.9).</summary>
public sealed class ContractRequestValidator : AbstractValidator<ContractRequest>
{
    public ContractRequestValidator()
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Hợp đồng phải có ít nhất 1 dòng hàng");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.MatHangId).NotEmpty();
            l.RuleFor(x => x.SoLuong).GreaterThan(0).WithMessage("Số lượng phải > 0");
            l.RuleFor(x => x.DonGia).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.DatCoc).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DenNgay)
            .GreaterThanOrEqualTo(x => x.TuNgay!.Value)
            .When(x => x.TuNgay.HasValue && x.DenNgay.HasValue)
            .WithMessage("Đến ngày phải sau hoặc bằng từ ngày");
    }
}

/// <summary>Use-case hợp đồng (THOPDONG) — mục 4.9. KHÔNG chứa SQL.</summary>
public sealed class ContractService(ContractRepository repo)
{
    private readonly ContractRequestValidator _validator = new();

    public async Task<ContractResult> CreateAsync(ContractRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu hợp đồng không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        return await repo.CreateAsync(req, userId, ct);
    }
}