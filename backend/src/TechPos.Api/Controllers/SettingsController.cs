using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Admin;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Cài đặt hệ thống (thông tin công ty cho PDF, tham số điểm thân thiết...).</summary>
[ApiController]
[Route("api/settings")]
[Authorize]
public sealed class SettingsController(SettingsService settings, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("ADMIN.SETTINGS.MANAGE")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<AppSettingDto>>.Ok(await settings.GetAllAsync(ct),
            HttpContext.TraceIdentifier));

    [HttpGet("company")]
    public async Task<IActionResult> CompanyInfo(CancellationToken ct)
        => Ok(ApiResult<CompanyInfoDto>.Ok(await settings.GetCompanyInfoAsync(ct),
            HttpContext.TraceIdentifier));

    [HttpPut]
    [RequirePermission("ADMIN.SETTINGS.MANAGE")]
    public async Task<IActionResult> Save([FromBody] AppSettingsSaveRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        await settings.SaveAsync(req, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "UPDATE", "W_APP_SETTING", "SETTINGS",
            newValue: req.Settings, ip: ip, userAgent: ua,
            correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}
