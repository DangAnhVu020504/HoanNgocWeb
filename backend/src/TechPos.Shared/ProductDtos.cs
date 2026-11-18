namespace TechPos.Shared;

/// <summary>
/// DTO mặt hàng (DMATHANG) — READ ONLY từ mirror MySQL (GĐ 1).
/// Dùng class có property public-set để Dapper map trực tiếp qua cột (không qua constructor
/// — tránh lỗi "parameterless/default constructor matching signature required" của Dapper + record).
/// </summary>
public sealed class ProductDto
{
    public string Id { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? MaSanCo { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? NhomMatHangId { get; set; }
    public string? DonViTinhId { get; set; }
    public string? HangSanXuatId { get; set; }
    public decimal GiaBan { get; set; }
    public decimal GiaBan2 { get; set; }   // GIABAN2 = "Giá bán sỉ" (bán sỉ 4.2)
    public decimal GiaBanChan { get; set; } // GIABANCHAN = "Giá bán chẵn" (bán theo đơn vị chẵn)
    public decimal GiaNhap { get; set; }
    public decimal GiaVon { get; set; }
    public decimal TonToiThieu { get; set; }
    public decimal TonToiDa { get; set; }
    public decimal HoaHong { get; set; }
    public int Status { get; set; }
    public string? DonViTinh { get; set; }
    public string? NhomMatHang { get; set; }
    public string? HangSanXuat { get; set; }
    public string? XuatXu { get; set; }
    public string? MauSac { get; set; }
    public string? ChatLieu { get; set; }
    public bool CoHanSuDung { get; set; }
    public bool CoSeri { get; set; }
    public DateTime? TimeCreated { get; set; }
    public DateTime? TimeModified { get; set; }

    /// <summary>Tồn kho hiện tại (OTONKHO): theo kho đang chọn nếu query có KhoId, ngược lại tổng mọi kho.</summary>
    public decimal TonKho { get; set; }
}

/// <summary>DTO danh mục nhóm mặt hàng (DNHOMMATHANG).</summary>
public sealed class ProductGroupDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Status { get; set; }
}

/// <summary>DTO danh mục đơn vị tính (DDONVITINH).</summary>
public sealed class ProductUnitDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Status { get; set; }
}

/// <summary>Tạo nhóm mặt hàng mới (DNHOMMATHANG).</summary>
public sealed record ProductGroupCreateRequest(string Name, string? Note = null);

/// <summary>DTO tồn kho theo kho (OTONKHO).</summary>
public sealed class ProductStockDto
{
    public string WareHouseId { get; set; } = string.Empty;
    public string? WareHouseName { get; set; }
    public decimal Ton { get; set; }
}

public sealed record ProductListQuery(
    string? Search = null,
    string? GroupId = null,
    bool? ActiveOnly = true,
    int Page = 1,
    int PageSize = 20,
    string Sort = "NAME",
    string? KhoId = null);

// ===================== Phase 4 CRUD (web ghi mirror, W_ORIGIN='WEB') =====================

/// <summary>Tạo mặt hàng mới trên web.</summary>
public sealed record ProductCreateRequest(
    string Name,
    string? Code,
    string? MaSanCo,
    string? Note,
    string? NhomMatHangId,
    string? DonViTinhId,
    string? HangSanXuatId,
    string? XuatXu,
    string? MauSac,
    string? ChatLieu,
    decimal GiaBan = 0,
    decimal GiaNhap = 0,
    decimal TonToiThieu = 0,
    decimal TonToiDa = 0,
    bool CoHanSuDung = false,
    bool CoSeri = false);

/// <summary>Sửa mặt hàng (partial — null = giữ nguyên).</summary>
public sealed record ProductUpdateRequest(
    string? Name,
    string? Note,
    decimal? GiaBan,
    decimal? GiaNhap,
    decimal? TonToiThieu,
    decimal? TonToiDa,
    string? NhomMatHangId,
    string? DonViTinhId,
    string? XuatXu,
    string? MauSac,
    string? ChatLieu,
    bool? CoHanSuDung,
    bool? CoSeri,
    int? Status);
