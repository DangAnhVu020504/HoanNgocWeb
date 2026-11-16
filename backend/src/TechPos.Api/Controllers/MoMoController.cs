using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Payments;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Thanh toán online MoMo (Phase 8) — cần cấu hình Integrations:MoMo.</summary>
[ApiController]
[Route("api/payments/momo")]
public sealed class MoMoController(MoMoService momo, PaymentRepository repo, MySqlConnectionFactory db) : ControllerBase
{
    /// <summary>Tạo giao dịch MoMo cho 1 đơn hàng; trả payUrl/qrCodeUrl để POS hiển thị.</summary>
    [HttpPost("create")]
    [Authorize]
    [RequirePermission("SALES.INVOICE.PAY")]
    public async Task<IActionResult> Create([FromBody] MoMoCreatePaymentRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await momo.CreatePaymentAsync(req, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "CREATE_PAYMENT", "W_PAYMENT_TXN", result.OrderId,
            newValue: new { req.DonHangId, req.Amount, Provider = MoMoService.Provider },
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<MoMoCreatePaymentResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>IPN MoMo gọi server-to-server — xác thực HMAC, idempotent theo orderId.</summary>
    [HttpPost("ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> Ipn([FromBody] MoMoIpnRequest req, CancellationToken ct)
    {
        await momo.HandleIpnAsync(req, ct);
        return NoContent();
    }

    /// <summary>Danh sách đơn web còn công nợ để chọn thanh toán MoMo.</summary>
    [HttpGet("orders")]
    [Authorize]
    [RequirePermission("SALES.INVOICE.PAY")]
    public async Task<IActionResult> Orders(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<PaymentOrderDto>>.Ok(
            await repo.GetPayableOrdersAsync(ct), HttpContext.TraceIdentifier));

    [HttpGet("{orderId}")]
    [Authorize]
    [RequirePermission("SALES.INVOICE.VIEW")]
    public async Task<IActionResult> Status(string orderId, CancellationToken ct)
        => Ok(ApiResult<MoMoPaymentStatusDto>.Ok(await momo.GetStatusAsync(orderId, ct),
            HttpContext.TraceIdentifier));
}
