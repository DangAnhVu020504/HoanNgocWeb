using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository báo giá (TBAOGIA) + đặt hàng KH (TDATHANG) — hai bảng cùng cấu trúc head/detail,
/// ghi W_ORIGIN='WEB', số phiếu từ W_DOC_SEQUENCE (BG/DH), 1 transaction.
/// Đây là chứng từ dùng bảng riêng (không qua TDONHANG ⇒ không cần W_DOC_TYPE_MAP).
/// </summary>
public sealed class CustomerDocRepository
{
    private readonly MySqlConnectionFactory _factory;
    public CustomerDocRepository(MySqlConnectionFactory factory) => _factory = factory;

    public Task<QuoteResult> CreateQuoteAsync(CustomerDocRequest req, string userId, CancellationToken ct = default)
        => CreateCoreAsync(req, userId, "TBAOGIA", "TBAOGIACHITIET", "TBAOGIAID", "BG",
            (id, sp, tien, tong, n) => new QuoteResult(id, sp, tien, tong, n), ct);

    public Task<OrderResult> CreateOrderAsync(CustomerDocRequest req, string userId, CancellationToken ct = default)
        => CreateCoreAsync(req, userId, "TDATHANG", "TDATHANGCHITIET", "TDATHANGID", "DH",
            (id, sp, tien, tong, n) => new OrderResult(id, sp, tien, tong, n), ct);

    private async Task<TResult> CreateCoreAsync<TResult>(
        CustomerDocRequest req, string userId, string docTable, string detailTable, string detailFk, string prefix,
        Func<string, string, decimal, decimal, int, TResult> build, CancellationToken ct)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, prefix, ct);
            var id = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;

            var tienHang = Math.Round(req.Lines.Sum(l =>
                l.SoLuong * l.DonGia * (1 - Math.Clamp(l.TileGiamGia, 0, 100) / 100m)), 0, MidpointRounding.AwayFromZero);
            var giam = Math.Clamp(req.TienGiamGia, 0, tienHang);
            var tongCong = tienHang - giam + Math.Max(0, req.TienThue) + Math.Max(0, req.PhiVanChuyen);

            await conn.ExecuteAsync(
                $"""
                INSERT INTO {docTable}
                (ID, NAME, NOTE, NGAY, TENKHACH, DIACHI, DIENTHOAI, EMAIL,
                 TIENHANG, TIENTHUE, TIENGIAMGIA, PHIVANCHUYEN, TONGCONG, LOAIGIA,
                 STATUS, USERCREATEDID, TIMECREATED, DKHACHHANGID, W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, @ngay, @tenkhach, @diachi, @dienthoai, @email,
                 @tienhang, @thue, @giam, @phivc, @tongcong, 0,
                 30, @uc, @tc, @kh, 'WEB', 0)
                """,
                new
                {
                    id,
                    sophieu = soPhieu,
                    note = req.GhiChu ?? (object?)null,
                    ngay,
                    tenkhach = req.TenKhach ?? (object?)null,
                    diachi = req.DiaChi ?? (object?)null,
                    dienthoai = req.DienThoai ?? (object?)null,
                    email = req.Email ?? (object?)null,
                    tienhang = tienHang,
                    thue = req.TienThue,
                    giam,
                    phivc = req.PhiVanChuyen,
                    tongcong = tongCong,
                    uc = userId,
                    tc = ngay,
                    kh = (object?)req.KhachHangId ?? DBNull.Value
                }, tx);

            foreach (var l in req.Lines)
            {
                var thanhTien = Math.Max(0, Math.Round(
                    l.SoLuong * l.DonGia * (1 - Math.Clamp(l.TileGiamGia, 0, 100) / 100m), 0, MidpointRounding.AwayFromZero));
                await conn.ExecuteAsync(
                    $"""
                    INSERT INTO {detailTable}
                    (ID, NOTE, STATUS, USERCREATEDID, TIMECREATED, {detailFk}, DMATHANGID,
                     SOLUONG, DONGIA, TILEGIAMGIA, THANHTIEN, W_ORIGIN, W_DELETED)
                    VALUES
                    (@id, @note, 30, @uc, @tc, @dh, @mh,
                     @sl, @dongia, @tile, @thanhtien, 'WEB', 0)
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    note = l.Note ?? (object?)null,
                    uc = userId,
                    tc = ngay,
                    dh = id,
                    mh = l.MatHangId,
                    sl = l.SoLuong,
                    dongia = l.DonGia,
                    tile = l.TileGiamGia,
                    thanhTien
                }, tx);
            }

            await tx.CommitAsync(ct);
            return build(id, soPhieu, tienHang, tongCong, req.Lines.Count);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}