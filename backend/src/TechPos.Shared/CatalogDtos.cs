namespace TechPos.Shared;

/// <summary>DTO khách hàng (DKHACHHANG) — READ ONLY từ mirror MySQL (Phase 4).</summary>
public sealed class CustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? MaKhach { get; set; }
    public string? DiaChi { get; set; }
    public string? DiaChiGiaoHang { get; set; }
    public string? DienThoai { get; set; }
    public string? Email { get; set; }
    public string? MaSoThue { get; set; }
    public string? NguoiDaiDien { get; set; }
    public string? ChucVu { get; set; }
    public string? Facebook { get; set; }
    public string? NhomKhachHang { get; set; }
    public string? NhanVienPhuTrach { get; set; }
    public int SongayChoNo { get; set; }
    public int DiemTichLuy { get; set; }
    public int Status { get; set; }
    public DateTime? TimeCreated { get; set; }
    public DateTime? TimeModified { get; set; }
}

public sealed record CustomerListQuery(
    string? Search = null,
    string? GroupId = null,
    bool? ActiveOnly = true,
    int Page = 1,
    int PageSize = 20,
    string Sort = "NAME");

/// <summary>DTO nhà cung cấp (DNHACUNGCAP) — READ ONLY từ mirror MySQL (Phase 4).</summary>
public sealed class SupplierDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? MaNhaCungCap { get; set; }
    public string? DiaChi { get; set; }
    public string? DienThoai { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Note { get; set; }
    public string? NhomNhaCungCap { get; set; }
    public int Status { get; set; }
    public DateTime? TimeCreated { get; set; }
    public DateTime? TimeModified { get; set; }
}

public sealed record SupplierListQuery(
    string? Search = null,
    string? GroupId = null,
    bool? ActiveOnly = true,
    int Page = 1,
    int PageSize = 20,
    string Sort = "NAME");

/// <summary>DTO kho hàng (DKHOHANG) — READ ONLY từ mirror MySQL (Phase 4).</summary>
public sealed class WarehouseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int Status { get; set; }
    public string? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? CuaHang { get; set; }
    public bool ChoPhepAmKho { get; set; }
    public DateTime? TimeCreated { get; set; }
    public DateTime? TimeModified { get; set; }
}

public sealed record WarehouseListQuery(
    string? Search = null,
    bool? ActiveOnly = true,
    int Page = 1,
    int PageSize = 20,
    string Sort = "NAME");

// ===================== Phase 4 CRUD (web ghi mirror, W_ORIGIN='WEB') =====================

/// <summary>Tạo khách hàng mới trên web.</summary>
public sealed record CustomerCreateRequest(
    string Name,
    string? MaKhach,
    string? DiaChi,
    string? DienThoai,
    string? Email,
    string? MaSoThue,
    string? NguoiDaiDien,
    string? ChucVu,
    string? Facebook,
    string? DiaChiGiaoHang,
    string? NhomKhachHangId,
    int SongayChoNo = 0,
    int DiemTichLuy = 0);

/// <summary>Sửa khách hàng (partial — null = giữ nguyên).</summary>
public sealed record CustomerUpdateRequest(
    string? Name,
    string? DiaChi,
    string? DienThoai,
    string? Email,
    string? MaSoThue,
    string? NguoiDaiDien,
    string? ChucVu,
    string? Facebook,
    string? DiaChiGiaoHang,
    string? NhomKhachHangId,
    int? SongayChoNo,
    int? DiemTichLuy,
    int? Status);

/// <summary>Tạo nhà cung cấp mới trên web.</summary>
public sealed record SupplierCreateRequest(
    string Name,
    string? MaNhaCungCap,
    string? DiaChi,
    string? DienThoai,
    string? Email,
    string? Website,
    string? Note,
    string? NhomNhaCungCapId);

/// <summary>Sửa nhà cung cấp (partial — null = giữ nguyên).</summary>
public sealed record SupplierUpdateRequest(
    string? Name,
    string? DiaChi,
    string? DienThoai,
    string? Email,
    string? Website,
    string? Note,
    string? NhomNhaCungCapId,
    int? Status);

/// <summary>Tạo kho hàng mới trên web.</summary>
public sealed record WarehouseCreateRequest(
    string Name,
    string? Note,
    string? ParentId,
    string? CuaHangId,
    bool ChoPhepAmKho = false);

/// <summary>Sửa kho hàng (partial — null = giữ nguyên).</summary>
public sealed record WarehouseUpdateRequest(
    string? Name,
    string? Note,
    string? ParentId,
    string? CuaHangId,
    bool? ChoPhepAmKho,
    int? Status);

/// <summary>Kết quả tạo mới — trả về Id để frontend mở chi tiết.</summary>
public sealed record CatalogCreateResult(string Id, string Name);
