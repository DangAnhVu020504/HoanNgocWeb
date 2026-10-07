using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository đặt mua nhà cung cấp (mục 4.9) — TDATHANGNCC + TDATHANGNCCCHITIET,
/// ghi W_ORIGIN='WEB', số phiếu DN, 1 transaction. Bảng riêng (không qua TDONHANG).
/// </summary>
public sealed class PurchaseOrderRepository
{
    private readonly MySqlConnectionFactory _factory;
    public PurchaseOrderRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<PurchaseOrderResult> CreateAsync(
        PurchaseOrderRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, "DN", ct);
            var id = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;

            var tienHang = Math.Round(req.Lines.Sum(l =>
                l.SoLuong * l.DonGia * (1 - Math.Clamp(l.TileGiamGia, 0, 100) / 100m)), 0, MidpointRounding.AwayFromZero);
            var giam = Math.Clamp(req.TienGiamGia, 0, tienHang);
            var tongCong = tienHang - giam + Math.Max(0, req.TienThue) + Math.Max(0, req.PhiVanChuyen);

            await conn.ExecuteAsync(
                """
                INSERT INTO TDATHANGNCC
                (ID, NAME, NOTE, STATUS, NGAY, USERCREATEDID, TIMECREATED,
                 DNHACUNGCAPID, TONGCONG, PHIVANCHUYEN, TIENGIAMGIA, TIENTHUE, TIENHANG,
                 W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @ngay, @uc, @tc,
                 @ncc, @tongcong, @phivc, @giam, @thue, @tienhang,
                 'WEB', 0)
                """,
                new
                {
                    id,
                    sophieu = soPhieu,
                    note = req.GhiChu ?? (object?)null,
                    ngay,
                    uc = userId,
                    tc = ngay,
                    ncc = req.NhaCungCapId,
                    tongcong = tongCong,
                    phivc = req.PhiVanChuyen,
                    giam,
                    thue = req.TienThue,
                    tienhang = tienHang
                }, tx);

            foreach (var l in req.Lines)
            {
                var thanhTien = Math.Max(0, Math.Round(
                    l.SoLuong * l.DonGia * (1 - Math.Clamp(l.TileGiamGia, 0, 100) / 100m), 0, MidpointRounding.AwayFromZero));
                await conn.ExecuteAsync(
                    """
                    INSERT INTO TDATHANGNCCCHITIET
                    (ID, NOTE, STATUS, USERCREATEDID, TIMECREATED, TDATHANGNCCID, DMATHANGID,
                     SOLUONG, DONGIA, TILEGIAMGIA, THANHTIEN, W_ORIGIN, W_DELETED)
                    VALUES
                    (@id, @note, 30, @uc, @tc, @dn, @mh,
                     @sl, @dongia, @tile, @thanhtien, 'WEB', 0)
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    note = l.Note ?? (object?)null,
                    uc = userId,
                    tc = ngay,
                    dn = id,
                    mh = l.MatHangId,
                    sl = l.SoLuong,
                    dongia = l.DonGia,
                    tile = l.TileGiamGia,
                    thanhTien
                }, tx);
            }

            await tx.CommitAsync(ct);
            return new PurchaseOrderResult(id, soPhieu, tienHang, tongCong, req.Lines.Count);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}