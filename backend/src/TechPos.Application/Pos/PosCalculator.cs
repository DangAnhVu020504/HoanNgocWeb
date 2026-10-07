using TechPos.Shared;

namespace TechPos.Application.Pos;

/// <summary>
/// Máy tính POS bán lẻ — pure function, unit test được (mục 60).
/// Tiền = decimal, làm tròn 0 chữ số thập phân (VNĐ); không FLOAT (quy tắc vàng 4).
/// </summary>
public static class PosCalculator
{
    /// <summary>Thành tiền 1 dòng = SL × ĐG × (1 - giảm%) − giảm tiền (không âm).</summary>
    public static decimal LineAmount(decimal soLuong, decimal donGia, decimal tileGiamGia, decimal giamTheoTien)
    {
        var goc = soLuong * donGia;
        var giam = RoundMoney(goc * ClampPct(tileGiamGia) / 100m) + Math.Max(0, giamTheoTien);
        return RoundMoney(Math.Max(0, goc - giam));
    }

    /// <summary>Tổng hoá đơn từ các dòng + giảm giá hoá đơn (tiền & %) + tiền khách đưa.</summary>
    public static SaleTotals Compute(
        IReadOnlyList<PosCartItemDto> lines,
        decimal tileGiamGiaHoaDon,
        decimal tienGiamGiaHoaDon,
        decimal khachDua)
    {
        var tienHang = RoundMoney(lines.Sum(l =>
        {
            var goc = RoundMoney(l.SoLuong * l.DonGia);
            var giam = RoundMoney(goc * ClampNonNeg(l.TileGiamGia) / 100m) + Math.Max(0, l.TienGiamGia);
            return Math.Max(0, goc - giam);
        }));

        var giamTile = RoundMoney(tienHang * ClampPct(tileGiamGiaHoaDon) / 100m);
        var giamTien = Math.Clamp(tienGiamGiaHoaDon, 0, tienHang);
        var giamTong = RoundMoney(Math.Min(giamTien + giamTile, tienHang));

        var tongCong = RoundMoney(tienHang - giamTong);
        var khachDuaC = Math.Max(0, RoundMoney(khachDua));
        var traLai = RoundMoney(Math.Max(0, khachDuaC - tongCong));
        var conNo = RoundMoney(Math.Max(0, tongCong - khachDuaC));

        return new SaleTotals(tienHang, giamTong, tongCong, khachDuaC, traLai, conNo);
    }

    public static decimal RoundMoney(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);

    private static decimal ClampPct(decimal v) => Math.Clamp(v, 0, 100);
    private static decimal ClampNonNeg(decimal v) => Math.Max(0, v);
}
