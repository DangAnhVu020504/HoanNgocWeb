using FluentAssertions;
using TechPos.Reporting;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

/// <summary>Phase 6.3 — PDF phiếu kho + bảng lương phải sinh được (PDF %PDF- header, kích thước hợp lệ).</summary>
public class PdfBuilderTests
{
    [Fact]
    public void StockVoucherPdf_builds_receipt_voucher()
    {
        var v = new StockVoucherPrintDto
        {
            DocType = "NHAP_KHO",
            SoPhieu = "PN26/00001",
            Ngay = new DateTime(2026, 10, 1, 10, 30, 0),
            KhoNhap = "Kho chính",
            DoiTac = "NCC Phương Đông",
            TienHang = 1_250_000m,
            TongCong = 1_250_000m,
            Lines =
            [
                new StockVoucherLineDto { MatHang = "Bàn bi-a", DonViTinh = "Cái", SlNhap = 2, DonGia = 600_000m, ThanhTien = 1_200_000m },
                new StockVoucherLineDto { MatHang = "Gậy", DonViTinh = "Cây", SlNhap = 1, DonGia = 50_000m, ThanhTien = 50_000m },
            ],
        };

        var bytes = StockVoucherPdfBuilder.Build(v);

        bytes.Length.Should().BeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void StockVoucherPdf_title_maps_doc_types()
    {
        StockVoucherPdfBuilder.Title("NHAP_KHO").Should().Be("PHIẾU NHẬP KHO");
        StockVoucherPdfBuilder.Title("CHUYEN_KHO").Should().Be("PHIẾU CHUYỂN KHO");
        StockVoucherPdfBuilder.Title("KIEM_KE").Should().Be("PHIẾU KIỂM KÊ");
        StockVoucherPdfBuilder.Title("UNKNOWN").Should().Be("PHIẾU KHO");
    }

    [Fact]
    public void PayrollPdf_builds_monthly_sheet()
    {
        var p = new PayrollDto
        {
            Id = "id-1", SoPhieu = "BL26/00001", Thang = 10, Nam = 2026,
            Lines =
            [
                new PayrollLineDto { TenNhanVien = "Nguyễn Văn A", SoCong = 26, LuongCa = 200_000m, ThucNhan = 5_200_000m },
                new PayrollLineDto { TenNhanVien = "Trần Thị B", SoCong = 24, LuongThang = 6_000_000m, TamUng = 1_000_000m, ThucNhan = 5_000_000m },
            ],
            TongThucNhan = 10_200_000m,
        };

        var bytes = PayrollPdfBuilder.Build(p);

        bytes.Length.Should().BeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
    }
}
