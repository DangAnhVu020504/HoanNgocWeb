using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Catalog;
using TechPos.Application.Reports;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Danh mục khách hàng (DKHACHHANG) — Phase 4, đọc + ghi mirror MySQL (W_ORIGIN='WEB').</summary>
[ApiController]
[Route("api/customers")]
[Authorize]
public sealed class CustomersController(CustomerService customers, CatalogImportService import, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("SALES.CUSTOMER.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] string? groupId,
        [FromQuery] bool? activeOnly, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "NAME", CancellationToken ct = default)
    {
        var result = await customers.SearchAsync(
            new CustomerListQuery(search, groupId, activeOnly, page, pageSize, sort), ct);
        return Ok(ApiResult<PageResult<CustomerDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id}")]
    [RequirePermission("SALES.CUSTOMER.VIEW")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
        => Ok(ApiResult<CustomerDto>.Ok(await customers.GetAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpGet("groups")]
    [RequirePermission("SALES.CUSTOMER.VIEW")]
    public async Task<IActionResult> Groups(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<CustomerGroupDto>>.Ok(
            await customers.GetGroupsAsync(ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("SALES.CUSTOMER.CREATE")]
    public async Task<IActionResult> Create([FromBody] CustomerCreateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await customers.CreateAsync(req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "CREATE", "DKHACHHANG", result.Id,
            newValue: req, ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CatalogCreateResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id}")]
    [RequirePermission("SALES.CUSTOMER.UPDATE")]
    public async Task<IActionResult> Update(string id, [FromBody] CustomerUpdateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var old = await customers.GetAsync(id, ct);
        var result = await customers.UpdateAsync(id, req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "UPDATE", "DKHACHHANG", id,
            oldValue: new { old.Name, old.DiaChi, old.DienThoai, old.Email, old.MaSoThue, old.Status },
            newValue: req, ip, ua, HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CustomerDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id}")]
    [RequirePermission("SALES.CUSTOMER.DELETE")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var old = await customers.GetAsync(id, ct);
        await customers.DeleteAsync(id, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "SOFT_DELETE", "DKHACHHANG", id,
            oldValue: new { old.Name, old.MaKhach }, ip: ip, userAgent: ua,
            correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Nhập Excel danh mục khách hàng (Phase 6.4) — cột như file xuất: Mã khách|Tên|ĐT|Địa chỉ|Email|Nhóm|Số ngày cho nợ|...</summary>
    [HttpPost("import")]
    [RequirePermission("SALES.CUSTOMER.CREATE")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Ok(ApiResult<string>.Fail("Chưa chọn file Excel", HttpContext.TraceIdentifier));
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await import.ImportCustomersAsync(ms.ToArray(), uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "IMPORT", "DKHACHHANG", null,
            newValue: new { file.FileName, result.Created, result.Skipped, SoLoi = result.Errors.Count },
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<ImportResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Danh mục nhà cung cấp (DNHACUNGCAP) — Phase 4, đọc + ghi mirror MySQL.</summary>
[ApiController]
[Route("api/suppliers")]
[Authorize]
public sealed class SuppliersController(SupplierService suppliers, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("DEBT.SUPPLIER.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] string? groupId,
        [FromQuery] bool? activeOnly, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "NAME", CancellationToken ct = default)
    {
        var result = await suppliers.SearchAsync(
            new SupplierListQuery(search, groupId, activeOnly, page, pageSize, sort), ct);
        return Ok(ApiResult<PageResult<SupplierDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id}")]
    [RequirePermission("DEBT.SUPPLIER.VIEW")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
        => Ok(ApiResult<SupplierDto>.Ok(await suppliers.GetAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpGet("groups")]
    [RequirePermission("DEBT.SUPPLIER.VIEW")]
    public async Task<IActionResult> Groups(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<SupplierGroupDto>>.Ok(
            await suppliers.GetGroupsAsync(ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("DEBT.SUPPLIER.CREATE")]
    public async Task<IActionResult> Create([FromBody] SupplierCreateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await suppliers.CreateAsync(req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "CREATE", "DNHACUNGCAP", result.Id,
            newValue: req, ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CatalogCreateResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id}")]
    [RequirePermission("DEBT.SUPPLIER.UPDATE")]
    public async Task<IActionResult> Update(string id, [FromBody] SupplierUpdateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var old = await suppliers.GetAsync(id, ct);
        var result = await suppliers.UpdateAsync(id, req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "UPDATE", "DNHACUNGCAP", id,
            oldValue: new { old.Name, old.DiaChi, old.DienThoai, old.Email, old.Status },
            newValue: req, ip, ua, HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<SupplierDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id}")]
    [RequirePermission("DEBT.SUPPLIER.DELETE")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var old = await suppliers.GetAsync(id, ct);
        await suppliers.DeleteAsync(id, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "SOFT_DELETE", "DNHACUNGCAP", id,
            oldValue: new { old.Name, old.MaNhaCungCap }, ip: ip, userAgent: ua,
            correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}

/// <summary>Danh mục kho hàng (DKHOHANG) — Phase 4, đọc + ghi mirror MySQL.</summary>
[ApiController]
[Route("api/warehouses")]
[Authorize]
public sealed class WarehousesController(WarehouseService warehouses, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("INVENTORY.STOCK.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] bool? activeOnly,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "NAME", CancellationToken ct = default)
    {
        var result = await warehouses.SearchAsync(
            new WarehouseListQuery(search, activeOnly, page, pageSize, sort), ct);
        return Ok(ApiResult<PageResult<WarehouseDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id}")]
    [RequirePermission("INVENTORY.STOCK.VIEW")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
        => Ok(ApiResult<WarehouseDto>.Ok(await warehouses.GetAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("INVENTORY.WAREHOUSE.CREATE")]
    public async Task<IActionResult> Create([FromBody] WarehouseCreateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await warehouses.CreateAsync(req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "CREATE", "DKHOHANG", result.Id,
            newValue: req, ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CatalogCreateResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id}")]
    [RequirePermission("INVENTORY.WAREHOUSE.UPDATE")]
    public async Task<IActionResult> Update(string id, [FromBody] WarehouseUpdateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var old = await warehouses.GetAsync(id, ct);
        var result = await warehouses.UpdateAsync(id, req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "UPDATE", "DKHOHANG", id,
            oldValue: new { old.Name, old.Note, old.ParentId, old.ChoPhepAmKho, old.Status },
            newValue: req, ip, ua, HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<WarehouseDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id}")]
    [RequirePermission("INVENTORY.WAREHOUSE.DELETE")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var old = await warehouses.GetAsync(id, ct);
        await warehouses.DeleteAsync(id, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "SOFT_DELETE", "DKHOHANG", id,
            oldValue: new { old.Name }, ip: ip, userAgent: ua,
            correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}
