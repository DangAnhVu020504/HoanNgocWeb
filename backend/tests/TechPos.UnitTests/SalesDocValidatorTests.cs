using FluentAssertions;
using TechPos.Application.Sales;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>Validator chứng từ Phase 4.9–4.10: báo giá/đặt hàng KH, đặt mua NCC,
/// hợp đồng, gửi COD, giao hàng theo xe.</summary>
public class SalesDocValidatorTests
{
    private static CustomerDocRequest Doc(params QuoteLine[] lines) =>
        new(null, null, null, null, null, lines);

    // ---------- Báo giá / Đặt hàng KH (4.9) ----------
    [Fact]
    public void CustomerDoc_valid_with_lines()
    {
        new CustomerDocRequestValidator()
            .Validate(Doc(new QuoteLine("m1", 2, 50000))).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CustomerDoc_invalid_with_empty_lines()
    {
        new CustomerDocRequestValidator().Validate(Doc()).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CustomerDoc_invalid_with_zero_qty()
    {
        new CustomerDocRequestValidator()
            .Validate(Doc(new QuoteLine("m1", 0, 50000))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CustomerDoc_invalid_with_tile_giam_over_100()
    {
        new CustomerDocRequestValidator()
            .Validate(Doc(new QuoteLine("m1", 1, 50000, 150))).IsValid.Should().BeFalse();
    }

    // ---------- Đặt mua NCC (4.9) ----------
    [Fact]
    public void PurchaseOrder_valid_when_ncc_and_lines_present()
    {
        var req = new PurchaseOrderRequest("ncc1", [new PurchaseOrderLine("m1", 3, 20000)]);
        new PurchaseOrderRequestValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void PurchaseOrder_invalid_without_ncc()
    {
        var req = new PurchaseOrderRequest("", [new PurchaseOrderLine("m1", 3, 20000)]);
        var r = new PurchaseOrderRequestValidator().Validate(req);
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(PurchaseOrderRequest.NhaCungCapId));
    }

    // ---------- Hợp đồng (4.9) ----------
    [Fact]
    public void Contract_valid_with_lines()
    {
        var req = new ContractRequest(null, null, null, null, null, null, null, null,
            [new ContractLine("m1", 5, 100000)]);
        new ContractRequestValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Contract_invalid_when_den_before_tu()
    {
        var req = new ContractRequest(null, null, null, null, null, null,
            new DateTime(2026, 5, 1), new DateTime(2026, 1, 1),
            [new ContractLine("m1", 1, 1000)]);
        var r = new ContractRequestValidator().Validate(req);
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(ContractRequest.DenNgay));
    }

    // ---------- COD / Giao xe (4.10) ----------
    [Fact]
    public void CodShipment_valid_with_partner_and_orders()
    {
        var req = new CodShipmentRequest("dv1", null, ["dh1", "dh2"]);
        new CodShipmentRequestValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CodShipment_invalid_without_orders()
    {
        var req = new CodShipmentRequest("dv1", null, []);
        new CodShipmentRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void VehicleDelivery_invalid_without_vehicle()
    {
        var req = new VehicleDeliveryRequest("", null, ["dh1"]);
        var r = new VehicleDeliveryRequestValidator().Validate(req);
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(VehicleDeliveryRequest.XeGiaoHangId));
    }
}