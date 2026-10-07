using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository hóa đơn bán sỉ (mục 4.2) — cùng cấu trúc POS bán lẻ nhưng DOC_TYPE='BAN_SI',
/// số phiếu BS (BS26/00001), KHACHDUA=tiền khách đã trả (ghi nợ phần CONNO).
/// Ghi TDONHANG + TDONHANGCHITIET + W_KHOLEDGER + giảm OTONKHO trong 1 transaction (W_ORIGIN='WEB');
/// có kiểm tra đủ tồn trước khi xuất. LOAI/MODE đọc từ W_DOC_TYPE_MAP (quy tắc vàng 5).
/// </summary>
public sealed class WholesaleRepository
{
    private readonly MySqlConnectionFactory _factory;
    public WholesaleRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<PosSaleResult> CreateSaleAsync(
        PosSaleRequest req, SaleTotals totals, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var map = await DocRepositoryHelpers.GetDocTypeAsync(conn, tx, "BAN_SI", ct);

            // Kiểm tra đủ tồn tại kho xuất trước khi ghi (trừ kho CHOPHEPAMKHO=1 — cho phép âm)
            var choPhepAm = await conn.ExecuteScalarAsync<long>(
                "SELECT COALESCE(CHOPHEPAMKHO,0) FROM DKHOHANG WHERE ID = @kho LIMIT 1",
                new { kho = req.KhoId }, tx) == 1;
            if (!choPhepAm)
            {
                var stocks = await DocRepositoryHelpers.GetStockAsync(
                    conn, tx, req.KhoId, req.Lines.Select(l => l.MatHangId).ToList(), ct);
                foreach (var l in req.Lines)
                    DocRepositoryHelpers.EnsureStock(stocks, l.MatHangId, l.SoLuong);
            }

            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, map.Prefix, ct);
            var donHangId = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;
            var ngayMilli = new DateTimeOffset(ngay).ToUnixTimeMilliseconds();
            var lineIndex = 0;

            await conn.ExecuteAsync(
                """
                INSERT INTO TDONHANG
                (ID, NAME, NOTE, STATUS, USERCREATEDID, NGAY, TIMECREATED,
                 TIENHANG, TIENGIAMGIA, TONGCONG,
                 DKHACHHANGID, DNHANVIENXUATID, DKHOXUATID, DIENGIAI,
                 KHACHDUA, TRALAI, CONNO, TIENTHANHTOAN, LOAI, MODE,
                 W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @uc, @ngay, @tc,
                 @tienhang, @tiengiam, @tongcong,
                 @kh, @nv, @kho, @diengiai,
                 @khachdua, @tralai, @conno, @khachdua, @loai, @mode,
                 'WEB', 0)
                """,
                new
                {
                    id = donHangId,
                    sophieu = soPhieu,
                    note = req.GhiChu ?? (object?)null,
                    uc = userId,
                    ngay,
                    tc = ngay,
                    tienhang = totals.TienHang,
                    tiengiam = totals.TienGiamGia,
                    tongcong = totals.TongCong,
                    kh = (object?)req.KhachHangId ?? DBNull.Value,
                    nv = (object?)req.NhanVienId ?? DBNull.Value,
                    kho = req.KhoId,
                    diengiai = "Bán sỉ",
                    khachdua = totals.KhachDua,
                    tralai = totals.TraLai,
                    conno = totals.ConNo,
                    loai = map.Loai,
                    mode = map.Mode
                }, tx);

            foreach (var l in req.Lines)
            {
                var thanhTien = Math.Max(0, Math.Round(
                    l.SoLuong * l.DonGia * (1 - Math.Clamp(l.TileGiamGia, 0, 100) / 100m) - Math.Max(0, l.GiamTheoTien),
                    0, MidpointRounding.AwayFromZero));
                await conn.ExecuteAsync(
                    """
                    INSERT INTO TDONHANGCHITIET
                    (ID, NOTE, STATUS, USERCREATEDID, TIMECREATED, TDONHANGID, DMATHANGID,
                     SLXUAT, DONGIA, TILEGIAMGIA, GIAMTHEOTIEN, THANHTIEN, DKHOHANGID, QUYDOI,
                     W_ORIGIN, W_DELETED)
                    VALUES
                    (@id, @note, 30, @uc, @tc, @dh, @mh,
                     @sl, @dongia, @tile, @giamtien, @thanhtien, @kho, 1,
                     'WEB', 0)
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    note = l.Note ?? (object?)null,
                    uc = userId,
                    tc = ngay,
                    dh = donHangId,
                    mh = l.MatHangId,
                    sl = l.SoLuong,
                    dongia = l.DonGia,
                    tile = l.TileGiamGia,
                    giamtien = l.GiamTheoTien,
                    thanhTien,
                    kho = l.KhoHangId ?? req.KhoId
                }, tx);

                await conn.ExecuteAsync(
                    """
                    INSERT INTO W_KHOLEDGER
                    (ID, ENTRY_DATE, DMathangId, DKhoHangId, DOC_TYPE, DOC_ID, DOC_NUMBER,
                     SL_NHAP, SL_XUAT, GIA_VON, ENTRY_SEQ, W_ORIGIN)
                    VALUES (@id, @ngay, @mh, @kho, 'BANSI', @dh, @sophieu, 0, @sl, NULL, @seq, 'WEB')
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    ngay = ngay.Date,
                    mh = l.MatHangId,
                    kho = l.KhoHangId ?? req.KhoId,
                    dh = donHangId,
                    sophieu = soPhieu,
                    sl = l.SoLuong,
                    seq = ngayMilli + lineIndex
                }, tx);
                lineIndex++;

                await conn.ExecuteAsync(
                    """
                    INSERT INTO OTONKHO (ID, STATUS, USERCREATEDID, TIMECREATED, DKHOHANGID, DMATHANGID, TON, W_SYNC_REV, W_ORIGIN, W_DELETED)
                    VALUES (@id, 30, @uc, @tc, @kho, @mh, -@sl, 0, 'WEB', 0)
                    ON DUPLICATE KEY UPDATE TON = TON - @sl, TIMEMODIFIED = NOW(3)
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    uc = userId,
                    tc = ngay,
                    kho = l.KhoHangId ?? req.KhoId,
                    mh = l.MatHangId,
                    sl = l.SoLuong
                }, tx);
            }

            await tx.CommitAsync(ct);
            return new PosSaleResult(donHangId, soPhieu, totals.TienHang, totals.TienGiamGia,
                totals.TongCong, totals.KhachDua, totals.TraLai, totals.ConNo);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
