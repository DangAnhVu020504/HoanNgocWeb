using FluentValidation;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Inventory;

/// <summary>Validator yêu cầu tính lại giá vốn (mục 4.8).</summary>
public sealed class CostRecalcRequestValidator : AbstractValidator<CostRecalcRequest>
{
    public CostRecalcRequestValidator()
    {
        RuleFor(x => x.PhuongPhap).NotEmpty().WithMessage("Chưa chọn phương pháp tính giá vốn");
        RuleFor(x => x.TuNgay).LessThanOrEqualTo(_ => DateTime.Today)
            .WithMessage("Tính từ ngày không được sau hôm nay");
    }
}

/// <summary>
/// Use-case tính lại giá vốn (mục 4.8) — dialog "Tính lại giá vốn": chọn phương pháp
/// (FIFO / bình quân liên hoàn / bình quân cuối kỳ) + tính từ ngày. KHÔNG chứa SQL.
/// Đích danh là chọn lô thủ công ⇒ web chưa hỗ trợ, chặn rõ ràng.
/// </summary>
public sealed class CostRecalcService(CostRecalcRepository repo)
{
    private readonly CostRecalcRequestValidator _validator = new();

    public async Task<CostRecalcResult> RecalculateAsync(CostRecalcRequest req, CancellationToken ct = default)
    {
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu tính lại giá vốn không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());

        if (req.PhuongPhap == CostMethodNames.Specific)
            throw new TechPos.Shared.ValidationException(
                "Phương pháp Đích danh yêu cầu chọn lô/seri thủ công từng lần xuất — chưa hỗ trợ tính tự động trên web",
                [new ApiError(nameof(req.PhuongPhap), "DICH_DANH_KHONG_HO_TRO",
                    "Đích danh chưa hỗ trợ tính tự động")]);

        var data = await repo.LoadAsync(req.TuNgay, ct);

        var outboundCosts = new List<CostRecalcRepository.OutboundCost>();
        foreach (var p in data)
        {
            List<decimal> costs = req.PhuongPhap switch
            {
                CostMethodNames.Fifo => CostCalculator.Fifo(p.OpeningQty, p.FallbackCost, p.Movements),
                CostMethodNames.MovingAverage => CostCalculator.MovingAverage(p.OpeningQty, p.FallbackCost, p.Movements),
                CostMethodNames.PeriodicAverage => CostCalculator.PeriodicAverage(p.OpeningQty, p.FallbackCost, p.Movements),
                _ => throw new TechPos.Shared.ValidationException(
                    $"Phương pháp '{req.PhuongPhap}' không hợp lệ (FIFO | BQ_LIEN_HOAN | BQ_CUOI_KY)",
                    [new ApiError(nameof(req.PhuongPhap), "PHUONG_PHAP_KHONG_HOP_LE", "Chọn FIFO / BQ_LIEN_HOAN / BQ_CUOI_KY")])
            };

            var outIdx = 0;
            foreach (var m in p.Movements)
            {
                if (m.SlXuat <= 0) continue;
                outboundCosts.Add(new CostRecalcRepository.OutboundCost(
                    m.LedgerId, p.DocIds.GetValueOrDefault(m.LedgerId), p.MatHangId, costs[outIdx++]));
            }
        }

        var soDongXuat = await repo.SaveAsync(outboundCosts, ct);
        return new CostRecalcResult(
            req.PhuongPhap, req.TuNgay.Date, data.Count, soDongXuat,
            Math.Round(outboundCosts.Sum(c => c.Cost), 2, MidpointRounding.AwayFromZero));
    }
}