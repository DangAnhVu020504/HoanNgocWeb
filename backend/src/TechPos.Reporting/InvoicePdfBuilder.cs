using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechPos.Shared;

namespace TechPos.Reporting;

/// <summary>
/// Sinh PDF hoá đơn bán hàng A5 (QuestPDF 2025.7) — thiết kế "Royal Pearl & Champagne Gold".
/// Input: InvoiceDto từ mirror (ReportRepository.GetInvoiceAsync). Không truy vấn DB.
/// Bố cục: đầu hoá đơn (cửa hàng · số · ngày) → khối thông tin khách → bảng hàng có kẻ viền
/// → khối tổng kết → số tiền bằng chữ → ghi chú → chữ ký → chân trang.
/// </summary>
public static class InvoicePdfBuilder
{
    static InvoicePdfBuilder() => QuestPDF.Settings.License = LicenseType.Community;

    private static readonly Color Gold = Color.FromHex("#D4AF37");
    private static readonly Color GoldSoft = Color.FromHex("#FBF3DA");
    private static readonly Color GoldDeep = Color.FromHex("#8A6100");
    private static readonly Color Ink = Color.FromHex("#1C180E");
    private static readonly Color Muted = Color.FromHex("#7E7359");

    public static byte[] Build(InvoiceDto inv, string companyName = "Cửa hàng Hoàn Ngọc")
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(22, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Ink));

                // ===================== ĐẦU HOÁ ĐƠN =====================
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(companyName).Bold().FontSize(15).FontColor(GoldDeep);
                    col.Item().AlignCenter().Text("HÓA ĐƠN BÁN HÀNG")
                        .FontSize(11).LetterSpacing(0.15f).SemiBold().FontColor(Ink);
                    col.Item().PaddingTop(4).LineHorizontal(1.4f).LineColor(Gold);

                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Số: ").FontColor(Muted);
                            t.Span(inv.SoPhieu).Bold().FontColor(GoldDeep);
                        });
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.Span("Ngày: ").FontColor(Muted);
                            t.Span(inv.Ngay?.ToString("dd/MM/yyyy HH:mm") ?? "—");
                        });
                    });
                });

                // ===================== NỘI DUNG =====================
                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Spacing(7);

                    // ---- Khối thông tin khách hàng / chứng từ ----
                    col.Item().Background(GoldSoft).Border(0.5f).BorderColor(Gold).Padding(7).Column(info =>
                    {
                        info.Spacing(2);
                        info.Item().Row(r =>
                        {
                            r.RelativeItem().Text(t =>
                            {
                                t.Span("Khách hàng: ").FontColor(Muted);
                                t.Span(inv.KhachHang ?? "Khách lẻ").SemiBold();
                            });
                            if (!string.IsNullOrWhiteSpace(inv.DienThoai))
                                r.RelativeItem().AlignRight().Text(t =>
                                {
                                    t.Span("ĐT: ").FontColor(Muted);
                                    t.Span(inv.DienThoai);
                                });
                        });
                        if (!string.IsNullOrWhiteSpace(inv.DiaChi))
                            info.Item().Text(t =>
                            {
                                t.Span("Địa chỉ: ").FontColor(Muted);
                                t.Span(inv.DiaChi);
                            });
                        info.Item().Row(r =>
                        {
                            r.RelativeItem().Text(t =>
                            {
                                t.Span("Nhân viên xuất HĐ: ").FontColor(Muted);
                                t.Span(inv.NhanVien ?? "—").SemiBold();
                                if (!string.IsNullOrWhiteSpace(inv.NhanVienDienThoai))
                                {
                                    t.Span(" · ĐT: ").FontColor(Muted);
                                    t.Span(inv.NhanVienDienThoai);
                                }
                            });
                            r.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Kho xuất: ").FontColor(Muted);
                                t.Span(inv.Kho ?? "—");
                            });
                        });
                    });

                    // ---- Bảng mặt hàng (kẻ viền) ----
                    col.Item().PaddingTop(2).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(20, Unit.Point);
                            c.RelativeColumn(3);
                            c.ConstantColumn(34, Unit.Point);
                            c.ConstantColumn(34, Unit.Point);
                            c.ConstantColumn(58, Unit.Point);
                            c.ConstantColumn(64, Unit.Point);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeadCell).AlignCenter().Text("STT");
                            header.Cell().Element(HeadCell).Text("Tên hàng hóa");
                            header.Cell().Element(HeadCell).AlignCenter().Text("ĐVT");
                            header.Cell().Element(HeadCell).AlignRight().Text("SL");
                            header.Cell().Element(HeadCell).AlignRight().Text("Đơn giá");
                            header.Cell().Element(HeadCell).AlignRight().Text("Thành tiền");
                        });

                        var stt = 1;
                        foreach (var l in inv.Lines)
                        {
                            table.Cell().Element(BodyCell).AlignCenter().Text(stt.ToString());
                            table.Cell().Element(BodyCell).Text(l.MatHang ?? "");
                            table.Cell().Element(BodyCell).AlignCenter().Text(l.DonViTinh ?? "");
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatQty(l.SoLuong));
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.DonGia));
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.ThanhTien)).SemiBold();
                            stt++;
                        }
                    });

                    // ---- Khối tổng kết ----
                    col.Item().PaddingTop(4).AlignRight().Width(210).Column(tot =>
                    {
                        tot.Spacing(3);
                        tot.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Tiền hàng:").FontColor(Muted);
                            r.ConstantItem(110).AlignRight().Text(FormatMoney(inv.TienHang));
                        });
                        if (inv.TienGiamGia != 0)
                            tot.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Giảm giá:").FontColor(Muted);
                                r.ConstantItem(110).AlignRight().Text($"- {FormatMoney(inv.TienGiamGia)}").FontColor(Color.FromHex("#E11D48"));
                            });
                        tot.Item().LineHorizontal(0.5f).LineColor(Gold);
                        tot.Item().Background(GoldSoft).PaddingVertical(3).PaddingHorizontal(5).Row(r =>
                        {
                            r.RelativeItem().Text("TỔNG CỘNG:").Bold().FontColor(GoldDeep);
                            r.ConstantItem(110).AlignRight().Text($"{FormatMoney(inv.TongCong)} đ")
                                .Bold().FontSize(12).FontColor(GoldDeep);
                        });
                        if (inv.KhachDua != 0)
                            tot.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Khách đưa:").FontColor(Muted);
                                r.ConstantItem(110).AlignRight().Text(FormatMoney(inv.KhachDua));
                            });
                        if (inv.TraLai != 0)
                            tot.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Trả lại khách:").FontColor(Muted);
                                r.ConstantItem(110).AlignRight().Text(FormatMoney(inv.TraLai)).FontColor(Color.FromHex("#10B981"));
                            });
                        if (inv.ConNo != 0)
                            tot.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Còn nợ:").FontColor(Muted);
                                r.ConstantItem(110).AlignRight().Text(FormatMoney(inv.ConNo)).Bold().FontColor(Color.FromHex("#E11D48"));
                            });
                    });

                    // ---- Số tiền bằng chữ ----
                    col.Item().PaddingTop(4).Text(t =>
                    {
                        t.Span("Số tiền bằng chữ: ").FontColor(Muted);
                        t.Span(VietnameseNumber.ToWords(inv.TongCong)).Italic();
                    });

                    if (!string.IsNullOrWhiteSpace(inv.GhiChu))
                        col.Item().Text(t =>
                        {
                            t.Span("Ghi chú: ").FontColor(Muted);
                            t.Span(inv.GhiChu);
                        });

                    // ---- Chữ ký ----
                    col.Item().PaddingTop(18).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("Người mua hàng").SemiBold().FontColor(GoldDeep);
                            c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").FontSize(8).FontColor(Muted);
                        });
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("Người bán hàng").SemiBold().FontColor(GoldDeep);
                            c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").FontSize(8).FontColor(Muted);
                        });
                    });
                });

                // ===================== CHÂN TRANG =====================
                page.Footer().Column(col =>
                {
                    col.Item().PaddingBottom(3).LineHorizontal(0.5f).LineColor(Gold);
                    col.Item().AlignCenter().Text(t =>
                    {
                        t.Span("Cảm ơn quý khách — Hẹn gặp lại!").FontColor(GoldDeep).SemiBold();
                        t.Span($"   ·   In lúc {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Muted);
                    });
                });
            });
        }).GeneratePdf();
    }

    private static IContainer HeadCell(IContainer c) =>
        c.Background(GoldSoft)
            .Border(0.5f).BorderColor(Gold)
            .PaddingVertical(4)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.FontSize(9).SemiBold().FontColor(GoldDeep));

    private static IContainer BodyCell(IContainer c) =>
        c.Border(0.5f).BorderColor(Gold)
            .PaddingVertical(3)
            .PaddingHorizontal(4);

    private static string FormatQty(decimal q) => q % 1 == 0 ? ((long)q).ToString() : q.ToString("0.##");
    private static string FormatMoney(decimal v) => v.ToString("N0");
}
