using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Sales;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

internal static class ControllerUserExtensions
{
    public static string GetUserId(this ControllerBase c) =>
        c.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? c.User.FindFirst("sub")?.Value ?? "";
}

/// <summary>Báo giá (mục 4.9).</summary>
[ApiController]
[Route("api/quotations")]
[Authorize]
public sealed class QuotationsController(CustomerDocService docs) : ControllerBase
{
    [HttpPost]
    [RequirePermission("SALES.QUOTATION.CREATE")]
    public async Task<IActionResult> Create([FromBody] CustomerDocRequest req, CancellationToken ct)
    {
        var result = await docs.CreateQuoteAsync(req, this.GetUserId(), ct);
        return Ok(ApiResult<QuoteResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Đặt hàng của khách (mục 4.9).</summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(CustomerDocService docs) : ControllerBase
{
    [HttpPost]
    [RequirePermission("SALES.ORDER.CREATE")]
    public async Task<IActionResult> Create([FromBody] CustomerDocRequest req, CancellationToken ct)
    {
        var result = await docs.CreateOrderAsync(req, this.GetUserId(), ct);
        return Ok(ApiResult<OrderResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Đặt mua nhà cung cấp (mục 4.9).</summary>
[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public sealed class PurchaseOrdersController(PurchaseOrderService purchase) : ControllerBase
{
    [HttpPost]
    [RequirePermission("PURCHASE.ORDER.CREATE")]
    public async Task<IActionResult> Create([FromBody] PurchaseOrderRequest req, CancellationToken ct)
    {
        var result = await purchase.CreateAsync(req, this.GetUserId(), ct);
        return Ok(ApiResult<PurchaseOrderResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Hợp đồng (mục 4.9).</summary>
[ApiController]
[Route("api/contracts")]
[Authorize]
public sealed class ContractsController(ContractService contracts) : ControllerBase
{
    [HttpPost]
    [RequirePermission("SALES.CONTRACT.CREATE")]
    public async Task<IActionResult> Create([FromBody] ContractRequest req, CancellationToken ct)
    {
        var result = await contracts.CreateAsync(req, this.GetUserId(), ct);
        return Ok(ApiResult<ContractResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Giao hàng COD (mục 4.10).</summary>
[ApiController]
[Route("api/cod")]
[Authorize]
public sealed class CodController(DeliveryService delivery, LookupRepository lookups) : ControllerBase
{
    /// <summary>Tra cứu đơn vị chuyển phát (DDONVICHUYENPHAT).</summary>
    [HttpGet("delivery-partners")]
    [RequirePermission("SALES.COD.CREATE")]
    public async Task<IActionResult> DeliveryPartners(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<LookupItem>>.Ok(await lookups.GetDeliveryPartnersAsync(ct), HttpContext.TraceIdentifier));

    /// <summary>Tra cứu xe giao hàng (DXEGIAOHANG).</summary>
    [HttpGet("vehicles")]
    [RequirePermission("SALES.COD.CREATE")]
    public async Task<IActionResult> Vehicles(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<LookupItem>>.Ok(await lookups.GetVehiclesAsync(ct), HttpContext.TraceIdentifier));

    /// <summary>Đơn hàng web để gắn vào phiếu COD / giao xe.</summary>
    [HttpGet("orders")]
    [RequirePermission("SALES.COD.CREATE")]
    public async Task<IActionResult> WebOrders(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<WebOrderItem>>.Ok(await lookups.GetWebOrdersAsync(ct), HttpContext.TraceIdentifier));

    /// <summary>Tạo phiếu gửi hàng COD (CODyy/xxxxx).</summary>
    [HttpPost("shipments")]
    [RequirePermission("SALES.COD.CREATE")]
    public async Task<IActionResult> CreateShipment([FromBody] CodShipmentRequest req, CancellationToken ct)
    {
        var result = await delivery.CreateCodShipmentAsync(req, this.GetUserId(), ct);
        return Ok(ApiResult<CodShipmentResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Nhập kết quả gửi COD (KQyy/xxxxx).</summary>
    [HttpPost("results")]
    [RequirePermission("SALES.COD.RESULT")]
    public async Task<IActionResult> CreateResult([FromBody] CodResultRequest req, CancellationToken ct)
    {
        var result = await delivery.CreateCodResultAsync(req, this.GetUserId(), ct);
        return Ok(ApiResult<CodResultEntry>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Giao hàng theo xe (mục 4.10).</summary>
[ApiController]
[Route("api/vehicle-deliveries")]
[Authorize]
public sealed class VehicleDeliveriesController(DeliveryService delivery) : ControllerBase
{
    [HttpPost]
    [RequirePermission("SALES.VEHICLE_DELIVERY.CREATE")]
    public async Task<IActionResult> Create([FromBody] VehicleDeliveryRequest req, CancellationToken ct)
    {
        var result = await delivery.CreateVehicleDeliveryAsync(req, this.GetUserId(), ct);
        return Ok(ApiResult<VehicleDeliveryResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}