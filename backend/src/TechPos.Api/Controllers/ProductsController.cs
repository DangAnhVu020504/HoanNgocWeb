using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Application.Products;
using TechPos.Application.Reports;
using TechPos.Api.Middleware;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>
/// Danh mục mặt hàng (mục 60): list/search/detail/barcode/groups/stock + CRUD Phase 4
/// (ghi mirror MySQL, W_ORIGIN='WEB', audit W_AUDIT_LOG).
/// </summary>
[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController(ProductService products, CatalogImportService import, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("SALES.PRODUCT.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] string? groupId,
        [FromQuery] bool? activeOnly, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "NAME", CancellationToken ct = default)
    {
        var result = await products.SearchAsync(
            new ProductListQuery(search, groupId, activeOnly, page, pageSize, sort), ct);
        return Ok(ApiResult<PageResult<ProductDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id}")]
    [RequirePermission("SALES.PRODUCT.VIEW")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
        => Ok(ApiResult<ProductDto>.Ok(await products.GetAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpGet("barcode/{barcode}")]
    [RequirePermission("SALES.PRODUCT.VIEW")]
    public async Task<IActionResult> ByBarcode(string barcode, CancellationToken ct)
    {
        var p = await products.FindByBarcodeAsync(barcode, ct);
        return p is null
            ? Ok(ApiResult<string>.Fail("Không tìm thấy mặt hàng theo mã", HttpContext.TraceIdentifier))
            : Ok(ApiResult<ProductDto>.Ok(p, HttpContext.TraceIdentifier));
    }

    [HttpGet("groups")]
    [RequirePermission("SALES.PRODUCT.VIEW")]
    public async Task<IActionResult> Groups(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<ProductGroupDto>>.Ok(
            await products.GetGroupsAsync(ct), HttpContext.TraceIdentifier));

    [HttpGet("{id}/stock")]
    [RequirePermission("SALES.PRODUCT.VIEW")]
    public async Task<IActionResult> Stock(string id, CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<ProductStockDto>>.Ok(
            await products.GetStockAsync(id, ct), HttpContext.TraceIdentifier));

    // ---------- Phase 4 CRUD ----------

    [HttpPost]
    [RequirePermission("SALES.PRODUCT.CREATE")]
    public async Task<IActionResult> Create([FromBody] ProductCreateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await products.CreateAsync(req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "CREATE", "DMATHANG", result.Id,
            newValue: req, ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CatalogCreateResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id}")]
    [RequirePermission("SALES.PRODUCT.UPDATE")]
    public async Task<IActionResult> Update(string id, [FromBody] ProductUpdateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var old = await products.GetAsync(id, ct);
        var result = await products.UpdateAsync(id, req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "UPDATE", "DMATHANG", id,
            oldValue: new { old.Name, old.Code, old.GiaBan, old.GiaNhap, old.TonToiThieu, old.TonToiDa, old.Status },
            newValue: req, ip, ua, HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<ProductDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id}")]
    [RequirePermission("SALES.PRODUCT.DELETE")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var old = await products.GetAsync(id, ct);
        await products.DeleteAsync(id, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "SOFT_DELETE", "DMATHANG", id,
            oldValue: new { old.Name, old.Code }, ip: ip, userAgent: ua,
            correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Nhập Excel danh mục mặt hàng (Phase 6.4) — cột như file xuất: Mã|Tên|Nhóm|...|Giá bán|Giá nhập|...</summary>
    [HttpPost("import")]
    [RequirePermission("SALES.PRODUCT.CREATE")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Ok(ApiResult<string>.Fail("Chưa chọn file Excel", HttpContext.TraceIdentifier));
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await import.ImportProductsAsync(ms.ToArray(), uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "IMPORT", "DMATHANG", null,
            newValue: new { file.FileName, result.Created, result.Skipped, SoLoi = result.Errors.Count },
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<ImportResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}
