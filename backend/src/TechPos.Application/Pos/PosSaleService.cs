using FluentValidation;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Pos;

/// <summary>Validator PosSaleRequest (mục 36/60).</summary>
public sealed class PosSaleRequestValidator : FluentValidation.AbstractValidator<PosSaleRequest>
{
    public PosSaleRequestValidator()
    {
        RuleFor(x => x.KhoId).NotEmpty().WithMessage("Chưa chọn kho xuất");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Hoá đơn phải có ít nhất 1 dòng hàng");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.MatHangId).NotEmpty();
            l.RuleFor(x => x.SoLuong).GreaterThan(0).WithMessage("Số lượng phải > 0");
            l.RuleFor(x => x.DonGia).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.TileGiamGia).InclusiveBetween(0, 100);
            l.RuleFor(x => x.GiamTheoTien).GreaterThanOrEqualTo(0);
        });
    }
}

/// <summary>Use-case POS bán lẻ (mục 4.1). KHÔNG chứa SQL.</summary>
public sealed class PosSaleService(PosSaleRepository repo)
{
    private readonly PosSaleRequestValidator _validator = new();

    public SaleTotals ComputeTotals(IReadOnlyList<PosCartItemDto> cart, decimal tileGiamHoaDon, decimal tienGiamHoaDon, decimal khachDua)
        => PosCalculator.Compute(cart, tileGiamHoaDon, tienGiamHoaDon, khachDua);

    public async Task<PosSaleResult> CreateSaleAsync(PosSaleRequest req, string userId, decimal khachDua, CancellationToken ct = default)
    {
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu hoá đơn không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());

        var totals = PosCalculator.Compute(
            req.Lines.Select(l => new PosCartItemDto(l.MatHangId, "", null, l.DonGia, l.SoLuong, l.TileGiamGia, l.GiamTheoTien)).ToList(),
            0, req.TienGiamGia, khachDua);

        return await repo.CreateSaleAsync(req, totals, userId, ct);
    }
}
