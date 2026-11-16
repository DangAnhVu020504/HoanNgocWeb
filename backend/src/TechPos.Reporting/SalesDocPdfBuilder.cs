using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechPos.Shared;

namespace TechPos.Reporting;

/// <summary>
/// Sinh PDF A4 cho chứng từ bán hàng dùng bảng riêng (báo giá / đặt hàng KH / đặt mua NCC /
/// hợp đồng) và phiếu gửi hàng (gửi COD / kết quả COD / giao xe).
/// Input DTO từ mirror (ReportRepository) — không truy vấn DB.
/// </summary>
public static class SalesDocPdfBuilder
{
    static SalesDocPdfBuilder() => QuestPDF.Settings.License = LicenseType.Community;

    private static string Title(string docType) => docType switch
    {
        "BAO_GIA" => "BÁO GIÁ",
        "DAT_HANG" => "ĐƠN ĐẶT HÀNG",
        "DAT_MUA_NCC" => "ĐƠN ĐẶT MUA HÀNG",
        "HOP_DONG" => "HỢP ĐỒNG BÁN HÀNG",
        "GUI_COD" => "PHIẾU GỬI HÀNG COD",
        "KET_QUA_COD" => "PHIẾU KẾT QUẢ GỬI COD",
        "GIAO_XE" => "PHIẾU GIAO HÀNG THEO XE",
        _ => "CHỨNG TỪ"
    };

    // ===================== Chứng từ bán hàng (BG / DH / DN / HD) =====================

    public static byte[] Build(SalesDocPrintDto doc, string companyName = "Cửa hàng Hoàn Ngọc")
    {
        var doiTuongLabel = doc.DocType == "DAT_MUA_NCC" ? "Nhà cung cấp" : "Khách hàng";
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(companyName).Bold().FontSize(15);
                    col.Item().AlignCenter().Text(Title(doc.DocType)).FontSize(13);
                    col.Item().PaddingTop(8, Unit.Point).Row(row =>
                    {
                        row.RelativeItem().Text($"Số: {doc.SoPhieu}").SemiBold();
                        if (doc.Ngay is not null)
                            row.RelativeItem().AlignRight()
                                .Text($"Ngày {doc.Ngay:dd/MM/yyyy}");
                    });
                    if (doc.DocType == "HOP_DONG")
                    {
                        if (doc.TuNgay is not null || doc.DenNgay is not null)
                            col.Item().Text($"Hiệu lực: {doc.TuNgay:dd/MM/yyyy} → {doc.DenNgay:dd/MM/yyyy}");
                    }
                });

                page.Content().PaddingVertical(10, Unit.Point).Column(col =>
                {
                    col.Spacing(4);

                    if (!string.IsNullOrWhiteSpace(doc.TenKhach))
                    {
                        col.Item().Text(t => { t.Span($"{doiTuongLabel}: ").SemiBold(); t.Span(doc.TenKhach); });
                        if (!string.IsNullOrWhiteSpace(doc.DiaChi))
                            col.Item().Text($"Địa chỉ: {doc.DiaChi}").FontSize(9);
                        if (!string.IsNullOrWhiteSpace(doc.DienThoai))
                            col.Item().Text($"Điện thoại: {doc.DienThoai}").FontSize(9);
                    }
                    if (!string.IsNullOrWhiteSpace(doc.NhanVien))
                        col.Item().Text($"Nhân viên: {doc.NhanVien}").FontSize(9);

                    col.Item().PaddingTop(6, Unit.Point).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(24, Unit.Point);
                            c.RelativeColumn(3);
                            c.ConstantColumn(42, Unit.Point);
                            c.ConstantColumn(50, Unit.Point);
                            c.ConstantColumn(78, Unit.Point);
                            c.ConstantColumn(48, Unit.Point);
                            c.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeadCell).Text("STT");
                            header.Cell().Element(HeadCell).Text("Mặt hàng");
                            header.Cell().Element(HeadCell).Text("ĐVT");
                            header.Cell().Element(HeadCell).AlignRight().Text("SL");
                            header.Cell().Element(HeadCell).AlignRight().Text("Đơn giá");
                            header.Cell().Element(HeadCell).AlignRight().Text("Giảm %");
                            header.Cell().Element(HeadCell).AlignRight().Text("Thành tiền");
                        });

                        var stt = 1;
                        foreach (var l in doc.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(stt.ToString());
                            table.Cell().Element(BodyCell).Text(l.MatHang ?? "");
                            table.Cell().Element(BodyCell).Text(l.DonViTinh ?? "");
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatQty(l.SoLuong));
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.DonGia));
                            table.Cell().Element(BodyCell).AlignRight()
                                .Text(l.TileGiamGia != 0 ? FormatQty(l.TileGiamGia) : "");
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.ThanhTien));
                            stt++;
                        }
                    });

                    col.Item().PaddingTop(6, Unit.Point).AlignRight().Column(tot =>
                    {
                        tot.Spacing(2);
                        tot.Item().Text($"Tiền hàng: {FormatMoney(doc.TienHang)}");
                        if (doc.TienGiamGia != 0) tot.Item().Text($"Giảm giá: {FormatMoney(doc.TienGiamGia)}");
                        if (doc.TienThue != 0) tot.Item().Text($"Tiền thuế: {FormatMoney(doc.TienThue)}");
                        if (doc.PhiVanChuyen != 0) tot.Item().Text($"Phí vận chuyển: {FormatMoney(doc.PhiVanChuyen)}");
                        tot.Item().Text($"TỔNG CỘNG: {FormatMoney(doc.TongCong)} đ").Bold().FontSize(12);
                        if (doc.DocType == "HOP_DONG" && doc.DatCoc != 0)
                        {
                            tot.Item().Text($"Đặt cọc: {FormatMoney(doc.DatCoc)}");
                            tot.Item().Text($"Còn lại: {FormatMoney(doc.TongCong - doc.DatCoc)}").Bold();
                        }
                    });

                    col.Item().PaddingTop(4, Unit.Point)
                        .Text(VietnameseNumber.ToWords(doc.TongCong)).Italic().FontSize(9);

                    if (!string.IsNullOrWhiteSpace(doc.GhiChu))
                        col.Item().Text($"Ghi chú: {doc.GhiChu}").FontSize(9);

                    // Chữ ký
                    col.Item().PaddingTop(28, Unit.Point).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("Người lập").FontSize(9);
                        row.RelativeItem().AlignCenter().Text(doiTuongLabel).FontSize(9);
                    });
                });

                page.Footer().AlignCenter()
                    .Text($"— In lúc {DateTime.Now:dd/MM/yyyy HH:mm} —").FontSize(8);
            });
        }).GeneratePdf();
    }

    // ===================== Phiếu gửi hàng (COD / KQ / XE) =====================

    public static byte[] BuildShipment(ShipmentDocPrintDto doc, string companyName = "Cửa hàng Hoàn Ngọc")
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(companyName).Bold().FontSize(15);
                    col.Item().AlignCenter().Text(Title(doc.DocType)).FontSize(13);
                    col.Item().PaddingTop(8, Unit.Point).Row(row =>
                    {
                        row.RelativeItem().Text($"Số: {doc.SoPhieu}").SemiBold();
                        if (doc.Ngay is not null)
                            row.RelativeItem().AlignRight().Text($"Ngày {doc.Ngay:dd/MM/yyyy}");
                    });
                });

                page.Content().PaddingVertical(10, Unit.Point).Column(col =>
                {
                    col.Spacing(4);
                    if (!string.IsNullOrWhiteSpace(doc.DonViChuyenPhat))
                        col.Item().Text(t => { t.Span($"Đơn vị chuyển phát: {doc.DonViChuyenPhat}").SemiBold(); });
                    if (!string.IsNullOrWhiteSpace(doc.XeGiaoHang))
                        col.Item().Text(t => { t.Span($"Xe giao hàng: {doc.XeGiaoHang}").SemiBold(); });
                    if (!string.IsNullOrWhiteSpace(doc.NhanVien))
                        col.Item().Text($"Nhân viên: {doc.NhanVien}");
                    if (!string.IsNullOrWhiteSpace(doc.GhiChu))
                        col.Item().Text($"Ghi chú: {doc.GhiChu}").FontSize(9);

                    if (doc.Lines.Count > 0)
                    {
                        col.Item().PaddingTop(6, Unit.Point).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(24, Unit.Point);
                                c.ConstantColumn(100, Unit.Point);
                                c.RelativeColumn(3);
                                c.ConstantColumn(80, Unit.Point);
                                c.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeadCell).Text("STT");
                                header.Cell().Element(HeadCell).Text("Số đơn");
                                header.Cell().Element(HeadCell).Text("Khách hàng");
                                header.Cell().Element(HeadCell).AlignRight().Text("Ngày");
                                header.Cell().Element(HeadCell).AlignRight().Text("Tổng tiền");
                            });

                            var stt = 1;
                            foreach (var l in doc.Lines)
                            {
                                table.Cell().Element(BodyCell).Text(stt.ToString());
                                table.Cell().Element(BodyCell).Text(l.SoDon);
                                table.Cell().Element(BodyCell).Text(l.KhachHang ?? "");
                                table.Cell().Element(BodyCell).AlignRight()
                                    .Text(l.Ngay?.ToString("dd/MM/yyyy") ?? "");
                                table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.TongCong));
                                stt++;
                            }
                        });

                        col.Item().PaddingTop(6, Unit.Point).AlignRight()
                            .Text($"TỔNG TIỀN: {FormatMoney(doc.TongTien)} đ").Bold().FontSize(12);
                        col.Item().AlignRight()
                            .Text($"Số đơn: {doc.Lines.Count}").FontSize(9);
                    }
                    else
                    {
                        col.Item().PaddingTop(6, Unit.Point)
                            .Text("(Phiếu không kèm danh sách đơn hàng)").FontSize(9).Italic();
                    }

                    col.Item().PaddingTop(28, Unit.Point).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("Người lập").FontSize(9);
                        row.RelativeItem().AlignCenter().Text("Người nhận").FontSize(9);
                    });
                });

                page.Footer().AlignCenter()
                    .Text($"— In lúc {DateTime.Now:dd/MM/yyyy HH:mm} —").FontSize(8);
            });
        }).GeneratePdf();
    }

    private static IContainer HeadCell(IContainer c) =>
        c.Background(Color.FromHex("#f0f0f0"))
            .BorderBottom(0.5f, Unit.Point)
            .Padding(3, Unit.Point);

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(0.5f, Unit.Point)
            .PaddingVertical(2, Unit.Point)
            .PaddingHorizontal(3, Unit.Point);

    private static string FormatQty(decimal q) => q % 1 == 0 ? ((long)q).ToString() : q.ToString("0.##");
    private static string FormatMoney(decimal v) => v.ToString("N0");
}
