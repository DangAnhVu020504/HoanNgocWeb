namespace TechPos.Shared;

// =====================================================================
// Phase 6 — Report Center (ROADMAP Phase 6, KE-HOACH §6.5):
//   SALES_BY_DAY      Tổng hợp bán hàng theo ngày (TDONHANG bán hàng)
//   SALES_BY_ITEM     Mặt hàng bán chạy trong kỳ (TDONHANGCHITIET)
//   CASH_BY_DAY       Tổng hợp thu chi theo ngày (TTHUCHI)
//   CASH_BY_REASON    Thu chi theo lý do (TTHUCHI + DLYDOTHUCHI)
//   INVENTORY_STOCK   Tồn kho (OTONKHO)
//   XNT_WAREHOUSE     Xuất – nhập – tồn (W_KHOLEDGER, tồn đầu kỳ + trong kỳ)
//   PAYROLL_MONTH     Bảng lương tháng (TBANGLUONG + TONGHOP — đã có Phase 5.3)
//   INVOICE           Hoá đơn in A5 (TDONHANG + CHITIET)
// Tiền = decimal exact (quy tắc vàng 4). READ ONLY trên mirror.
// =====================================================================

/// <summary>Query chung của báo cáo theo kỳ.</summary>
public sealed record ReportQuery(
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? KhoId = null,
    string? NhanVienId = null,
    int Top = 0);

/// <summary>Dòng báo cáo bán hàng theo ngày.</summary>
public sealed class SalesByDayRow
{
    public DateTime Ngay { get; set; }
    public int SoHoaDon { get; set; }
    public decimal TienHang { get; set; }
    public decimal TienGiamGia { get; set; }
    public decimal TongCong { get; set; }
    public decimal ConNo { get; set; }
}

/// <summary>Dòng báo cáo doanh số theo mặt hàng.</summary>
public sealed class SalesByItemRow
{
    public string MatHangId { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DonViTinh { get; set; }
    public decimal SoLuong { get; set; }
    public decimal ThanhTien { get; set; }
}

/// <summary>Dòng tổng hợp thu chi theo ngày.</summary>
public sealed class CashByDayRow
{
    public DateTime Ngay { get; set; }
    public decimal Thu { get; set; }
    public decimal Chi { get; set; }
}

/// <summary>Dòng thu chi theo lý do.</summary>
public sealed class CashByReasonRow
{
    public string LyDo { get; set; } = string.Empty;
    public decimal Thu { get; set; }
    public decimal Chi { get; set; }
}

/// <summary>Dòng báo cáo xuất – nhập – tồn 1 mặt hàng trong kỳ.</summary>
public sealed class XntRow
{
    public string MatHangId { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DonViTinh { get; set; }
    public decimal TonDauKy { get; set; }
    public decimal SlNhap { get; set; }
    public decimal SlXuat { get; set; }
    public decimal TonCuoi { get; set; }
    public decimal GiaVon { get; set; }
    public decimal GiaTriTon { get; set; }
}

/// <summary>Hoá đơn bán lẻ/sỉ để in (A5) — đọc TDONHANG + chi tiết.</summary>
public sealed class InvoiceDto
{
    public string DonHangId { get; set; } = string.Empty;
    public string SoPhieu { get; set; } = string.Empty;
    public DateTime? Ngay { get; set; }
    public string? KhachHang { get; set; }
    public string? DiaChi { get; set; }
    public string? DienThoai { get; set; }
    public string? NhanVien { get; set; }
    public string? Kho { get; set; }
    public decimal TienHang { get; set; }
    public decimal TienGiamGia { get; set; }
    public decimal TongCong { get; set; }
    public decimal KhachDua { get; set; }
    public decimal TraLai { get; set; }
    public decimal ConNo { get; set; }
    public string? GhiChu { get; set; }
    public IReadOnlyList<InvoiceLineDto> Lines { get; set; } = [];
}

public sealed class InvoiceLineDto
{
    public string? MatHang { get; set; }
    public string? DonViTinh { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
}

/// <summary>Phiếu thu/chi để in.</summary>
public sealed class CashVoucherPrintDto
{
    public string SoPhieu { get; set; } = string.Empty;
    public DateTime? Ngay { get; set; }
    public string? TenDoiTuong { get; set; }
    public string? DienGiai { get; set; }
    public decimal Thu { get; set; }
    public decimal Chi { get; set; }
    public string? LyDo { get; set; }
    public string? GhiChu { get; set; }
}

/// <summary>Dto phiếu kho để in (nhập/xuất/chuyển/kiểm kê/xuất trả NCC) — dựng từ mirror TDONHANG.</summary>
public sealed class StockVoucherPrintDto
{
    public string DocType { get; set; } = string.Empty;   // NHAP_KHO | XUAT_KHO | CHUYEN_KHO | KIEM_KE | XUAT_TRA_NCC
    public string SoPhieu { get; set; } = string.Empty;
    public DateTime? Ngay { get; set; }
    public string? KhoNhap { get; set; }
    public string? KhoXuat { get; set; }
    public string? DoiTac { get; set; }                   // NCC / khách
    public string? NhanVien { get; set; }
    public string? DienGiai { get; set; }
    public string? GhiChu { get; set; }
    public decimal TienHang { get; set; }
    public decimal TienGiamGia { get; set; }
    public decimal TongCong { get; set; }
    public IReadOnlyList<StockVoucherLineDto> Lines { get; set; } = [];
}

public sealed class StockVoucherLineDto
{
    public string? MatHang { get; set; }
    public string? DonViTinh { get; set; }
    public decimal SlNhap { get; set; }
    public decimal SlXuat { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
}

// ---------- Phase 6.1 bổ sung: công nợ / tồn quỹ / KQKD ----------

/// <summary>Công nợ khách hàng tổng hợp (TDONHANG bán chưa trả hết + hạn nợ).</summary>
public sealed class CustomerDebtRow
{
    public string KhachHangId { get; set; } = string.Empty;
    public string? MaKhach { get; set; }
    public string TenKhach { get; set; } = string.Empty;
    public string? DienThoai { get; set; }
    public int SoHoaDon { get; set; }
    public decimal CongNo { get; set; }
    public int SongayChoNo { get; set; }
}

/// <summary>Công nợ nhà cung cấp tổng hợp (TDONHANG nhập chưa trả hết).</summary>
public sealed class SupplierDebtRow
{
    public string NhaCungCapId { get; set; } = string.Empty;
    public string? MaNhaCungCap { get; set; }
    public string TenNhaCungCap { get; set; } = string.Empty;
    public string? DienThoai { get; set; }
    public int SoPhieu { get; set; }
    public decimal CongNo { get; set; }
}

/// <summary>Dòng tồn quỹ theo ngày (luỹ kế THU − CHI từ TTHUCHI).</summary>
public sealed class CashBalanceRow
{
    public DateTime Ngay { get; set; }
    public decimal Thu { get; set; }
    public decimal Chi { get; set; }
    public decimal TonCuoiNgay { get; set; }
}

/// <summary>Kết quả kinh doanh trong kỳ: doanh thu − giá vốn − chi phí = lợi nhuận.</summary>
public sealed class KqkdRow
{
    public decimal DoanhThu { get; set; }
    public decimal GiamGia { get; set; }
    public decimal GiaVon { get; set; }
    public decimal ChiPhi { get; set; }
    public decimal LoiNhuan { get; set; }
}

/// <summary>Số tiền bằng chữ tiếng Việt (in hoá đơn/phiếu thu chi).</summary>
public static class VietnameseNumber
{
    public static string ToWords(decimal value)
    {
        var n = (long)Math.Floor(Math.Abs(value));
        if (n == 0) return "Không đồng";
        var units = new[] { "", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
        var scales = new[] { "", " nghìn", " triệu", " tỷ" };
        var groups = new List<int>();
        var remaining = n;
        while (remaining > 0) { groups.Add((int)(remaining % 1000)); remaining /= 1000; }

        var parts = new List<string>();
        for (var i = groups.Count - 1; i >= 0; i--)
        {
            var g = groups[i];
            if (g == 0 && parts.Count > 0) continue;
            var hundreds = g / 100;
            var tens = g % 100 / 10;
            var ones = g % 10;
            var text = "";
            if (hundreds > 0) text += units[hundreds] + " trăm ";
            switch (tens)
            {
                case 0 when text.Length > 0 && i > 0:
                    text += "lẻ ";
                    break;
                case > 1:
                    text += units[tens] + " mươi ";
                    break;
                case 1:
                    text += "mười ";
                    break;
            }
            if (tens > 1 && ones == 5) text += "lăm ";
            else if (tens == 1 && ones == 5) text += "lăm ";
            else if (ones > 0 || (tens == 0 && hundreds == 0 && i == 0)) text += units[ones] + " ";
            parts.Add(text.Trim() + scales[Math.Min(i, scales.Length - 1)]);
        }
        var result = string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
        return char.ToUpperInvariant(result[0]) + result[1..] + " đồng";
    }
}
