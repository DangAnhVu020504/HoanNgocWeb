using FluentAssertions;
using TechPos.Application.Hr;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>Validator + công thức tính lương Phase 5.3 (mục 5.3, DOI-CHIEU §2.7):
/// nhân viên bắt buộc tên; thưởng/phạt chỉ 1 trong 2; công thức lương ca/tháng,
/// TONGLUONG = ca + tháng + thưởng + phụ cấp, THUCNHAN = TONGLUONG − phạt − tạm ứng.</summary>
public class HrValidatorTests
{
    private static EmployeeCreateRequest Emp(
        string name = "Nguyễn Văn A", int ctl = 0,
        decimal luongCa = 50000, decimal luongThang = 0, decimal phuCap = 0) =>
        new(name, null, null, ctl, luongCa, luongThang, phuCap, false, true, null);

    // ---------- EmployeeValidator ----------
    [Fact]
    public void Employee_valid_with_name()
    {
        new EmployeeValidator().Validate(Emp()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Employee_invalid_without_name()
    {
        var r = new EmployeeValidator().Validate(Emp(name: ""));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Employee_invalid_cach_tinh_luong()
    {
        var r = new EmployeeValidator().Validate(Emp(ctl: 5));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Employee_monthly_requires_luong_thang_non_negative()
    {
        var r = new EmployeeValidator().Validate(Emp(ctl: 1, luongThang: 5_000_000));
        r.IsValid.Should().BeTrue();
    }

    // ---------- BonusPenaltyValidator ----------
    [Fact]
    public void Bonus_valid_when_thuong_only()
    {
        var req = new BonusPenaltyCreateRequest("nv1", Thuong: 200_000);
        new BonusPenaltyValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bonus_valid_when_phat_only()
    {
        var req = new BonusPenaltyCreateRequest("nv1", Phat: 100_000);
        new BonusPenaltyValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Bonus_invalid_when_both_thuong_and_phat()
    {
        var req = new BonusPenaltyCreateRequest("nv1", Thuong: 100_000, Phat: 50_000);
        new BonusPenaltyValidator().Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Bonus_invalid_when_zero()
    {
        var req = new BonusPenaltyCreateRequest("nv1");
        new BonusPenaltyValidator().Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Bonus_invalid_without_nhan_vien()
    {
        var req = new BonusPenaltyCreateRequest("", Thuong: 100_000);
        var r = new BonusPenaltyValidator().Validate(req);
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == nameof(BonusPenaltyCreateRequest.NhanVienId));
    }

    // ---------- SalaryAdvanceValidator ----------
    [Fact]
    public void Advance_invalid_when_tien_zero()
    {
        var r = new SalaryAdvanceValidator().Validate(new SalaryAdvanceRequest("nv1", 0));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Advance_valid()
    {
        new SalaryAdvanceValidator().Validate(new SalaryAdvanceRequest("nv1", 1_000_000)).IsValid.Should().BeTrue();
    }

    // ---------- IntroLetterValidator ----------
    [Fact]
    public void IntroLetter_valid()
    {
        var req = new IntroLetterCreateRequest(null, null, null, null, null, "Nguyễn Văn B", "Công ty ABC", "Giao hàng", null);
        new IntroLetterValidator().Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void IntroLetter_invalid_without_ong_ba()
    {
        var req = new IntroLetterCreateRequest(null, null, null, null, null, "", "ABC", null, null);
        var r = new IntroLetterValidator().Validate(req);
        r.IsValid.Should().BeFalse();
    }

    // ---------- PayrollCalculator (công thức §2.7 — TILELUONG dạng %: 100 = nguyên ca) ----------
    [Fact]
    public void Payroll_luong_ca_theo_so_cong_va_ti_le()
    {
        // 20 công × ca 100% × 50.000 = 1.000.000
        PayrollCalculator.LuongCa(20, 100m, 50_000).Should().Be(1_000_000);
        // 10 công × ca 50% × 40.000 = 200.000
        PayrollCalculator.LuongCa(10, 50m, 40_000).Should().Be(200_000);
    }

    [Fact]
    public void Payroll_tong_luong_gom_ca_thang_thuong_phucap()
    {
        PayrollCalculator.TongLuong(1_000_000, 5_000_000, 500_000, 200_000).Should().Be(6_700_000);
    }

    [Fact]
    public void Payroll_thuc_nhan_tru_phat_tam_ung()
    {
        PayrollCalculator.ThucNhan(6_700_000, 300_000, 1_000_000).Should().Be(5_400_000);
    }
}
