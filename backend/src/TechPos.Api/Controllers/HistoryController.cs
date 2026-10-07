using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Lịch sử chứng từ — xem toàn bộ hoá đơn/phiếu/hợp đồng đã tạo trên web (chỉ đọc).</summary>
[ApiController]
[Route("api/history")]
[Authorize]
public sealed class HistoryController(HistoryRepository repo) : ControllerBase
{
    [HttpGet]
    [RequirePermission("ADMIN.AUDIT.VIEW")]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchAsync(new HistoryQuery(search, page, pageSize), ct);
        return Ok(ApiResult<object>.Ok(new { items, total, page, pageSize }, HttpContext.TraceIdentifier));
    }
}
