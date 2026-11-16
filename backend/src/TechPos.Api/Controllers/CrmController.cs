using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Admin;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>CSKH — lịch sử chăm sóc khách hàng.</summary>
[ApiController]
[Route("api/crm/care")]
[Authorize]
public sealed class CrmCareController(CrmService crm, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("CRM.CARE.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] string? careType,
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(ApiResult<PageResult<CustomerCareDto>>.Ok(
            await crm.SearchCareAsync(new CustomerCareQuery(search, careType, fromDate, toDate, page, pageSize), ct),
            HttpContext.TraceIdentifier));

    [HttpGet("{id}")]
    [RequirePermission("CRM.CARE.VIEW")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
        => Ok(ApiResult<CustomerCareDto>.Ok(await crm.GetCareAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("CRM.CARE.CREATE")]
    public async Task<IActionResult> Create([FromBody] CustomerCareCreateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await crm.CreateCareAsync(req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "CREATE", "W_CUSTOMER_CARE", result.Id,
            newValue: req, ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CustomerCareDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id}")]
    [RequirePermission("CRM.CARE.UPDATE")]
    public async Task<IActionResult> Update(string id, [FromBody] CustomerCareUpdateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await crm.UpdateCareAsync(id, req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "UPDATE", "W_CUSTOMER_CARE", id,
            newValue: req, ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CustomerCareDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id}")]
    [RequirePermission("CRM.CARE.DELETE")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        await crm.DeleteCareAsync(id, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "DELETE", "W_CUSTOMER_CARE", id,
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}

/// <summary>CSKH — phản hồi/đánh giá khách hàng.</summary>
[ApiController]
[Route("api/crm/feedback")]
[Authorize]
public sealed class CrmFeedbackController(CrmService crm, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("CRM.FEEDBACK.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] int? rating,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(ApiResult<PageResult<CustomerFeedbackDto>>.Ok(
            await crm.SearchFeedbackAsync(new CustomerFeedbackQuery(search, status, rating, page, pageSize), ct),
            HttpContext.TraceIdentifier));

    [HttpGet("{id}")]
    [RequirePermission("CRM.FEEDBACK.VIEW")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
        => Ok(ApiResult<CustomerFeedbackDto>.Ok(await crm.GetFeedbackAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("CRM.FEEDBACK.CREATE")]
    public async Task<IActionResult> Create([FromBody] CustomerFeedbackCreateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await crm.CreateFeedbackAsync(req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "CREATE", "W_CUSTOMER_FEEDBACK", result.Id,
            newValue: req, ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CustomerFeedbackDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id}/respond")]
    [RequirePermission("CRM.FEEDBACK.RESPOND")]
    public async Task<IActionResult> Respond(string id, [FromBody] CustomerFeedbackRespondRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await crm.RespondFeedbackAsync(id, req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "UPDATE", "W_CUSTOMER_FEEDBACK", id,
            newValue: req, ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CustomerFeedbackDto>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>CSKH — tin nhắn Email/SMS/Zalo.</summary>
[ApiController]
[Route("api/crm/messages")]
[Authorize]
public sealed class CrmMessagesController(CrmService crm, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("CRM.MESSAGE.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? channel, [FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(ApiResult<PageResult<MessageDto>>.Ok(
            await crm.SearchMessagesAsync(new MessageQuery(channel, status, search, page, pageSize), ct),
            HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("CRM.MESSAGE.SEND")]
    public async Task<IActionResult> Send([FromBody] MessageSendRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await crm.SendMessageAsync(req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "SEND", "W_MESSAGE", result.Id,
            newValue: new { req.Channel, req.CustomerId, result.Recipient, req.Subject },
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<MessageDto>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>CSKH — điểm thân thiết.</summary>
[ApiController]
[Route("api/crm/loyalty")]
[Authorize]
public sealed class CrmLoyaltyController(CrmService crm, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("CRM.LOYALTY.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(ApiResult<PageResult<LoyaltySummaryDto>>.Ok(
            await crm.SearchLoyaltyAsync(new LoyaltyQuery(search, page, pageSize), ct),
            HttpContext.TraceIdentifier));

    [HttpGet("{customerId}/transactions")]
    [RequirePermission("CRM.LOYALTY.VIEW")]
    public async Task<IActionResult> Transactions(string customerId, CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<LoyaltyTxnDto>>.Ok(await crm.GetLoyaltyTxnsAsync(customerId, ct),
            HttpContext.TraceIdentifier));

    [HttpPost("adjust")]
    [RequirePermission("CRM.LOYALTY.ADJUST")]
    public async Task<IActionResult> Adjust([FromBody] LoyaltyAdjustRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await crm.AdjustLoyaltyAsync(req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "ADJUST", "W_LOYALTY_TXN", req.CustomerId,
            newValue: new { req.Points, req.Note, result.Applied, result.NewPoints },
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<LoyaltyAdjustResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}
