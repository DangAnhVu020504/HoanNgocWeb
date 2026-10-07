using FluentAssertions;
using TechPos.Application.Catalog;
using TechPos.Application.Products;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>
/// Validators Phase 4 CRUD danh mục phải chặn dữ liệu rác trước khi tới DB (mục 8.2 DoD #5).
/// </summary>
public class CatalogValidatorTests
{
    private static CustomerCreateRequest Cust(string name = "Nguyễn Văn A", string? email = null) =>
        new(
            Name: name,
            MaKhach: "MK001",
            DiaChi: null,
            DienThoai: "0901234567",
            Email: email,
            MaSoThue: null,
            NguoiDaiDien: null,
            ChucVu: null,
            Facebook: null,
            DiaChiGiaoHang: null,
            NhomKhachHangId: null,
            SongayChoNo: 0,
            DiemTichLuy: 0);

    private static SupplierCreateRequest Sup(string name = "NCC A", string? email = null) =>
        new(name, "NCC001", null, "0901234567", email, null, null, null);

    private static WarehouseCreateRequest Wh(string name = "Kho chính") =>
        new(name, null, null, null, false);

    private static ProductCreateRequest Prod(string name = "Mặt hàng A", string? code = "MH001") =>
        new(
            Name: name,
            Code: code,
            MaSanCo: null,
            Note: null,
            NhomMatHangId: null,
            DonViTinhId: null,
            HangSanXuatId: null,
            XuatXu: null,
            MauSac: null,
            ChatLieu: null);

    // ---------- CustomerValidator ----------
    [Fact]
    public void CustomerValidator_accepts_valid_request()
    {
        var r = new CustomerValidator().Validate(Cust());
        r.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CustomerValidator_rejects_empty_name()
    {
        var r = new CustomerValidator().Validate(Cust(name: ""));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CustomerValidator_rejects_bad_email()
    {
        var r = new CustomerValidator().Validate(Cust(email: "khong-hople"));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void CustomerValidator_rejects_negative_debt_days()
    {
        var r = new CustomerValidator().Validate(
            new CustomerCreateRequest(
                Name: "A", MaKhach: null, DiaChi: null, DienThoai: null, Email: null, MaSoThue: null,
                NguoiDaiDien: null, ChucVu: null, Facebook: null, DiaChiGiaoHang: null,
                NhomKhachHangId: null, SongayChoNo: -1));
        r.IsValid.Should().BeFalse();
    }

    // ---------- SupplierValidator ----------
    [Fact]
    public void SupplierValidator_accepts_valid_request()
    {
        new SupplierValidator().Validate(Sup()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SupplierValidator_rejects_empty_name()
    {
        new SupplierValidator().Validate(Sup(name: "")).IsValid.Should().BeFalse();
    }

    // ---------- WarehouseValidator ----------
    [Fact]
    public void WarehouseValidator_accepts_valid_request()
    {
        new WarehouseValidator().Validate(Wh()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void WarehouseValidator_rejects_empty_name()
    {
        new WarehouseValidator().Validate(Wh(name: "  ")).IsValid.Should().BeFalse();
    }

    // ---------- ProductValidator ----------
    [Fact]
    public void ProductValidator_accepts_valid_request()
    {
        new ProductValidator().Validate(Prod()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ProductValidator_rejects_negative_price()
    {
        var r = new ProductValidator().Validate(
            new ProductCreateRequest(
                Name: "X", Code: null, MaSanCo: null, Note: null, NhomMatHangId: null,
                DonViTinhId: null, HangSanXuatId: null, XuatXu: null, MauSac: null, ChatLieu: null,
                GiaBan: -100));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ProductValidator_rejects_empty_name()
    {
        new ProductValidator().Validate(Prod(name: "")).IsValid.Should().BeFalse();
    }
}

/// <summary>CatalogWriteGuard chặn ghi khi flag OFF (ADR-02 — GĐ1–6 web không ghi nghiệp vụ).</summary>
public class CatalogWriteGuardTests
{
    [Fact]
    public void Throws_forbidden_when_flag_off()
    {
        var act = () => CatalogWriteGuard.EnsureWriteAllowed(new FeatureFlagOptions { EnableCatalogWrite = false });
        act.Should().Throw<ForbiddenException>()
            .WithMessage("*ENABLE_CATALOG_WRITE=false*");
    }

    [Fact]
    public void Passes_when_flag_on()
    {
        var act = () => CatalogWriteGuard.EnsureWriteAllowed(new FeatureFlagOptions { EnableCatalogWrite = true });
        act.Should().NotThrow();
    }

    [Fact]
    public void Default_flag_is_off()
    {
        // Quy ước an toàn: quên cấu hình = chặn ghi (không bao giờ ghi ngoài ý muốn)
        new FeatureFlagOptions().EnableCatalogWrite.Should().BeFalse();
    }
}
