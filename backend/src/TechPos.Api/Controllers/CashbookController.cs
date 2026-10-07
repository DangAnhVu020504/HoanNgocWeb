using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Cashbook;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Quỹ (mục 5.1) — lý do thu chi, phiếu thu/chi, tồn quỹ.</summary>
[ApiController]
[Route("api/cashbook")]
[Authorize]
public sealed class CashbookController(CashbookService cashbook) : ControllerBase
{
    /// <summary>Danh mục lý do thu chi (DLYDOTHUCHI).</summary>
    [HttpGet("reasons")]
    [RequirePermission("CASHBOOK.RECEIPT.VIEW")]
    public async Task<IActionResult> Reasons(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<CashReasonDto>>.Ok(await cashbook.GetReasonsAsync(ct), HttpContext.TraceIdentifier));

    /// <summary>Tồn quỹ theo kỳ (tiền mặt + chuyển khoản).</summary>
    [HttpGet("balance")]
    [RequirePermission("CASHBOOK.RECEIPT.VIEW")]
    public async Task<IActionResult> Balance([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(ApiResult<CashBalanceDto>.Ok(
            await cashbook.GetBalanceAsync(new CashBalanceQuery(from, to), ct), HttpContext.TraceIdentifier));

    /// <summary>Danh sách phiếu thu/chi.</summary>
    [HttpGet("vouchers")]
    [RequirePermission("CASHBOOK.RECEIPT.VIEW")]
    public async Task<IActionResult> Vouchers(
        [FromQuery] bool? isThu, [FromQuery] string? search,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await cashbook.SearchAsync(
            new CashVoucherListQuery(isThu, search, from, to, page, pageSize), ct);
        return Ok(ApiResult<PageResult<CashVoucherDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Tạo phiếu thu (PTyy/xxxxx) — THU từ KH/NCC, có phân bổ trả nợ hóa đơn web.</summary>
    [HttpPost("receipts")]
    [RequirePermission("CASHBOOK.RECEIPT.CREATE")]
    public async Task<IActionResult> CreateReceipt([FromBody] CashVoucherRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await cashbook.CreateReceiptAsync(req, userId, ct);
        return Ok(ApiResult<CashVoucherResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Tạo phiếu chi (PCyy/xxxxx) — CHI cho KH/NCC, chi trả NCC có phân bổ nợ.</summary>
    [HttpPost("payments")]
    [RequirePermission("CASHBOOK.PAYMENT.CREATE")]
    public async Task<IActionResult> CreatePayment([FromBody] CashVoucherRequest req, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await cashbook.CreatePaymentAsync(req, userId, ct);
        return Ok(ApiResult<CashVoucherResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Công nợ (mục 5.2) — công nợ KH / công nợ NCC.</summary>
[ApiController]
[Route("api/debts")]
[Authorize]
public sealed class DebtsController(DebtService debts) : ControllerBase
{
    /// <summary>Công nợ khách hàng — SUM(TDONHANG.CONNO) theo khách.</summary>
    [HttpGet("customers")]
    [RequirePermission("DEBT.CUSTOMER.VIEW")]
    public async Task<IActionResult> Customers(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await debts.SearchCustomerDebtsAsync(new CustomerDebtQuery(search, page, pageSize), ct);
        return Ok(ApiResult<PageResult<CustomerDebtDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Công nợ nhà cung cấp — SUM(TDONHANG.CONLAI) theo NCC.</summary>
    [HttpGet("suppliers")]
    [RequirePermission("DEBT.SUPPLIER.VIEW")]
    public async Task<IActionResult> Suppliers(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await debts.SearchSupplierDebtsAsync(new SupplierDebtQuery(search, page, pageSize), ct);
        return Ok(ApiResult<PageResult<SupplierDebtDto>>.Ok(result, HttpContext.TraceIdentifier));
    }
}