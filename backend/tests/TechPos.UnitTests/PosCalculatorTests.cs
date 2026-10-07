using FluentAssertions;
using TechPos.Application.Pos;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>POS bán lẻ: tính tiền phải đúng, làm tròn VNĐ, không âm (mục 4.1, quy tắc vàng 4).</summary>
public class PosCalculatorTests
{
    [Fact]
    public void LineAmount_basic()
    {
        PosCalculator.LineAmount(2, 50000, 0, 0).Should().Be(100000);
    }

    [Fact]
    public void LineAmount_percent_discount()
    {
        // 2 × 100.000 giảm 10% = 180.000
        PosCalculator.LineAmount(2, 100000, 10, 0).Should().Be(180000);
    }

    [Fact]
    public void LineAmount_money_discount()
    {
        // 2 × 100.000 − 50.000 = 150.000
        PosCalculator.LineAmount(2, 100000, 0, 50000).Should().Be(150000);
    }

    [Fact]
    public void LineAmount_never_negative()
    {
        PosCalculator.LineAmount(1, 100000, 0, 500000).Should().Be(0);
    }

    [Fact]
    public void Compute_multi_lines_with_invoice_discount()
    {
        var lines = new List<PosCartItemDto>
        {
            new("m1", "A", null, 100000m, 2m),   // 200.000
            new("B", "B", null, 50000m, 1m)      // 50.000
        };
        var totals = PosCalculator.Compute(lines, tileGiamGiaHoaDon: 0, tienGiamGiaHoaDon: 30_000, khachDua: 300_000);

        totals.TienHang.Should().Be(250000);
        totals.TienGiamGia.Should().Be(30000);
        totals.TongCong.Should().Be(220000);
        totals.KhachDua.Should().Be(300000);
        totals.TraLai.Should().Be(80000);
        totals.ConNo.Should().Be(0);
    }

    [Fact]
    public void Compute_customer_pays_less_creates_debt()
    {
        var lines = new List<PosCartItemDto> { new("m", "A", null, 100000m, 1m) };
        var totals = PosCalculator.Compute(lines, 0, 0, 50_000);
        totals.TongCong.Should().Be(100000);
        totals.TraLai.Should().Be(0);
        totals.ConNo.Should().Be(50000);
    }

    [Fact]
    public void Compute_invoice_percent_discount()
    {
        var lines = new List<PosCartItemDto> { new("m", "A", null, 200000m, 1m) };
        var totals = PosCalculator.Compute(lines, tileGiamGiaHoaDon: 10, tienGiamGiaHoaDon: 0, khachDua: 180_000);
        totals.TienHang.Should().Be(200000);
        totals.TienGiamGia.Should().Be(20000);
        totals.TongCong.Should().Be(180000);
        totals.TraLai.Should().Be(0);
        totals.ConNo.Should().Be(0);
    }

    [Fact]
    public void Rounding_is_away_from_zero()
    {
        PosCalculator.RoundMoney(2.5m).Should().Be(3);
        PosCalculator.RoundMoney(2500.4m).Should().Be(2500);
    }
}
