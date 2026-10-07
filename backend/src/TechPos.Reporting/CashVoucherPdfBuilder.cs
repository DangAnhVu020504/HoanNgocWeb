using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechPos.Shared;

namespace TechPos.Reporting;

/// <summary>
/// Sinh PDF phiếu thu/chi A5 (QuestPDF) — Phase 6 (KE-HOACH §6.3, mục 6.5 nhóm QUỸ).
/// Input: CashVoucherPrintDto từ mirror (ReportRepository.GetCashVoucherPrintAsync). Không truy vấn DB.
/// </summary>
public static class CashVoucherPdfBuilder
{
    static CashVoucherPdfBuilder() => QuestPDF.Settings.License = LicenseType.Community;

    public static byte[] Build(CashVoucherPrintDto v, string companyName = "Cửa hàng Hoàn Ngọc")
    {
        var laThu = v.Thu != 0;
        var soTien = laThu ? v.Thu : v.Chi;
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
                    col.Item().AlignCenter().Text(laThu ? "PHIẾU THU" : "PHIẾU CHI").FontSize(12).Bold();
                    col.Item().PaddingTop(6, Unit.Point).Row(row =>
                    {
                        row.RelativeItem().Text($"Số phiếu: {v.SoPhieu}").SemiBold();
                        row.RelativeItem().AlignRight().Text(v.Ngay?.ToString("dd/MM/yyyy HH:mm") ?? "");
                    });
                });

                page.Content().PaddingVertical(8, Unit.Point).Column(col =>
                {
                    col.Spacing(6);

                    if (!string.IsNullOrWhiteSpace(v.TenDoiTuong))
                        col.Item().Text($"{(laThu ? "Người nộp" : "Người nhận")}: {v.TenDoiTuong}");
                    if (!string.IsNullOrWhiteSpace(v.LyDo))
                        col.Item().Text($"Lý do: {v.LyDo}");
                    if (!string.IsNullOrWhiteSpace(v.DienGiai))
                        col.Item().Text($"Diễn giải: {v.DienGiai}");

                    col.Item().PaddingTop(4, Unit.Point).AlignRight()
                        .Text($"SỐ TIỀN: {FormatMoney(soTien)} đ").Bold().FontSize(12);

                    col.Item().PaddingTop(4, Unit.Point)
                        .Text(VietnameseNumber.ToWords(soTien)).Italic().FontSize(9);

                    if (!string.IsNullOrWhiteSpace(v.GhiChu))
                        col.Item().Text($"Ghi chú: {v.GhiChu}").FontSize(9);
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().AlignCenter().Column(c =>
                    {
                        c.Item().Text("Người nộp / Người nhận").FontSize(9);
                        c.Item().PaddingTop(24, Unit.Point).Text("");
                    });
                    row.RelativeItem().AlignCenter().Column(c =>
                    {
                        c.Item().Text("Thủ quỹ / Kế toán").FontSize(9);
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

    private static string FormatMoney(decimal v) => v.ToString("N0");
}
