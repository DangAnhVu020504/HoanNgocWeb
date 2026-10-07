using FluentAssertions;
using TechPos.Application.Cashbook;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>Validator phiếu thu/chi (mục 5.1) — đối tượng bắt buộc, tiền > 0,
/// tổng phân bổ không vượt số tiền phiếu.</summary>
public class CashVoucherValidatorTests
{
    private static CashVoucherRequest Kh(decimal tien, params CashVoucherAllocation[]? allocs) =>
        new(CashTargetNames.KhachHang, "kh1", null, null, null, tien, false, null, allocs);

    [Fact]
    public void Valid_customer_receipt()
    {
        new CashVoucherRequestValidator().Validate(Kh(500000)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_target()
    {
        var req = Kh(1000).WithDoiTuong("XX");
        new CashVoucherRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Customer_requires_khach_hang_id()
    {
        var req = new CashVoucherRequest(CashTargetNames.KhachHang, null, null, null, null, 1000);
        var r = new CashVoucherRequestValidator().Validate(req);
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(CashVoucherRequest.KhachHangId));
    }

    [Fact]
    public void Ncc_requires_nha_cung_cap_id()
    {
        var req = new CashVoucherRequest(CashTargetNames.NhaCungCap, null, null, null, null, 1000);
        new CashVoucherRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Ncc_valid_with_id()
    {
        var req = new CashVoucherRequest(CashTargetNames.NhaCungCap, null, "ncc1", null, null, 1000);
        new CashVoucherRequestValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Amount_must_be_positive()
    {
        new CashVoucherRequestValidator().Validate(Kh(0)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Allocation_sum_must_not_exceed_amount()
    {
        var req = Kh(1000,
            new CashVoucherAllocation("dh1", 600), new CashVoucherAllocation("dh2", 600));
        var r = new CashVoucherRequestValidator().Validate(req);
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.ErrorMessage.Contains("vượt số tiền phiếu"));
    }

    [Fact]
    public void Allocation_equal_to_amount_is_valid()
    {
        var req = Kh(1000,
            new CashVoucherAllocation("dh1", 400), new CashVoucherAllocation("dh2", 600));
        new CashVoucherRequestValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Allocation_amount_must_be_positive()
    {
        var req = Kh(1000, new CashVoucherAllocation("dh1", 0));
        new CashVoucherRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }
}

file static class TestExtensions
{
    internal static CashVoucherRequest WithDoiTuong(this CashVoucherRequest r, string doiTuong) =>
        r with { DoiTuong = doiTuong };
}