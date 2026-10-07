using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Inventory;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Tồn kho + Thẻ kho (mục 4.8) — đọc OTONKHO + W_KHOLEDGER.</summary>
[ApiController]
[Route("api/inventory")]
[Authorize]
public sealed class InventoryController(InventoryService inventory) : ControllerBase
{
    [HttpGet]
    [RequirePermission("INVENTORY.STOCK.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] string? khoId,
        [FromQuery] bool? nonZeroOnly, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string sort = "NAME", CancellationToken ct = default)
    {
        var result = await inventory.SearchAsync(
            new InventoryListQuery(search, khoId, nonZeroOnly, page, pageSize, sort), ct);
        return Ok(ApiResult<PageResult<InventoryDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Thẻ kho 1 mặt hàng (tồn lũy kế từ W_KHOLEDGER).</summary>
    [HttpGet("ledger/{matHangId}")]
    [RequirePermission("INVENTORY.STOCK.VIEW")]
    public async Task<IActionResult> Ledger(
        string matHangId, [FromQuery] string? khoId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var rows = await inventory.GetLedgerAsync(new LedgerQuery(matHangId, khoId, from, to), ct);
        return Ok(ApiResult<IReadOnlyList<LedgerEntryDto>>.Ok(rows, HttpContext.TraceIdentifier));
    }

    /// <summary>Tồn tất cả các kho (mục 4.8) — ma trận mặt hàng × kho.</summary>
    [HttpGet("all-warehouses")]
    [RequirePermission("INVENTORY.STOCK.VIEW")]
    public async Task<IActionResult> AllWarehouses(
        [FromQuery] string? search, [FromQuery] bool? nonZeroOnly,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await inventory.SearchAllWarehousesAsync(
            new AllWarehouseStockQuery(search, nonZeroOnly, page, pageSize), ct);
        return Ok(ApiResult<PageResult<AllWarehouseStockDto>>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Tính lại giá vốn (mục 4.8) — FIFO / bình quân liên hoàn / bình quân cuối kỳ.</summary>
[ApiController]
[Route("api/inventory/cost-recalc")]
[Authorize]
public sealed class CostRecalcController(CostRecalcService recalc) : ControllerBase
{
    /// <summary>Thực hiện tính lại giá vốn từ ngày — ghi W_KHOLEDGER.GIA_VON
    /// + TDONHANGCHITIET.GIAVON (chỉ dòng web).</summary>
    [HttpPost]
    [RequirePermission("INVENTORY.COST_RECALC")]
    public async Task<IActionResult> Recalculate([FromBody] CostRecalcRequest req, CancellationToken ct)
    {
        var result = await recalc.RecalculateAsync(req, ct);
        return Ok(ApiResult<CostRecalcResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Phiếu nhập kho (mục 4.4).</summary>
[ApiController]
[Route("api/receipts")]
[Authorize]
public sealed class ReceiptsController(InventoryService inventory) : ControllerBase
{
    /// <summary>Tạo phiếu nhập kho — ghi TDONHANG(LOAI=1) + W_KHOLEDGER + OTONKHO(+).</summary>
    [HttpPost]
    [RequirePermission("INVENTORY.RECEIPT.CREATE")]
    public async Task<IActionResult> Create([FromBody] ReceiptRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await inventory.CreateReceiptAsync(req, userId, ct);
        return Ok(ApiResult<ReceiptResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Phiếu kiểm kê (mục 4.7).</summary>
[ApiController]
[Route("api/stocktakes")]
[Authorize]
public sealed class StocktakesController(InventoryService inventory) : ControllerBase
{
    /// <summary>Tạo phiếu kiểm kê — ghi TDONHANG(LOAI=5 KTL) + W_KHOLEDGER + OTONKHO về SL thực tế.</summary>
    [HttpPost]
    [RequirePermission("INVENTORY.STOCKTAKE.CREATE")]
    public async Task<IActionResult> Create([FromBody] StocktakeRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await inventory.CreateStocktakeAsync(req, userId, ct);
        return Ok(ApiResult<StocktakeResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Phiếu xuất kho (mục 4.5).</summary>
[ApiController]
[Route("api/inventory/issues")]
[Authorize]
public sealed class StockIssuesController(InventoryService inventory) : ControllerBase
{
    /// <summary>Tạo phiếu xuất kho — ghi TDONHANG + TDONHANGCHITIET(SLXUAT) + W_KHOLEDGER + OTONKHO(−).</summary>
    [HttpPost]
    [RequirePermission("INVENTORY.ISSUE.CREATE")]
    public async Task<IActionResult> Create([FromBody] StockIssueRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await inventory.CreateIssueAsync(req, userId, ct);
        return Ok(ApiResult<StockIssueResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Phiếu chuyển kho (mục 4.6).</summary>
[ApiController]
[Route("api/inventory/transfers")]
[Authorize]
public sealed class StockTransfersController(InventoryService inventory) : ControllerBase
{
    /// <summary>Tạo phiếu chuyển kho — xuất kho A (+) nhập kho B, ledger 'CHUYENKHO' 2 chiều.</summary>
    [HttpPost]
    [RequirePermission("INVENTORY.TRANSFER.CREATE")]
    public async Task<IActionResult> Create([FromBody] StockTransferRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await inventory.CreateTransferAsync(req, userId, ct);
        return Ok(ApiResult<StockTransferResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Phiếu xuất trả NCC (mục 4.5).</summary>
[ApiController]
[Route("api/inventory/supplier-returns")]
[Authorize]
public sealed class SupplierReturnsController(InventoryService inventory) : ControllerBase
{
    /// <summary>Tạo phiếu xuất trả NCC — ghi TDONHANG + SLXUAT + W_KHOLEDGER 'XUATTRANCC' + OTONKHO(−).</summary>
    [HttpPost]
    [RequirePermission("INVENTORY.SUPPLIER_RETURN.CREATE")]
    public async Task<IActionResult> Create([FromBody] SupplierReturnRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await inventory.CreateSupplierReturnAsync(req, userId, ct);
        return Ok(ApiResult<SupplierReturnResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}
