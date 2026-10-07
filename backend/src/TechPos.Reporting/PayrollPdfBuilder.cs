using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechPos.Shared;

namespace TechPos.Reporting;

/// <summary>
/// Sinh PDF bảng lương tháng A4 (QuestPDF) — Phase 6 (KE-HOACH §6.3: "bảng lương").
/// Input: PayrollDto (TBANGLUONG + TBANGLUONGTONGHOP — Phase 5.3). Không truy vấn DB.
/// </summary>
public static class PayrollPdfBuilder
{
    static PayrollPdfBuilder() => QuestPDF.Settings.License = LicenseType.Community;

    public static byte[] Build(PayrollDto p, string companyName = "Cửa hàng Hoàn Ngọc")
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(companyName).Bold().FontSize(14);
                    col.Item().AlignCenter().Text($"BẢNG LƯƠNG THÁNG {p.Thang:00}/{p.Nam}").FontSize(12).Bold();
                    col.Item().PaddingTop(4, Unit.Point).Row(row =>
                    {
                        row.RelativeItem().Text($"Số phiếu: {p.SoPhieu}").SemiBold();
                        row.RelativeItem().AlignRight().Text(p.Ngay?.ToString("dd/MM/yyyy") ?? "");
                    });
                    if (!string.IsNullOrWhiteSpace(p.Note))
                        col.Item().Text($"Ghi chú: {p.Note}").FontSize(9);
                });

                page.Content().PaddingVertical(8, Unit.Point).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(24, Unit.Point);   // STT
                        c.RelativeColumn(3);                // NV
                        c.ConstantColumn(40, Unit.Point);   // số công
                        c.ConstantColumn(70, Unit.Point);   // lương ca
                        c.ConstantColumn(78, Unit.Point);   // lương tháng
                        c.ConstantColumn(60, Unit.Point);   // thưởng
                        c.ConstantColumn(55, Unit.Point);   // phụ cấp
                        c.ConstantColumn(50, Unit.Point);   // phạt
                        c.ConstantColumn(62, Unit.Point);   // tạm ứng
                        c.ConstantColumn(80, Unit.Point);   // thực nhận
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeadCell).Text("STT");
                        header.Cell().Element(HeadCell).Text("Nhân viên");
                        header.Cell().Element(HeadCell).AlignRight().Text("Công");
                        header.Cell().Element(HeadCell).AlignRight().Text("Lương ca");
                        header.Cell().Element(HeadCell).AlignRight().Text("Lương tháng");
                        header.Cell().Element(HeadCell).AlignRight().Text("Thưởng");
                        header.Cell().Element(HeadCell).AlignRight().Text("Phụ cấp");
                        header.Cell().Element(HeadCell).AlignRight().Text("Phạt");
                        header.Cell().Element(HeadCell).AlignRight().Text("Tạm ứng");
                        header.Cell().Element(HeadCell).AlignRight().Text("Thực nhận");
                    });

                    var stt = 1;
                    foreach (var l in p.Lines)
                    {
                        table.Cell().Element(BodyCell).Text(stt.ToString());
                        table.Cell().Element(BodyCell).Text(l.TenNhanVien ?? "");
                        table.Cell().Element(BodyCell).AlignRight().Text(l.SoCong.ToString());
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.LuongCa));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.LuongThang));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.Thuong));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.PhuCap));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.Phat));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.TamUng));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.ThucNhan)).Bold();
                        stt++;
                    }
                });

                page.Footer().Column(footCol =>
                {
                    footCol.Item().PaddingTop(6, Unit.Point).AlignRight()
                        .Text($"TỔNG THỰC NHẬN: {FormatMoney(p.TongThucNhan)} đ").Bold().FontSize(11);
                    footCol.Item().PaddingTop(22, Unit.Point).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Text("Người lập").FontSize(9);
                        row.RelativeItem().AlignCenter().Text("Kế toán").FontSize(9);
                        row.RelativeItem().AlignCenter().Text("Giám đốc").FontSize(9);
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

    private static string FormatMoney(decimal v) => v.ToString("N0");
}
