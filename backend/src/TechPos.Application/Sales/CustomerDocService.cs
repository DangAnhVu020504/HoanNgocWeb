using FluentValidation;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Sales;

/// <summary>Validator báo giá / đặt hàng KH (mục 4.9).</summary>
public sealed class CustomerDocRequestValidator : AbstractValidator<CustomerDocRequest>
{
    public CustomerDocRequestValidator()
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Chứng từ phải có ít nhất 1 dòng hàng");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.MatHangId).NotEmpty();
            l.RuleFor(x => x.SoLuong).GreaterThan(0).WithMessage("Số lượng phải > 0");
            l.RuleFor(x => x.DonGia).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.TileGiamGia).InclusiveBetween(0, 100);
        });
        RuleFor(x => x.TienGiamGia).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TienThue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PhiVanChuyen).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Use-case báo giá (TBAOGIA) + đặt hàng KH (TDATHANG) — mục 4.9. KHÔNG chứa SQL.</summary>
public sealed class CustomerDocService(CustomerDocRepository repo)
{
    private readonly CustomerDocRequestValidator _validator = new();

    public async Task<QuoteResult> CreateQuoteAsync(CustomerDocRequest req, string userId, CancellationToken ct = default)
    {
        EnsureValid(req, "báo giá");
        return await repo.CreateQuoteAsync(req, userId, ct);
    }

    public async Task<OrderResult> CreateOrderAsync(CustomerDocRequest req, string userId, CancellationToken ct = default)
    {
        EnsureValid(req, "đặt hàng");
        return await repo.CreateOrderAsync(req, userId, ct);
    }

    private void EnsureValid(CustomerDocRequest req, string label)
    {
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException($"Dữ liệu {label} không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
    }
}