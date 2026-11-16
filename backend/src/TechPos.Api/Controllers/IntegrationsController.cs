using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TechPos.Api.Middleware;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Trạng thái cấu hình tích hợp đối tác (không trả secret).</summary>
[ApiController]
[Route("api/integrations")]
[Authorize]
public sealed class IntegrationsController(IOptions<IntegrationOptions> options) : ControllerBase
{
    [HttpGet("status")]
    [RequirePermission("ADMIN.SETTINGS.MANAGE")]
    public IActionResult Status()
    {
        var o = options.Value;
        var codMissing = new List<string>();
        if (!o.Cod.Enabled) codMissing.Add("Enabled");
        if (string.IsNullOrWhiteSpace(o.Cod.Provider)) codMissing.Add("Provider");
        if (string.IsNullOrWhiteSpace(o.Cod.BaseUrl)) codMissing.Add("BaseUrl");
        if (string.IsNullOrWhiteSpace(o.Cod.ApiKey)) codMissing.Add("ApiKey");
        if (string.IsNullOrWhiteSpace(o.Cod.ApiSecret)) codMissing.Add("ApiSecret");

        var momoMissing = new List<string>();
        if (!o.MoMo.Enabled) momoMissing.Add("Enabled");
        if (string.IsNullOrWhiteSpace(o.MoMo.PartnerCode)) momoMissing.Add("PartnerCode");
        if (string.IsNullOrWhiteSpace(o.MoMo.AccessKey)) momoMissing.Add("AccessKey");
        if (string.IsNullOrWhiteSpace(o.MoMo.SecretKey)) momoMissing.Add("SecretKey");
        if (string.IsNullOrWhiteSpace(o.MoMo.Endpoint)) momoMissing.Add("Endpoint");
        if (string.IsNullOrWhiteSpace(o.MoMo.RedirectUrl)) momoMissing.Add("RedirectUrl");
        if (string.IsNullOrWhiteSpace(o.MoMo.IpnUrl)) momoMissing.Add("IpnUrl");

        return Ok(ApiResult<IntegrationsStatusDto>.Ok(new IntegrationsStatusDto(
            new IntegrationStatusDto(o.Cod.Enabled, o.Cod.IsConfigured, o.Cod.Provider, codMissing),
            new IntegrationStatusDto(o.MoMo.Enabled, o.MoMo.IsConfigured, "MoMo", momoMissing)
        ), HttpContext.TraceIdentifier));
    }
}
