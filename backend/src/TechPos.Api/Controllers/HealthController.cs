using Microsoft.AspNetCore.Mvc;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Health + parity status (mục 37).</summary>
[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
        => Ok(ApiResult<object>.Ok(new { status = "UP", utc = DateTime.UtcNow }, HttpContext.TraceIdentifier));
}