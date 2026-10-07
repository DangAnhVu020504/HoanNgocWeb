using FluentValidation;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Pos;

/// <summary>Use-case hóa đơn bán sỉ (mục 4.2) — tái dùng PosSaleRequest/PosCalculator/PosSaleResult.
/// KHÔNG chứa SQL. LOAI/MODE do repository đọc từ W_DOC_TYPE_MAP ('BAN_SI').</summary>
public sealed class WholesaleService(WholesaleRepository repo)
{
    private readonly PosSaleRequestValidator _validator = new();

    public async Task<PosSaleResult> CreateSaleAsync(PosSaleRequest req, string userId, decimal khachDua, CancellationToken ct = default)
    {
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu hóa đơn bán sỉ không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());

        var totals = PosCalculator.Compute(
            req.Lines.Select(l => new PosCartItemDto(l.MatHangId, "", null, l.DonGia, l.SoLuong, l.TileGiamGia, l.GiamTheoTien)).ToList(),
            0, req.TienGiamGia, khachDua);

        return await repo.CreateSaleAsync(req, totals, userId, ct);
    }
}
