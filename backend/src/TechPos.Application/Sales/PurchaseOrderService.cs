using FluentValidation;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Sales;

/// <summary>Validator đặt mua nhà cung cấp (mục 4.9).</summary>
public sealed class PurchaseOrderRequestValidator : AbstractValidator<PurchaseOrderRequest>
{
    public PurchaseOrderRequestValidator()
    {
        RuleFor(x => x.NhaCungCapId).NotEmpty().WithMessage("Chưa chọn nhà cung cấp");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Đơn đặt mua phải có ít nhất 1 dòng hàng");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.MatHangId).NotEmpty();
            l.RuleFor(x => x.SoLuong).GreaterThan(0).WithMessage("Số lượng phải > 0");
            l.RuleFor(x => x.DonGia).GreaterThanOrEqualTo(0);
        });
    }
}

/// <summary>Use-case đặt mua nhà cung cấp (TDATHANGNCC) — mục 4.9. KHÔNG chứa SQL.</summary>
public sealed class PurchaseOrderService(PurchaseOrderRepository repo)
{
    private readonly PurchaseOrderRequestValidator _validator = new();

    public async Task<PurchaseOrderResult> CreateAsync(PurchaseOrderRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu đặt mua NCC không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        return await repo.CreateAsync(req, userId, ct);
    }
}