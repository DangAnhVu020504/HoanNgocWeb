using TechPos.Data.MySql;
using TechPos.Reporting;
using TechPos.Shared;

namespace TechPos.Application.Reports;

/// <summary>
/// Use-case Report Center (Phase 6 — ROADMAP): chạy báo cáo trên mirror MySQL
/// rồi render Excel (ClosedXML) / PDF (QuestPDF) / mã vạch (ZXing).
/// KHÔNG chứa SQL — truy vấn nằm ở ReportRepository/ProductService.
/// </summary>
public sealed class ReportService(
    ReportRepository reports,
    ProductRepository products,
    CustomerRepository customers,
    SupplierRepository suppliers,
    WarehouseRepository warehouses,
    InventoryRepository inventory)
{
    public async Task<IReadOnlyList<SalesByDayRow>> SalesByDayAsync(ReportQuery q, CancellationToken ct = default)
        => await reports.SalesByDayAsync(q, ct);

    public async Task<IReadOnlyList<SalesByItemRow>> SalesByItemAsync(ReportQuery q, CancellationToken ct = default)
        => await reports.SalesByItemAsync(q, ct);

    public async Task<IReadOnlyList<CashByDayRow>> CashByDayAsync(ReportQuery q, CancellationToken ct = default)
        => await reports.CashByDayAsync(q, ct);

    public async Task<IReadOnlyList<CashByReasonRow>> CashByReasonAsync(ReportQuery q, CancellationToken ct = default)
        => await reports.CashByReasonAsync(q, ct);

    public async Task<IReadOnlyList<XntRow>> XntAsync(ReportQuery q, CancellationToken ct = default)
        => await reports.XntAsync(q, ct);

    public async Task<IReadOnlyList<CustomerDebtRow>> CustomerDebtsAsync(ReportQuery q, CancellationToken ct = default)
        => await reports.CustomerDebtsAsync(q, ct);

    public async Task<IReadOnlyList<SupplierDebtRow>> SupplierDebtsAsync(ReportQuery q, CancellationToken ct = default)
        => await reports.SupplierDebtsAsync(q, ct);

    public async Task<IReadOnlyList<CashBalanceRow>> CashBalanceAsync(ReportQuery q, CancellationToken ct = default)
        => await reports.CashBalanceAsync(q, ct);

    public async Task<KqkdRow> KqkdAsync(ReportQuery q, CancellationToken ct = default)
        => await reports.KqkdAsync(q, ct);

    public async Task<PageResult<InventoryDto>> StockAsync(InventoryListQuery q, CancellationToken ct = default)
    {
        var (items, total) = await inventory.SearchAsync(q, ct);
        return new PageResult<InventoryDto>
        {
            Items = items,
            Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize),
            Total = total
        };
    }

    public async Task<InvoiceDto> GetInvoiceAsync(string donHangId, CancellationToken ct = default)
        => await reports.GetInvoiceAsync(donHangId, ct)
           ?? throw new NotFoundException($"Không tìm thấy hoá đơn {donHangId}");

    public async Task<CashVoucherPrintDto> GetCashVoucherPrintAsync(string id, CancellationToken ct = default)
        => await reports.GetCashVoucherPrintAsync(id, ct)
           ?? throw new NotFoundException($"Không tìm thấy phiếu thu/chi {id}");

    public async Task<StockVoucherPrintDto> GetStockVoucherAsync(string donHangId, CancellationToken ct = default)
        => await reports.GetStockVoucherAsync(donHangId, ct)
           ?? throw new NotFoundException($"Không tìm thấy phiếu kho {donHangId}");

    // ---------- Export helpers (một nguồn cho xlsx/pdf) ----------

    public async Task<byte[]> ExportSalesByDayAsync(ReportQuery q, CancellationToken ct = default)
        => ExcelExporter.Build("Bán hàng theo ngày",
        [
            new("Ngày", r => ((SalesByDayRow)r).Ngay),
            new("Số hoá đơn", r => ((SalesByDayRow)r).SoHoaDon),
            new("Tiền hàng", r => ((SalesByDayRow)r).TienHang),
            new("Giảm giá", r => ((SalesByDayRow)r).TienGiamGia),
            new("Tổng cộng", r => ((SalesByDayRow)r).TongCong),
            new("Còn nợ", r => ((SalesByDayRow)r).ConNo),
        ], (await reports.SalesByDayAsync(q, ct)).Cast<object>().ToList());

    public async Task<byte[]> ExportSalesByItemAsync(ReportQuery q, CancellationToken ct = default)
        => ExcelExporter.Build("Mặt hàng bán chạy",
        [
            new("Mã", r => ((SalesByItemRow)r).Code ?? ""),
            new("Tên mặt hàng", r => ((SalesByItemRow)r).Name),
            new("ĐVT", r => ((SalesByItemRow)r).DonViTinh ?? ""),
            new("Số lượng", r => ((SalesByItemRow)r).SoLuong),
            new("Thành tiền", r => ((SalesByItemRow)r).ThanhTien),
        ], (await reports.SalesByItemAsync(q, ct)).Cast<object>().ToList());

    public async Task<byte[]> ExportCashByDayAsync(ReportQuery q, CancellationToken ct = default)
        => ExcelExporter.Build("Thu chi theo ngày",
        [
            new("Ngày", r => ((CashByDayRow)r).Ngay),
            new("Thu", r => ((CashByDayRow)r).Thu),
            new("Chi", r => ((CashByDayRow)r).Chi),
        ], (await reports.CashByDayAsync(q, ct)).Cast<object>().ToList());

    public async Task<byte[]> ExportCashByReasonAsync(ReportQuery q, CancellationToken ct = default)
        => ExcelExporter.Build("Thu chi theo lý do",
        [
            new("Lý do", r => ((CashByReasonRow)r).LyDo),
            new("Thu", r => ((CashByReasonRow)r).Thu),
            new("Chi", r => ((CashByReasonRow)r).Chi),
        ], (await reports.CashByReasonAsync(q, ct)).Cast<object>().ToList());

    public async Task<byte[]> ExportXntAsync(ReportQuery q, CancellationToken ct = default)
        => ExcelExporter.Build("Xuat-Nhap-Ton",
        [
            new("Mã", r => ((XntRow)r).Code ?? ""),
            new("Tên mặt hàng", r => ((XntRow)r).Name),
            new("ĐVT", r => ((XntRow)r).DonViTinh ?? ""),
            new("Tồn đầu kỳ", r => ((XntRow)r).TonDauKy),
            new("Nhập", r => ((XntRow)r).SlNhap),
            new("Xuất", r => ((XntRow)r).SlXuat),
            new("Tồn cuối", r => ((XntRow)r).TonCuoi),
            new("Giá vốn", r => ((XntRow)r).GiaVon),
            new("Giá trị tồn", r => ((XntRow)r).GiaTriTon),
        ], (await reports.XntAsync(q, ct)).Cast<object>().ToList());

    public async Task<byte[]> ExportStockAsync(InventoryListQuery q, CancellationToken ct = default)
    {
        var (items, _) = await inventory.SearchAsync(q with { Page = 1, PageSize = Paging.MaxPageSize }, ct);
        return ExcelExporter.Build("Ton kho",
        [
            new("Mã", r => ((InventoryDto)r).Code ?? ""),
            new("Tên mặt hàng", r => ((InventoryDto)r).Name),
            new("ĐVT", r => ((InventoryDto)r).DonViTinh ?? ""),
            new("Kho", r => ((InventoryDto)r).Kho ?? ""),
            new("Tồn", r => ((InventoryDto)r).Ton),
            new("Giá vốn", r => ((InventoryDto)r).GiaVon),
            new("Giá bán", r => ((InventoryDto)r).GiaBan),
            new("Giá trị tồn", r => ((InventoryDto)r).GiaTriTon),
        ], items.Cast<object>().ToList());
    }

    // ---------- Export báo cáo bổ sung (mục 6.1: công nợ / tồn quỹ / KQKD) ----------

    public async Task<byte[]> ExportCustomerDebtsAsync(ReportQuery q, CancellationToken ct = default)
        => ExcelExporter.Build("Cong no khach hang",
        [
            new("Mã khách", r => ((CustomerDebtRow)r).MaKhach ?? ""),
            new("Tên khách hàng", r => ((CustomerDebtRow)r).TenKhach),
            new("Điện thoại", r => ((CustomerDebtRow)r).DienThoai ?? ""),
            new("Số hoá đơn còn nợ", r => ((CustomerDebtRow)r).SoHoaDon),
            new("Số ngày cho nợ", r => ((CustomerDebtRow)r).SongayChoNo),
            new("Công nợ", r => ((CustomerDebtRow)r).CongNo),
        ], (await reports.CustomerDebtsAsync(q, ct)).Cast<object>().ToList());

    public async Task<byte[]> ExportSupplierDebtsAsync(ReportQuery q, CancellationToken ct = default)
        => ExcelExporter.Build("Cong no nha cung cap",
        [
            new("Mã NCC", r => ((SupplierDebtRow)r).MaNhaCungCap ?? ""),
            new("Tên nhà cung cấp", r => ((SupplierDebtRow)r).TenNhaCungCap),
            new("Điện thoại", r => ((SupplierDebtRow)r).DienThoai ?? ""),
            new("Số phiếu còn nợ", r => ((SupplierDebtRow)r).SoPhieu),
            new("Công nợ", r => ((SupplierDebtRow)r).CongNo),
        ], (await reports.SupplierDebtsAsync(q, ct)).Cast<object>().ToList());

    public async Task<byte[]> ExportCashBalanceAsync(ReportQuery q, CancellationToken ct = default)
        => ExcelExporter.Build("Ton quy theo ngay",
        [
            new("Ngày", r => ((CashBalanceRow)r).Ngay),
            new("Thu", r => ((CashBalanceRow)r).Thu),
            new("Chi", r => ((CashBalanceRow)r).Chi),
            new("Tồn cuối ngày", r => ((CashBalanceRow)r).TonCuoiNgay),
        ], (await reports.CashBalanceAsync(q, ct)).Cast<object>().ToList());

    // ---------- Danh mục export (mục 6.4: xuất Excel mọi lưới chính) ----------

    public async Task<byte[]> ExportProductsAsync(ProductListQuery q, CancellationToken ct = default)
    {
        var (items, _) = await products.SearchAsync(q with { Page = 1, PageSize = Paging.MaxPageSize }, ct);
        return ExcelExporter.Build("Mat hang",
        [
            new("Mã", r => ((ProductDto)r).Code ?? ""),
            new("Tên mặt hàng", r => ((ProductDto)r).Name),
            new("Nhóm", r => ((ProductDto)r).NhomMatHang ?? ""),
            new("ĐVT", r => ((ProductDto)r).DonViTinh ?? ""),
            new("Hãng SX", r => ((ProductDto)r).HangSanXuat ?? ""),
            new("Giá bán", r => ((ProductDto)r).GiaBan),
            new("Giá nhập", r => ((ProductDto)r).GiaNhap),
            new("Giá vốn", r => ((ProductDto)r).GiaVon),
            new("Tồn tối thiểu", r => ((ProductDto)r).TonToiThieu),
            new("Tồn tối đa", r => ((ProductDto)r).TonToiDa),
            new("Trạng thái", r => ((ProductDto)r).Status),
        ], items.Cast<object>().ToList());
    }

    public async Task<byte[]> ExportCustomersAsync(CustomerListQuery q, CancellationToken ct = default)
    {
        var (items, _) = await customers.SearchAsync(q with { Page = 1, PageSize = Paging.MaxPageSize }, ct);
        return ExcelExporter.Build("Khach hang",
        [
            new("Mã khách", r => ((CustomerDto)r).MaKhach ?? ""),
            new("Tên khách hàng", r => ((CustomerDto)r).Name),
            new("Điện thoại", r => ((CustomerDto)r).DienThoai ?? ""),
            new("Địa chỉ", r => ((CustomerDto)r).DiaChi ?? ""),
            new("Email", r => ((CustomerDto)r).Email ?? ""),
            new("Nhóm KH", r => ((CustomerDto)r).NhomKhachHang ?? ""),
            new("Số ngày cho nợ", r => ((CustomerDto)r).SongayChoNo),
            new("Điểm tích luỹ", r => ((CustomerDto)r).DiemTichLuy),
            new("Trạng thái", r => ((CustomerDto)r).Status),
        ], items.Cast<object>().ToList());
    }

    public async Task<byte[]> ExportSuppliersAsync(SupplierListQuery q, CancellationToken ct = default)
    {
        var (items, _) = await suppliers.SearchAsync(q with { Page = 1, PageSize = Paging.MaxPageSize }, ct);
        return ExcelExporter.Build("Nha cung cap",
        [
            new("Mã NCC", r => ((SupplierDto)r).MaNhaCungCap ?? ""),
            new("Tên nhà cung cấp", r => ((SupplierDto)r).Name),
            new("Điện thoại", r => ((SupplierDto)r).DienThoai ?? ""),
            new("Địa chỉ", r => ((SupplierDto)r).DiaChi ?? ""),
            new("Email", r => ((SupplierDto)r).Email ?? ""),
            new("Nhóm NCC", r => ((SupplierDto)r).NhomNhaCungCap ?? ""),
            new("Trạng thái", r => ((SupplierDto)r).Status),
        ], items.Cast<object>().ToList());
    }

    public async Task<byte[]> ExportWarehousesAsync(WarehouseListQuery q, CancellationToken ct = default)
    {
        var (items, _) = await warehouses.SearchAsync(q with { Page = 1, PageSize = Paging.MaxPageSize }, ct);
        return ExcelExporter.Build("Kho hang",
        [
            new("Tên kho", r => ((WarehouseDto)r).Name),
            new("Cửa hàng", r => ((WarehouseDto)r).CuaHang ?? ""),
            new("Kho cha", r => ((WarehouseDto)r).ParentName ?? ""),
            new("Cho phép âm kho", r => ((WarehouseDto)r).ChoPhepAmKho),
            new("Ghi chú", r => ((WarehouseDto)r).Note ?? ""),
            new("Trạng thái", r => ((WarehouseDto)r).Status),
        ], items.Cast<object>().ToList());
    }

    // ---------- Print (PDF) ----------

    public byte[] InvoicePdf(InvoiceDto inv, string? companyName = null)
        => InvoicePdfBuilder.Build(inv, companyName ?? "Cửa hàng Hoàn Ngọc");

    public byte[] CashVoucherPdf(CashVoucherPrintDto v, string? companyName = null)
        => CashVoucherPdfBuilder.Build(v, companyName ?? "Cửa hàng Hoàn Ngọc");

    public byte[] StockVoucherPdf(StockVoucherPrintDto v, string? companyName = null)
        => StockVoucherPdfBuilder.Build(v, companyName ?? "Cửa hàng Hoàn Ngọc");

    public byte[] PayrollPdf(PayrollDto p, string? companyName = null)
        => PayrollPdfBuilder.Build(p, companyName ?? "Cửa hàng Hoàn Ngọc");

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public byte[] ProductBarcodePng(string productId)
    {
        var p = products.GetByIdAsync(productId).GetAwaiter().GetResult()
                ?? throw new NotFoundException($"Không tìm thấy mặt hàng {productId}");
        var content = !string.IsNullOrWhiteSpace(p.Code) ? p.Code! : productId;
        return BarcodeGenerator.CreatePng(content);
    }
}
