using ClosedXML.Excel;
using TechPos.Application.Catalog;
using TechPos.Application.Products;
using TechPos.Shared;

namespace TechPos.Application.Reports;

/// <summary>1 dòng lỗi import (để UI hiển thị).</summary>
public sealed record ImportRowError(int Row, string Message);

/// <summary>Kết quả import Excel danh mục.</summary>
public sealed record ImportResult(
    int TotalRows,
    int Created,
    int Skipped,
    IReadOnlyList<ImportRowError> Errors);

/// <summary>
/// Nhập Excel danh mục (Phase 6.4 — KE-HOACH §6.4: "nhập Excel (danh mục, mặt hàng, KH)").
/// Đọc .xlsx bằng ClosedXML (cột như file xuất Excel — ExcelExporter), ghi qua
/// ProductService/CustomerService CreateAsync (đủ validator + ConflictException trùng mã).
/// 1 dòng lỗi KHÔNG dừng cả file (dead-letter từng dòng).
/// </summary>
public sealed class CatalogImportService(ProductService products, CustomerService customers)
{
    public async Task<ImportResult> ImportProductsAsync(byte[] fileBytes, string userId, CancellationToken ct = default)
    {
        using var ms = new MemoryStream(fileBytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        var created = 0; var skipped = 0; var errors = new List<ImportRowError>();
        var total = 0;

        for (var r = 2; r <= lastRow; r++)   // dòng 1 = header
        {
            var name = ws.Cell(r, 2).GetString().Trim();     // cột 2 = "Tên mặt hàng"
            if (string.IsNullOrEmpty(name)) continue;         // dòng trống — bỏ qua (không đếm lỗi)
            total++;

            try
            {
                var code = ws.Cell(r, 1).GetString().Trim();  // cột 1 = "Mã"
                var nhom = ws.Cell(r, 3).GetString().Trim();  // "Nhóm"
                var giaBan = ParseDecimal(ws.Cell(r, 6));
                var giaNhap = ParseDecimal(ws.Cell(r, 7));
                var tonToiThieu = ParseDecimal(ws.Cell(r, 9));

                await products.CreateAsync(new ProductCreateRequest(
                    Name: name,
                    Code: string.IsNullOrEmpty(code) ? null : code,
                    MaSanCo: null, Note: null,
                    NhomMatHangId: null, DonViTinhId: null, HangSanXuatId: null,
                    XuatXu: null, MauSac: null, ChatLieu: null,
                    GiaBan: giaBan, GiaNhap: giaNhap, TonToiThieu: tonToiThieu), userId, ct);
                created++;
            }
            catch (ConflictException) { skipped++; }   // trùng mã — bỏ qua (import idempotent)
            catch (ValidationException ex) { errors.Add(new ImportRowError(r, ex.Message)); }
            catch (Exception ex) { errors.Add(new ImportRowError(r, ex.Message)); }
        }

        return new ImportResult(total, created, skipped, errors);
    }

    public async Task<ImportResult> ImportCustomersAsync(byte[] fileBytes, string userId, CancellationToken ct = default)
    {
        using var ms = new MemoryStream(fileBytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheet(1);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        var created = 0; var skipped = 0; var errors = new List<ImportRowError>();
        var total = 0;

        for (var r = 2; r <= lastRow; r++)
        {
            var name = ws.Cell(r, 2).GetString().Trim();     // cột 2 = "Tên khách hàng"
            if (string.IsNullOrEmpty(name)) continue;
            total++;

            try
            {
                var maKhach = ws.Cell(r, 1).GetString().Trim();    // "Mã khách"
                var dienThoai = ws.Cell(r, 3).GetString().Trim();  // "Điện thoại"
                var diaChi = ws.Cell(r, 4).GetString().Trim();     // "Địa chỉ"
                var email = ws.Cell(r, 5).GetString().Trim();      // "Email"
                var songayChoNo = (int)ParseDecimal(ws.Cell(r, 7));

                await customers.CreateAsync(new CustomerCreateRequest(
                    Name: name,
                    MaKhach: string.IsNullOrEmpty(maKhach) ? null : maKhach,
                    DiaChi: string.IsNullOrEmpty(diaChi) ? null : diaChi,
                    DienThoai: string.IsNullOrEmpty(dienThoai) ? null : dienThoai,
                    Email: string.IsNullOrEmpty(email) ? null : email,
                    MaSoThue: null, NguoiDaiDien: null, ChucVu: null, Facebook: null,
                    DiaChiGiaoHang: null, NhomKhachHangId: null,
                    SongayChoNo: songayChoNo), userId, ct);
                created++;
            }
            catch (ConflictException) { skipped++; }
            catch (ValidationException ex) { errors.Add(new ImportRowError(r, ex.Message)); }
            catch (Exception ex) { errors.Add(new ImportRowError(r, ex.Message)); }
        }

        return new ImportResult(total, created, skipped, errors);
    }

    private static decimal ParseDecimal(IXLCell cell)
    {
        var raw = cell.GetString().Trim();
        if (string.IsNullOrEmpty(raw)) return 0;
        if (decimal.TryParse(raw.Replace(".", "").Replace(",", ""), out var v)) return v;
        return decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out v) ? v : 0;
    }
}
