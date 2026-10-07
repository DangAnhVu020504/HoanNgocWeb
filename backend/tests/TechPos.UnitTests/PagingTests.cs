using FluentAssertions;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>Phân trang server-side phải chuẩn (mục 60): page ≥ 1, pageSize trong [1..200].</summary>
public class PagingTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(999, 999)]
    public void NormalizePage_always_at_least_1(int input, int expected)
        => Paging.NormalizePage(input).Should().Be(expected);

    [Fact]
    public void NormalizePageSize_clamps_to_max_200()
    {
        Paging.NormalizePageSize(500).Should().Be(200);
        Paging.NormalizePageSize(0).Should().Be(1);
        Paging.NormalizePageSize(-3).Should().Be(1);
        Paging.NormalizePageSize(50).Should().Be(50);
    }

    [Fact]
    public void Offset_is_zero_based()
    {
        Paging.Offset(1, 20).Should().Be(0);
        Paging.Offset(2, 20).Should().Be(20);
        Paging.Offset(3, 50).Should().Be(100);
    }
}
