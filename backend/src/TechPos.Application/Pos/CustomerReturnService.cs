using FluentValidation;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Pos;

/// <summary>Validator phiếu khách trả lại (mục 4.3).</summary>
public sealed class CustomerReturnRequestValidator : AbstractValidator<CustomerReturnRequest>
{
    public CustomerReturnRequestValidator()
    {
        RuleFor(x => x.KhoNhanId).NotEmpty().WithMessage("Chưa chọn kho nhận hàng về");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Phiếu trả lại phải có ít nhất 1 dòng hàng");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.MatHangId).NotEmpty();
            l.RuleFor(x => x.SoLuong).GreaterThan(0).WithMessage("Số lượng trả lại phải > 0");
            l.RuleFor(x => x.DonGia).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.TienTraLai).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Use-case phiếu khách trả lại hàng (mục 4.3). KHÔNG chứa SQL.</summary>
public sealed class CustomerReturnService(CustomerReturnRepository repo)
{
    private readonly CustomerReturnRequestValidator _validator = new();

    public async Task<CustomerReturnResult> CreateAsync(CustomerReturnRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu phiếu trả lại không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        return await repo.CreateAsync(req, userId, ct);
    }
}
