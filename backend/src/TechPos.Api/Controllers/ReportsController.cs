using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Reports;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>
/// Report Center (Phase 6 — ROADMAP): 6 báo cáo vận hành + xuất Excel + in PDF hoá đơn/phiếu thu chi.
/// Đọc READ-ONLY mirror MySQL. File trả về dạng FileContent (không bọc ApiResult).
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController(ReportService reports) : ControllerBase
{
    // ---------- Data endpoints (JSON) ----------

    /// <summary>Bán hàng theo ngày (TDONHANG xuất kho bán).</summary>
    [HttpGet("sales-by-day")]
    [RequirePermission("REPORT.VIEW")]
    public async Task<IActionResult> SalesByDay([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<SalesByDayRow>>.Ok(
            await reports.SalesByDayAsync(new ReportQuery(from, to), ct), HttpContext.TraceIdentifier));

    /// <summary>Mặt hàng bán chạy (TDONHANGCHITIET).</summary>
    [HttpGet("sales-by-item")]
    [RequirePermission("REPORT.VIEW")]
    public async Task<IActionResult> SalesByItem(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int top = 100, CancellationToken ct = default)
        => Ok(ApiResult<IReadOnlyList<SalesByItemRow>>.Ok(
            await reports.SalesByItemAsync(new ReportQuery(from, to, Top: top), ct), HttpContext.TraceIdentifier));

    /// <summary>Thu chi theo ngày (TTHUCHI).</summary>
    [HttpGet("cash-by-day")]
    [RequirePermission("REPORT.VIEW")]
    public async Task<IActionResult> CashByDay([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<CashByDayRow>>.Ok(
            await reports.CashByDayAsync(new ReportQuery(from, to), ct), HttpContext.TraceIdentifier));

    /// <summary>Thu chi theo lý do (TTHUCHI + DLYDOTHUCHI).</summary>
    [HttpGet("cash-by-reason")]
    [RequirePermission("REPORT.VIEW")]
    public async Task<IActionResult> CashByReason([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<CashByReasonRow>>.Ok(
            await reports.CashByReasonAsync(new ReportQuery(from, to), ct), HttpContext.TraceIdentifier));

    /// <summary>Xuất – nhập – tồn trong kỳ (W_KHOLEDGER).</summary>
    [HttpGet("xnt")]
    [RequirePermission("REPORT.VIEW")]
    public async Task<IActionResult> Xnt(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? khoId, CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<XntRow>>.Ok(
            await reports.XntAsync(new ReportQuery(from, to, khoId), ct), HttpContext.TraceIdentifier));

    /// <summary>Tồn kho (OTONKHO) — dùng cho export excel tồn kho.</summary>
    [HttpGet("stock")]
    [RequirePermission("REPORT.VIEW")]
    public async Task<IActionResult> Stock(
        [FromQuery] string? search, [FromQuery] string? khoId, [FromQuery] bool? nonZeroOnly, CancellationToken ct)
    {
        var result = await reports.StockAsync(new InventoryListQuery(search, khoId, nonZeroOnly, 1, Paging.MaxPageSize), ct);
        return Ok(ApiResult<PageResult<InventoryDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Công nợ khách hàng tổng hợp (mục 5.2/6.1).</summary>
    [HttpGet("customer-debts")]
    [RequirePermission("REPORT.VIEW")]
    public async Task<IActionResult> CustomerDebts([FromQuery] string? search, CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<CustomerDebtRow>>.Ok(
            await reports.CustomerDebtsAsync(new ReportQuery(KhoId: search), ct), HttpContext.TraceIdentifier));

    /// <summary>Công nợ nhà cung cấp tổng hợp (mục 5.2/6.1).</summary>
    [HttpGet("supplier-debts")]
    [RequirePermission("REPORT.VIEW")]
    public async Task<IActionResult> SupplierDebts([FromQuery] string? search, CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<SupplierDebtRow>>.Ok(
            await reports.SupplierDebtsAsync(new ReportQuery(KhoId: search), ct), HttpContext.TraceIdentifier));

    /// <summary>Tồn quỹ theo ngày (luỹ kế THU − CHI).</summary>
    [HttpGet("cash-balance")]
    [RequirePermission("REPORT.VIEW")]
    public async Task<IActionResult> CashBalance([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<CashBalanceRow>>.Ok(
            await reports.CashBalanceAsync(new ReportQuery(from, to), ct), HttpContext.TraceIdentifier));

    /// <summary>Kết quả kinh doanh trong kỳ (doanh thu − giá vốn − chi phí).</summary>
    [HttpGet("kqkd")]
    [RequirePermission("REPORT.VIEW")]
    public async Task<IActionResult> Kqkd([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(ApiResult<KqkdRow>.Ok(
            await reports.KqkdAsync(new ReportQuery(from, to), ct), HttpContext.TraceIdentifier));

    // ---------- Excel export ----------

    /// <summary>Xuất Excel báo cáo bán hàng theo ngày (REPORT.EXPORT).</summary>
    [HttpGet("sales-by-day/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportSalesByDay([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => File(await reports.ExportSalesByDayAsync(new ReportQuery(from, to), ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ban-hang-theo-ngay.xlsx");

    [HttpGet("sales-by-item/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportSalesByItem(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int top = 100, CancellationToken ct = default)
        => File(await reports.ExportSalesByItemAsync(new ReportQuery(from, to, Top: top), ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "mat-hang-ban-chay.xlsx");

    [HttpGet("cash-by-day/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportCashByDay([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => File(await reports.ExportCashByDayAsync(new ReportQuery(from, to), ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "thu-chi-theo-ngay.xlsx");

    [HttpGet("cash-by-reason/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportCashByReason([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => File(await reports.ExportCashByReasonAsync(new ReportQuery(from, to), ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "thu-chi-theo-ly-do.xlsx");

    [HttpGet("xnt/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportXnt(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? khoId, CancellationToken ct)
        => File(await reports.ExportXntAsync(new ReportQuery(from, to, khoId), ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xuat-nhap-ton.xlsx");

    [HttpGet("stock/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportStock(
        [FromQuery] string? search, [FromQuery] string? khoId, [FromQuery] bool? nonZeroOnly, CancellationToken ct)
        => File(await reports.ExportStockAsync(new InventoryListQuery(search, khoId, nonZeroOnly), ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ton-kho.xlsx");

    [HttpGet("customer-debts/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportCustomerDebts([FromQuery] string? search, CancellationToken ct)
        => File(await reports.ExportCustomerDebtsAsync(new ReportQuery(KhoId: search), ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "cong-no-khach-hang.xlsx");

    [HttpGet("supplier-debts/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportSupplierDebts([FromQuery] string? search, CancellationToken ct)
        => File(await reports.ExportSupplierDebtsAsync(new ReportQuery(KhoId: search), ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "cong-no-nha-cung-cap.xlsx");

    [HttpGet("cash-balance/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportCashBalance([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => File(await reports.ExportCashBalanceAsync(new ReportQuery(from, to), ct),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ton-quy-theo-ngay.xlsx");

    // ---------- Danh mục export (mục 6.4) ----------

    [HttpGet("products/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportProducts([FromQuery] string? search, CancellationToken ct)
        => File(await reports.ExportProductsAsync(new ProductListQuery(search)), 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "danh-muc-mat-hang.xlsx");

    [HttpGet("customers/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportCustomers([FromQuery] string? search, CancellationToken ct)
        => File(await reports.ExportCustomersAsync(new CustomerListQuery(search)),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "danh-muc-khach-hang.xlsx");

    [HttpGet("suppliers/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportSuppliers([FromQuery] string? search, CancellationToken ct)
        => File(await reports.ExportSuppliersAsync(new SupplierListQuery(search)),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "danh-muc-nha-cung-cap.xlsx");

    [HttpGet("warehouses/export")]
    [RequirePermission("REPORT.EXPORT")]
    public async Task<IActionResult> ExportWarehouses([FromQuery] string? search, CancellationToken ct)
        => File(await reports.ExportWarehousesAsync(new WarehouseListQuery(search)),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "danh-muc-kho-hang.xlsx");

    // ---------- Print (PDF) ----------

    /// <summary>In hoá đơn bán lẻ/sỉ A5 (mục 4.1/4.2) — PDF stream.</summary>
    [HttpGet("invoices/{donHangId}/pdf")]
    [RequirePermission("SALES.INVOICE.PRINT")]
    public async Task<IActionResult> InvoicePdf(string donHangId, CancellationToken ct)
    {
        var inv = await reports.GetInvoiceAsync(donHangId, ct);
        return File(reports.InvoicePdf(inv), "application/pdf", $"hoa-don-{FileNameHelper.Sanitize(inv.SoPhieu)}.pdf");
    }

    /// <summary>In phiếu thu/chi A5 (mục 5.1) — PDF stream.</summary>
    [HttpGet("cash-vouchers/{id}/pdf")]
    [RequirePermission("CASHBOOK.RECEIPT.VIEW")]
    public async Task<IActionResult> CashVoucherPdf(string id, CancellationToken ct)
    {
        var v = await reports.GetCashVoucherPrintAsync(id, ct);
        return File(reports.CashVoucherPdf(v), "application/pdf", $"phieu-{FileNameHelper.Sanitize(v.SoPhieu)}.pdf");
    }

    /// <summary>In phiếu kho (nhập/xuất/chuyển/kiểm kê/trả NCC) A5 — PDF stream (mục 6.3).</summary>
    [HttpGet("stock-vouchers/{donHangId}/pdf")]
    [RequirePermission("INVENTORY.STOCK.VIEW")]
    public async Task<IActionResult> StockVoucherPdf(string donHangId, CancellationToken ct)
    {
        var v = await reports.GetStockVoucherAsync(donHangId, ct);
        return File(reports.StockVoucherPdf(v), "application/pdf", $"phieu-kho-{FileNameHelper.Sanitize(v.SoPhieu)}.pdf");
    }
}

/// <summary>Sinh mã vạch CODE_128 PNG cho 1 mặt hàng (in tem — mục 6.3/6.4).</summary>
[ApiController]
[Route("api/reports/barcode")]
[Authorize]
public sealed class ProductBarcodeController(ReportService reports) : ControllerBase
{
    [HttpGet("{productId}")]
    [RequirePermission("SALES.PRODUCT.VIEW")]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public IActionResult Get(string productId)
        => File(reports.ProductBarcodePng(productId), "image/png");
}

internal static class FileNameHelper
{
    public static string Sanitize(string? name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", (name ?? "file").Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }
}
