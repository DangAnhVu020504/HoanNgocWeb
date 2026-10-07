using ClosedXML.Excel;
using FluentAssertions;
using TechPos.Reporting;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>Phase 6 — Excel export (ClosedXML) phải giữ đúng header/số/tiền.</summary>
public class ExcelExporterTests
{
    private sealed record Row(string Code, string Name, decimal Tien, int Sl, DateTime Ngay);

    [Fact]
    public void Build_writes_header_and_rows()
    {
        var bytes = ExcelExporter.Build("Test",
        [
            new("Mã", r => ((Row)r).Code),
            new("Tên", r => ((Row)r).Name),
            new("Tiền", r => ((Row)r).Tien),
            new("SL", r => ((Row)r).Sl),
            new("Ngày", r => ((Row)r).Ngay),
        ], new object[]
        {
            new Row("A01", "Bàn bi-a 123", 1234500m, 2, new DateTime(2026, 10, 1)),
            new Row("A02", "Gậy Phương Đông", 999999m, 1, new DateTime(2026, 10, 2)),
        });

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(500);

        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheet(1);
        ws.Cell(1, 1).GetString().Should().Be("Mã");
        ws.Cell(1, 3).GetString().Should().Be("Tiền");
        ws.Cell(2, 1).GetString().Should().Be("A01");
        ws.Cell(2, 2).GetString().Should().Be("Bàn bi-a 123");
        ws.Cell(2, 3).GetDouble().Should().Be(1234500);
        ws.Cell(3, 4).GetDouble().Should().Be(1);
    }

    [Fact]
    public void Build_handles_unicode_vietnamese_and_nulls()
    {
        var bytes = ExcelExporter.Build("Kho",
        [new("Tên", r => ((Row)r).Name), new("Mã", r => ((Row)r).Code)],
        new object[] { new Row(null!, "Khách Đại Lý — Huyện Cư Kuin", 0m, 0, default) });

        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        wb.Worksheet(1).Cell(2, 1).GetString().Should().Be("Khách Đại Lý — Huyện Cư Kuin");
        wb.Worksheet(1).Cell(2, 2).GetString().Should().Be("");
    }

    [Fact]
    public void VietnameseNumber_reads_common_amounts()
    {
        VietnameseNumber.ToWords(0).Should().Be("Không đồng");
        VietnameseNumber.ToWords(125000).Should().Contain("hai mươi lăm nghìn");
        VietnameseNumber.ToWords(1000000).Should().Contain("triệu");
        VietnameseNumber.ToWords(15000).Should().Contain("Mười lăm nghìn");
        VietnameseNumber.ToWords(-50000).Should().Contain("Năm mươi nghìn");
    }
}
