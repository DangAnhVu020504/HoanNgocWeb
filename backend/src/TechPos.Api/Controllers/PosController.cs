using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Pos;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>POS Bán lẻ (mục 4.1) + Bán sỉ (4.2) + Trả lại KH (4.3).</summary>
[ApiController]
[Route("api/pos")]
[Authorize]
public sealed class PosController(PosSaleService pos, WholesaleService wholesale, CustomerReturnService returns) : ControllerBase
{
    /// <summary>Tính tổng tiền preview (không ghi DB).</summary>
    [HttpPost("quote")]
    [RequirePermission("SALES.INVOICE.CREATE")]
    public IActionResult Compute([FromBody] PosComputeRequest req)
    {
        var totals = pos.ComputeTotals(req.Lines, req.TileGiamGia, req.TienGiamGia, req.KhachDua);
        return Ok(ApiResult<SaleTotals>.Ok(totals, HttpContext.TraceIdentifier));
    }

    /// <summary>Thanh toán (F12) — tạo hoá đơn bán lẻ.</summary>
    [HttpPost("sales")]
    [RequirePermission("SALES.INVOICE.CREATE")]
    public async Task<IActionResult> Create([FromBody] PosCreateRequest body, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value ?? "";
        var result = await pos.CreateSaleAsync(body.ToSaleRequest(), userId, body.KhachDua, HttpContext.RequestAborted);
        return Ok(ApiResult<PosSaleResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Tạo hóa đơn bán sỉ (BS26/00001) — mục 4.2.</summary>
    [HttpPost("wholesale")]
    [RequirePermission("SALES.WHOLESALE.CREATE")]
    public async Task<IActionResult> CreateWholesale([FromBody] PosCreateRequest body, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value ?? "";
        var result = await wholesale.CreateSaleAsync(body.ToSaleRequest(), userId, body.KhachDua, HttpContext.RequestAborted);
        return Ok(ApiResult<PosSaleResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Tạo phiếu khách trả lại hàng — mục 4.3 (hàng về kho, tiền trả lại khách).</summary>
    [HttpPost("returns")]
    [RequirePermission("SALES.RETURN.CREATE")]
    public async Task<IActionResult> CreateReturn([FromBody] CustomerReturnRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value ?? "";
        var result = await returns.CreateAsync(req, userId, HttpContext.RequestAborted);
        return Ok(ApiResult<CustomerReturnResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

public sealed record PosComputeRequest(
    IReadOnlyList<PosCartItemDto> Lines,
    decimal TileGiamGia = 0,
    decimal TienGiamGia = 0,
    decimal KhachDua = 0);

public sealed record PosCreateRequest(
    string KhoId,
    string? KhachHangId,
    string? NhanVienId,
    IReadOnlyList<PosSaleLine> Lines,
    decimal TienGiamGia = 0,
    decimal TileGiamGia = 0,
    string? GhiChu = null,
    decimal KhachDua = 0)
{
    public PosSaleRequest ToSaleRequest() => new(KhoId, KhachHangId, NhanVienId, Lines, TienGiamGia, TileGiamGia, GhiChu);
}
