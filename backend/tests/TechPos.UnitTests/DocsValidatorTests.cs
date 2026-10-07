using FluentAssertions;
using TechPos.Application.Inventory;
using TechPos.Application.Pos;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>Validator các chứng từ Phase 4 mới: trả lại KH (4.3), xuất kho (4.5),
/// chuyển kho (4.6), xuất trả NCC — validate ở Application layer (mục 36).</summary>
public class DocsValidatorTests
{
    // ---------- Trả lại KH (4.3) ----------
    [Fact]
    public void CustomerReturn_valid_when_kho_and_lines_present()
    {
        var req = new CustomerReturnRequest("kho1", "kh1", null,
            [new CustomerReturnLine("m1", 2, 50000)]);
        new CustomerReturnRequestValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CustomerReturn_invalid_without_kho()
    {
        var req = new CustomerReturnRequest("", null, null,
            [new CustomerReturnLine("m1", 1, 1000)]);
        var r = new CustomerReturnRequestValidator().Validate(req);
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(CustomerReturnRequest.KhoNhanId));
    }

    [Fact]
    public void CustomerReturn_invalid_with_empty_lines()
    {
        var req = new CustomerReturnRequest("kho1", null, null, []);
        new CustomerReturnRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CustomerReturn_invalid_with_non_positive_qty()
    {
        var req = new CustomerReturnRequest("kho1", null, null,
            [new CustomerReturnLine("m1", 0, 1000)]);
        new CustomerReturnRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    // ---------- Xuất kho (4.5) ----------
    [Fact]
    public void StockIssue_valid_when_kho_and_lines_present()
    {
        var req = new StockIssueRequest("kho1", null, "Xuất hủy",
            [new StockIssueLine("m1", 3, 20000)]);
        new StockIssueRequestValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void StockIssue_invalid_without_kho()
    {
        var req = new StockIssueRequest("", null, null, [new StockIssueLine("m1", 1, 0)]);
        new StockIssueRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    // ---------- Chuyển kho (4.6) ----------
    [Fact]
    public void StockTransfer_valid_when_two_distinct_kho()
    {
        var req = new StockTransferRequest("khoA", "khoB", null, null,
            [new StockTransferLine("m1", 5)]);
        new StockTransferRequestValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void StockTransfer_invalid_when_same_kho()
    {
        var req = new StockTransferRequest("khoA", "khoA", null, null,
            [new StockTransferLine("m1", 5)]);
        var r = new StockTransferRequestValidator().Validate(req);
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(StockTransferRequest.KhoNhapId));
    }

    [Fact]
    public void StockTransfer_invalid_without_nhap_kho()
    {
        var req = new StockTransferRequest("khoA", "", null, null,
            [new StockTransferLine("m1", 5)]);
        new StockTransferRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    // ---------- Xuất trả NCC (4.5) ----------
    [Fact]
    public void SupplierReturn_valid_when_ncc_kho_lines_present()
    {
        var req = new SupplierReturnRequest("kho1", "ncc1", null,
            [new SupplierReturnLine("m1", 2, 30000)]);
        new SupplierReturnRequestValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SupplierReturn_invalid_without_ncc()
    {
        var req = new SupplierReturnRequest("kho1", "", null,
            [new SupplierReturnLine("m1", 1, 1000)]);
        var r = new SupplierReturnRequestValidator().Validate(req);
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(SupplierReturnRequest.NhaCungCapId));
    }
}