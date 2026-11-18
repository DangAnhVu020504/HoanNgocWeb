using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Catalog;
using TechPos.Application.Notifications;
using TechPos.Application.Pos;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>POS Bán lẻ (mục 4.1) + Bán sỉ (4.2) + Trả lại KH (4.3).</summary>
[ApiController]
[Route("api/pos")]
[Authorize]
public sealed class PosController(PosSaleService pos, WholesaleService wholesale, CustomerReturnService returns,
    CustomerService customers, EmailService emails) : ControllerBase
{
    /// <summary>Tính tổng tiền preview (không ghi DB).</summary>
    [HttpPost("quote")]
    [RequirePermission("SALES.INVOICE.CREATE")]
    public IActionResult Compute([FromBody] PosComputeRequest req)
    {
        var totals = pos.ComputeTotals(req.Lines, req.TileGiamGia, req.TienGiamGia, req.KhachDua);
        return Ok(ApiResult<SaleTotals>.Ok(totals, HttpContext.TraceIdentifier));
    }

    /// <summary>Số hoá đơn bán lẻ kế tiếp (tự động, chỉ xem trước — không tăng sequence).</summary>
    [HttpGet("next-number")]
    [RequirePermission("SALES.INVOICE.CREATE")]
    public async Task<IActionResult> NextNumber(CancellationToken ct)
    {
        var soPhieu = await pos.GetNextInvoiceNumberAsync(ct);
        return Ok(ApiResult<string>.Ok(soPhieu, HttpContext.TraceIdentifier));
    }

    /// <summary>Thanh toán (F12) — tạo hoá đơn bán lẻ.</summary>
    [HttpPost("sales")]
    [RequirePermission("SALES.INVOICE.CREATE")]
    public async Task<IActionResult> Create([FromBody] PosCreateRequest body, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value ?? "";
        var result = await pos.CreateSaleAsync(body.ToSaleRequest(), userId, body.KhachDua, HttpContext.RequestAborted);
        await SendSaleEmailsAsync(body.KhachHangId, result, ct);
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
        await SendSaleEmailsAsync(body.KhachHangId, result, ct);
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

    private async Task SendSaleEmailsAsync(string? khachHangId, PosSaleResult result, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(khachHangId)) return;
        try
        {
            var kh = await customers.GetAsync(khachHangId, ct);
            if (string.IsNullOrWhiteSpace(kh.Email)) return;
            var variables = new Dictionary<string, string?>
            {
                ["CUSTOMER_NAME"] = kh.Name,
                ["ORDER_NUMBER"] = result.SoPhieu,
                ["ORDER_DATE"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                ["TOTAL_AMOUNT"] = result.TongCong.ToString("N0") + " đ",
                ["DEBT_AMOUNT"] = result.ConNo.ToString("N0") + " đ"
            };
            await emails.EnqueueFromEventAsync("ORDER_CREATED", [(kh.Email!, kh.Name)], variables,
                "TDONHANG", result.DonHangId, ct);
            if (result.ConNo > 0)
                await emails.EnqueueFromEventAsync("DEBT_CREATED", [(kh.Email!, kh.Name)], variables,
                    "TDONHANG", result.DonHangId, ct);
        }
        catch (Exception)
        {
        }
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
    decimal KhachDua = 0,
    string? DienGiai = null,
    decimal TienThue = 0,
    decimal PhiVanChuyen = 0)
{
    public PosSaleRequest ToSaleRequest() => new(KhoId, KhachHangId, NhanVienId, Lines, TienGiamGia, TileGiamGia, GhiChu, DienGiai, TienThue, PhiVanChuyen);
}
