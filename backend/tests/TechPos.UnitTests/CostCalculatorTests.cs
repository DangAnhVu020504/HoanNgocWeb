using FluentAssertions;
using TechPos.Application.Inventory;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>Tính giá vốn (mục 4.8): FIFO / bình quân liên hoàn / bình quân cuối kỳ.
/// Tiền = DECIMAL, làm tròn 2 chữ số (quy tắc vàng 4).</summary>
public class CostCalculatorTests
{
    private static CostMovement In(string id, decimal qty, decimal gia) => new(id, qty, 0, gia);
    private static CostMovement Out(string id, decimal qty) => new(id, 0, qty, 0);

    // ================= FIFO =================

    [Fact]
    public void Fifo_consumes_oldest_layer_first()
    {
        // Nhập 10@1000 rồi nhập 10@2000; xuất 15 ⇒ 10@1000 + 5@2000 = 20000/15 ≈ 1333.33
        var moves = new List<CostMovement> { In("1", 10, 1000), In("2", 10, 2000), Out("3", 15) };
        CostCalculator.Fifo(0, 0, moves).Should().ContainSingle().Which.Should().Be(1333.33m);
    }

    [Fact]
    public void Fifo_second_out_uses_next_layer()
    {
        // Sau khi hết lớp 1, xuất tiếp lấy nguyên lớp 2
        var moves = new List<CostMovement> { In("1", 10, 1000), In("2", 10, 2000), Out("3", 10), Out("4", 5) };
        var r = CostCalculator.Fifo(0, 0, moves);
        r.Should().HaveCount(2);
        r[0].Should().Be(1000);
        r[1].Should().Be(2000);
    }

    [Fact]
    public void Fifo_opening_stock_consumed_first()
    {
        // Tồn đầu 5@800, nhập 5@1200, xuất 8 ⇒ 5@800 + 3@1200 = 7600/8 = 950
        var moves = new List<CostMovement> { In("1", 5, 1200), Out("2", 8) };
        CostCalculator.Fifo(5, 800, moves).Should().ContainSingle().Which.Should().Be(950);
    }

    [Fact]
    public void Fifo_out_exceeding_stock_falls_back_to_last_cost()
    {
        // Xuất 20 khi chỉ nhập 10@1000 ⇒ 10@1000 + 10@1000 (fallback lớp cuối)
        var moves = new List<CostMovement> { In("1", 10, 1000), Out("2", 20) };
        CostCalculator.Fifo(0, 0, moves).Should().ContainSingle().Which.Should().Be(1000);
    }

    // ================= Bình quân liên hoàn (moving average) =================

    [Fact]
    public void MovingAverage_updates_after_each_inbound()
    {
        // Nhập 10@1000 (avg=1000), nhập 10@2000 (avg=1500), xuất 4 ⇒ 1500
        var moves = new List<CostMovement> { In("1", 10, 1000), In("2", 10, 2000), Out("3", 4) };
        CostCalculator.MovingAverage(0, 0, moves).Should().ContainSingle().Which.Should().Be(1500);
    }

    [Fact]
    public void MovingAverage_uses_opening_average_first()
    {
        // Tồn đầu 10@1000; xuất 4 trước khi nhập ⇒ 1000 (còn 6@1000 = 6000);
        // nhập 10@2000 ⇒ avg = (6000+20000)/16 = 1625; xuất 4 ⇒ 1625
        var moves = new List<CostMovement> { Out("1", 4), In("2", 10, 2000), Out("3", 4) };
        var r = CostCalculator.MovingAverage(10, 1000, moves);
        r.Should().HaveCount(2);
        r[0].Should().Be(1000);
        r[1].Should().Be(1625);
    }

    [Fact]
    public void MovingAverage_never_negative_stock()
    {
        var moves = new List<CostMovement> { Out("1", 999), In("2", 10, 500), Out("3", 2) };
        var r = CostCalculator.MovingAverage(0, 700, moves);
        r.Should().HaveCount(2);
        r[1].Should().Be(500);
    }

    // ================= Bình quân cuối kỳ (periodic average) =================

    [Fact]
    public void PeriodicAverage_single_rate_for_whole_period()
    {
        // (10×1000 + 10×2000) / 20 = 1500 cho mọi dòng xuất
        var moves = new List<CostMovement> { In("1", 10, 1000), Out("2", 3), In("3", 10, 2000), Out("4", 5) };
        var r = CostCalculator.PeriodicAverage(0, 0, moves);
        r.Should().HaveCount(2);
        r.Should().OnlyContain(v => v == 1500);
    }

    [Fact]
    public void PeriodicAverage_includes_opening_stock()
    {
        // Tồn đầu 10@1000 + nhập 10@2000 ⇒ avg 1500
        var moves = new List<CostMovement> { In("1", 10, 2000), Out("2", 4) };
        CostCalculator.PeriodicAverage(10, 1000, moves).Should().ContainSingle().Which.Should().Be(1500);
    }

    [Fact]
    public void PeriodicAverage_no_inbound_falls_back()
    {
        var moves = new List<CostMovement> { Out("1", 4), Out("2", 6) };
        CostCalculator.PeriodicAverage(0, 900, moves).Should().OnlyContain(v => v == 900);
    }

    // ================= Rounding =================

    [Fact]
    public void All_methods_round_to_2_decimals_away_from_zero()
    {
        // FIFO: 2@1000 + 1@1001 tiêu thụ = 3001/3 = 1000.333… ⇒ 1000.33
        var fifoMoves = new List<CostMovement> { In("1", 2, 1000), In("2", 2, 1001), Out("3", 3) };
        CostCalculator.Fifo(0, 0, fifoMoves).Should().ContainSingle().Which.Should().Be(1000.33m);

        // BQ liên hoàn / cuối kỳ: (1×1000 + 2×1001)/3 = 3002/3 = 1000.666… ⇒ 1000.67
        var avgMoves = new List<CostMovement> { In("1", 1, 1000), In("2", 2, 1001), Out("3", 1) };
        CostCalculator.MovingAverage(0, 0, avgMoves).Should().ContainSingle().Which.Should().Be(1000.67m);
        CostCalculator.PeriodicAverage(0, 0, avgMoves).Should().ContainSingle().Which.Should().Be(1000.67m);
    }
}