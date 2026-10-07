using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechPos.Shared;

namespace TechPos.Reporting;

/// <summary>
/// Sinh PDF phiếu kho A5 (QuestPDF) — Phase 6 (KE-HOACH §6.3: "phiếu kho").
/// Tiêu đề theo DOC_TYPE; nạp từ ReportRepository.GetStockVoucherAsync. Không truy vấn DB.
/// </summary>
public static class StockVoucherPdfBuilder
{
    static StockVoucherPdfBuilder() => QuestPDF.Settings.License = LicenseType.Community;

    private static readonly Dictionary<string, string> Titles = new()
    {
        ["NHAP_KHO"] = "PHIẾU NHẬP KHO",
        ["XUAT_KHO"] = "PHIẾU XUẤT KHO",
        ["CHUYEN_KHO"] = "PHIẾU CHUYỂN KHO",
        ["KIEM_KE"] = "PHIẾU KIỂM KÊ",
        ["XUAT_TRA_NCC"] = "PHIẾU XUẤT TRẢ NCC",
        ["TRA_LAI_KH"] = "PHIẾU TRẢ LẠI KHÁCH",
        ["BAN_LE"] = "PHIẾU BÁN LẺ",
        ["BAN_SI"] = "PHIẾU BÁN SỈ",
    };

    public static string Title(string docType) =>
        Titles.GetValueOrDefault(docType, "PHIẾU KHO");

    public static byte[] Build(StockVoucherPrintDto v, string companyName = "Cửa hàng Hoàn Ngọc")
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(9.5f));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(companyName).Bold().FontSize(13);
                    col.Item().AlignCenter().Text(Title(v.DocType)).FontSize(12).Bold();
                    col.Item().PaddingTop(6, Unit.Point).Row(row =>
                    {
                        row.RelativeItem().Text($"Số phiếu: {v.SoPhieu}").SemiBold();
                        row.RelativeItem().AlignRight().Text(v.Ngay?.ToString("dd/MM/yyyy HH:mm") ?? "");
                    });
                });

                page.Content().PaddingVertical(8, Unit.Point).Column(col =>
                {
                    col.Spacing(4);

                    var info = new List<(string, string?)>
                    {
                        ("Kho xuất", v.KhoXuat),
                        ("Kho nhập", v.KhoNhap),
                        ("Đối tác", v.DoiTac),
                        ("Nhân viên", v.NhanVien),
                        ("Diễn giải", v.DienGiai),
                    };
                    foreach (var (label, val) in info)
                        if (!string.IsNullOrWhiteSpace(val))
                            col.Item().Text($"{label}: {val}");

                    col.Item().PaddingTop(4, Unit.Point).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22, Unit.Point);
                            c.RelativeColumn(3);
                            c.ConstantColumn(42, Unit.Point);
                            c.ConstantColumn(42, Unit.Point);
                            c.ConstantColumn(42, Unit.Point);
                            c.ConstantColumn(70, Unit.Point);
                            c.ConstantColumn(70, Unit.Point);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeadCell).Text("STT");
                            header.Cell().Element(HeadCell).Text("Mặt hàng");
                            header.Cell().Element(HeadCell).AlignRight().Text("Nhập");
                            header.Cell().Element(HeadCell).AlignRight().Text("Xuất");
                            header.Cell().Element(HeadCell).Text("ĐVT");
                            header.Cell().Element(HeadCell).AlignRight().Text("Đơn giá");
                            header.Cell().Element(HeadCell).AlignRight().Text("Thành tiền");
                        });

                        var stt = 1;
                        foreach (var l in v.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(stt.ToString());
                            table.Cell().Element(BodyCell).Text(l.MatHang ?? "");
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatQty(l.SlNhap));
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatQty(l.SlXuat));
                            table.Cell().Element(BodyCell).Text(l.DonViTinh ?? "");
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.DonGia));
                            table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.ThanhTien));
                            stt++;
                        }
                    });

                    if (v.TienGiamGia != 0)
                        col.Item().PaddingTop(4, Unit.Point).AlignRight().Text($"Giảm giá: {FormatMoney(v.TienGiamGia)}");
                    col.Item().AlignRight().Text($"TỔNG CỘNG: {FormatMoney(v.TongCong)} đ").Bold().FontSize(11);
                    if (!string.IsNullOrWhiteSpace(v.GhiChu))
                        col.Item().Text($"Ghi chú: {v.GhiChu}").FontSize(9);
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().AlignCenter().Column(c =>
                    {
                        c.Item().Text("Người lập phiếu").FontSize(9);
                        c.Item().PaddingTop(24, Unit.Point).Text("");
                    });
                    row.RelativeItem().AlignCenter().Column(c =>
                    {
                        c.Item().Text("Thủ kho").FontSize(9);
                        c.Item().PaddingTop(24, Unit.Point).Text("");
                    });
                    row.RelativeItem().AlignCenter().Column(c =>
                    {
                        c.Item().Text("Giám đốc").FontSize(9);
                        c.Item().PaddingTop(24, Unit.Point).Text("");
                    });
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
