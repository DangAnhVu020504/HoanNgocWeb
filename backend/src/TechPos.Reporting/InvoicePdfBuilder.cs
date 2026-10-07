using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechPos.Shared;

namespace TechPos.Reporting;

/// <summary>
/// Sinh PDF hoá đơn A5 (QuestPDF 2025.7) — Phase 6 (KE-HOACH §6.3, ROADMAP Phase 6).
/// Input: InvoiceDto từ mirror (ReportRepository.GetInvoiceAsync). Không truy vấn DB.
/// </summary>
public static class InvoicePdfBuilder
{
    static InvoicePdfBuilder() => QuestPDF.Settings.License = LicenseType.Community;

    public static byte[] Build(InvoiceDto inv, string companyName = "Cửa hàng Hoàn Ngọc")
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(companyName).Bold().FontSize(14);
                    col.Item().AlignCenter().Text("HÓA ĐƠN BÁN HÀNG").FontSize(12);
                    col.Item().PaddingTop(6, Unit.Point).Row(row =>
                    {
                        row.RelativeItem().Text($"Số phiếu: {inv.SoPhieu}").SemiBold();
                        row.RelativeItem().AlignRight().Text(inv.Ngay?.ToString("dd/MM/yyyy HH:mm") ?? "");
                    });
                    if (!string.IsNullOrWhiteSpace(inv.KhachHang))
                        col.Item().Text($"Khách hàng: {inv.KhachHang}");
                    if (!string.IsNullOrWhiteSpace(inv.NhanVien))
                        col.Item().Text($"Nhân viên: {inv.NhanVien}");
                });

                page.Content().PaddingVertical(8, Unit.Point).Column(col =>
                {
                    col.Spacing(6);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22, Unit.Point);
                            c.RelativeColumn(3);
                            c.ConstantColumn(45, Unit.Point);
                            c.ConstantColumn(45, Unit.Point);
                            c.ConstantColumn(75, Unit.Point);
                            c.ConstantColumn(70, Unit.Point);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeadCell).Text("STT");
                            header.Cell().Element(HeadCell).Text("Mặt hàng");
                            header.Cell().Element(HeadCell).Text("ĐVT");
                            header.Cell().Element(HeadCell).AlignRight().Text("SL");
                            header.Cell().Element(HeadCell).AlignRight().Text("Đơn giá");
                            header.Cell().Element(HeadCell).AlignRight().Text("Thành tiền");
                        });

                        var stt = 1;
                        foreach (var l in inv.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(stt.ToString());
                            table.Cell().Element(BodyCell).Text(l.MatHang ?? "");
                            table.Cell().Element(BodyCell).Text(l.DonViTinh ?? "");
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatQty(l.SoLuong));
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.DonGia));
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.ThanhTien));
                            stt++;
                        }
                    });

                    col.Item().PaddingTop(4, Unit.Point).AlignRight().Text($"Tiền hàng: {FormatMoney(inv.TienHang)}");
                    if (inv.TienGiamGia != 0)
                        col.Item().AlignRight().Text($"Giảm giá: {FormatMoney(inv.TienGiamGia)}");
                    col.Item().AlignRight().Text($"TỔNG CỘNG: {FormatMoney(inv.TongCong)} đ").Bold().FontSize(12);
                    if (inv.KhachDua != 0)
                    {
                        col.Item().AlignRight().Text($"Khách đưa: {FormatMoney(inv.KhachDua)}");
                        col.Item().AlignRight().Text($"Trả lại: {FormatMoney(inv.TraLai)}");
                    }
                    if (inv.ConNo != 0)
                        col.Item().AlignRight().Text($"Còn nợ: {FormatMoney(inv.ConNo)}").Bold();

                    col.Item().PaddingTop(4, Unit.Point)
                        .Text(VietnameseNumber.ToWords(inv.TongCong)).Italic().FontSize(9);

                    if (!string.IsNullOrWhiteSpace(inv.GhiChu))
                        col.Item().Text($"Ghi chú: {inv.GhiChu}").FontSize(9);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Cảm ơn quý khách! ");
                    x.Span($"— In lúc {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                });
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
