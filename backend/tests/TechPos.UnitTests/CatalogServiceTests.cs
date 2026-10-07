using FluentAssertions;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>
/// Query danh mục Phase 4 phải có default an toàn (mục 60) — paging luôn nằm trong giới hạn.
/// </summary>
public class CatalogQueryTests
{
    [Fact]
    public void CustomerListQuery_defaults_are_safe()
    {
        var q = new CustomerListQuery();
        q.Page.Should().Be(1);
        q.PageSize.Should().Be(20);
        q.Sort.Should().Be("NAME");
        q.ActiveOnly.Should().BeTrue();
    }

    [Fact]
    public void SupplierListQuery_defaults_are_safe()
    {
        var q = new SupplierListQuery();
        q.Page.Should().Be(1);
        q.PageSize.Should().Be(20);
        q.ActiveOnly.Should().BeTrue();
    }

    [Fact]
    public void WarehouseListQuery_defaults_are_safe()
    {
        var q = new WarehouseListQuery();
        q.Page.Should().Be(1);
        q.PageSize.Should().Be(20);
        q.Sort.Should().Be("NAME");
    }
}
