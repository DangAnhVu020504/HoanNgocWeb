using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Notifications;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Notification Center (Phase 7 — mục 25/47): list/unread/mark-read/mark-all-read.</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(NotificationService service) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? group = null,
        CancellationToken ct = default)
    {
        var uid = CurrentUser.Info(HttpContext).UserId;
        var result = await service.SearchAsync(new NotificationListQuery(page, pageSize, group), uid, ct);
        return Ok(ApiResult<PageResult<NotificationDto>>.Ok(new PageResult<NotificationDto>
        {
            Items = result.Items, Page = page, PageSize = pageSize, Total = result.Total
        }, HttpContext.TraceIdentifier));
    }

    /// <summary>Số thông báo chưa đọc (poll 30s hoặc push qua SignalR).</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
        => Ok(ApiResult<long>.Ok(await service.UnreadCountAsync(CurrentUser.Info(HttpContext).UserId, ct),
            HttpContext.TraceIdentifier));

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(string id, CancellationToken ct)
    {
        await service.MarkReadAsync(id, CurrentUser.Info(HttpContext).UserId, ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await service.MarkAllReadAsync(CurrentUser.Info(HttpContext).UserId, ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}

/// <summary>Trung tâm email Admin (Phase 7 — API-STRUCTURE /api/emails): template/event/queue/settings.</summary>
[ApiController]
[Route("api/emails")]
[Authorize]
public sealed class EmailsController(EmailService emails) : ControllerBase
{
    [HttpGet("templates")]
    [RequirePermission("ADMIN.SETTINGS.MANAGE")]
    public async Task<IActionResult> Templates(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<EmailTemplateDto>>.Ok(await emails.GetTemplatesAsync(ct), HttpContext.TraceIdentifier));

    [HttpPut("templates/{code}")]
    [RequirePermission("EMAIL.TEMPLATE.MANAGE")]
    public async Task<IActionResult> UpdateTemplate(string code, [FromBody] EmailTemplateUpdateRequest req, CancellationToken ct)
    {
        await emails.UpdateTemplateAsync(code, req, ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    [HttpGet("events")]
    [RequirePermission("ADMIN.SETTINGS.MANAGE")]
    public async Task<IActionResult> Events(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<EmailEventDto>>.Ok(await emails.GetEventsAsync(ct), HttpContext.TraceIdentifier));

    [HttpPut("events/{code}/enabled")]
    [RequirePermission("EMAIL.TEMPLATE.MANAGE")]
    public async Task<IActionResult> SetEventEnabled(string code, [FromBody] bool enabled, CancellationToken ct)
    {
        await emails.SetEventEnabledAsync(code, enabled, ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    [HttpGet("queue")]
    [RequirePermission("EMAIL.HISTORY.VIEW")]
    public async Task<IActionResult> Queue(
        [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await emails.SearchQueueAsync(status, page, pageSize, ct);
        return Ok(ApiResult<PageResult<EmailQueueItemDto>>.Ok(new PageResult<EmailQueueItemDto>
        {
            Items = result.Items, Page = page, PageSize = pageSize, Total = result.Total
        }, HttpContext.TraceIdentifier));
    }

    [HttpPost("queue/{id}/retry")]
    [RequirePermission("EMAIL.TEMPLATE.MANAGE")]
    public async Task<IActionResult> Retry(string id, CancellationToken ct)
        => Ok(ApiResult<bool>.Ok(await emails.RetryQueueItemAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpPost("queue/{id}/cancel")]
    [RequirePermission("EMAIL.TEMPLATE.MANAGE")]
    public async Task<IActionResult> Cancel(string id, CancellationToken ct)
        => Ok(ApiResult<bool>.Ok(await emails.CancelQueueItemAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpGet("settings")]
    [RequirePermission("ADMIN.SETTINGS.MANAGE")]
    public async Task<IActionResult> Settings(CancellationToken ct)
        => Ok(ApiResult<Dictionary<string, string?>>.Ok(await emails.GetSettingsAsync(ct), HttpContext.TraceIdentifier));

    [HttpPost("settings")]
    [RequirePermission("EMAIL.TEMPLATE.MANAGE")]
    public async Task<IActionResult> SaveSettings([FromBody] EmailSettingsSaveRequest req, CancellationToken ct)
    {
        await emails.SaveSettingsAsync(req, ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    /// <summary>Gửi 1 email test trực tiếp qua SMTP đã cấu hình (EMAIL-ARCHITECTURE §2).</summary>
    [HttpPost("test-send")]
    [RequirePermission("EMAIL.TEMPLATE.MANAGE")]
    public async Task<IActionResult> TestSend([FromBody] EmailSendRequest req, CancellationToken ct)
    {
        var (ok, err) = await emails.TestSendAsync(req, ct);
        if (ok) return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
        return Ok(ApiResult<bool>.Fail(err ?? "Gửi email thất bại", HttpContext.TraceIdentifier));
    }
}
