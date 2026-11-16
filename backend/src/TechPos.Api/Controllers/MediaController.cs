using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Admin;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Thư viện media: metadata W_MEDIA + phục vụ file (ảnh mặt hàng, menu, báo cáo...).</summary>
[ApiController]
[Route("api/media")]
[Authorize]
public sealed class MediaController(MediaService media) : ControllerBase
{
    [HttpGet]
    [RequirePermission("MEDIA.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] string? referenceType, [FromQuery] string? mimeType,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 24, CancellationToken ct = default)
    {
        var result = await media.SearchAsync(new MediaQuery(search, referenceType, mimeType, page, pageSize), ct);
        return Ok(ApiResult<PageResult<MediaItemDto>>.Ok(new PageResult<MediaItemDto>
        {
            Items = result.Items, Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 100), Total = result.Total
        }, HttpContext.TraceIdentifier));
    }

    [HttpGet("reference-types")]
    [RequirePermission("MEDIA.VIEW")]
    public async Task<IActionResult> ReferenceTypes(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<string>>.Ok(await media.GetReferenceTypesAsync(ct),
            HttpContext.TraceIdentifier));

    [HttpGet("{id}/file")]
    [RequirePermission("MEDIA.VIEW")]
    public async Task<IActionResult> File(string id, CancellationToken ct)
    {
        var item = await media.GetAsync(id, ct);
        if (item is null) return NotFound();
        var path = media.ResolvePhysicalPath(item);
        if (path is null) return NotFound();
        return PhysicalFile(path, item.MimeType ?? "application/octet-stream", enableRangeProcessing: true);
    }
}
