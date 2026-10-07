using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository phiếu xuất trả lại nhà cung cấp (mục 4.5) — TDONHANG DNHACUNGCAPID +
/// DKHOXUATID + dòng SLXUAT + W_KHOLEDGER 'XUATTRANCC' + giảm OTONKHO trong 1 transaction
/// (W_ORIGIN='WEB'); DATHANHTOAN/CONLAI ghi công nợ phần còn lại. Có kiểm tra đủ tồn.
/// LOAI/MODE từ W_DOC_TYPE_MAP 'XUAT_TRA_NCC' (quy tắc vàng 5).
/// </summary>
public sealed class SupplierReturnRepository
{
    private readonly MySqlConnectionFactory _factory;
    public SupplierReturnRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<SupplierReturnResult> CreateAsync(
        SupplierReturnRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var map = await DocRepositoryHelpers.GetDocTypeAsync(conn, tx, "XUAT_TRA_NCC", ct);

            var stocks = await DocRepositoryHelpers.GetStockAsync(
                conn, tx, req.KhoXuatId, req.Lines.Select(l => l.MatHangId).ToList(), ct);
            foreach (var l in req.Lines)
                DocRepositoryHelpers.EnsureStock(stocks, l.MatHangId, l.SoLuong);

            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, map.Prefix, ct);
            var donHangId = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;
            var ngayMilli = new DateTimeOffset(ngay).ToUnixTimeMilliseconds();

            var tienHang = Math.Round(req.Lines.Sum(l => l.SoLuong * l.DonGia), 0, MidpointRounding.AwayFromZero);
            var giam = Math.Clamp(req.TienGiamGia, 0, tienHang);
            var tongCong = tienHang - giam;
            var daThanhToan = Math.Clamp(req.DaThanhToan, 0, tongCong);
            var conLai = tongCong - daThanhToan;
            var lineIndex = 0;

            await conn.ExecuteAsync(
                """
                INSERT INTO TDONHANG
                (ID, NAME, NOTE, STATUS, USERCREATEDID, NGAY, TIMECREATED,
                 TIENHANG, TIENGIAMGIA, TONGCONG,
                 DNHACUNGCAPID, DNHANVIENXUATID, DKHOXUATID, DIENGIAI,
                 DATHANHTOAN, THANHTOAN, CONLAI, LOAI, MODE,
                 W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @uc, @ngay, @tc,
                 @tienhang, @giam, @tongcong,
                 @ncc, @nv, @kho, @diengiai,
                 @dathanhtoan, @dathanhtoan, @conlai, @loai, @mode,
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
                    tienhang = tienHang,
                    giam,
                    tongcong = tongCong,
                    ncc = req.NhaCungCapId,
                    nv = (object?)req.NhanVienId ?? DBNull.Value,
                    kho = req.KhoXuatId,
                    diengiai = "Xuất trả nhà cung cấp",
                    dathanhtoan = daThanhToan,
                    conlai = conLai,
                    loai = map.Loai,
                    mode = map.Mode
                }, tx);

            foreach (var l in req.Lines)
            {
                var thanhTien = Math.Round(l.SoLuong * l.DonGia, 0, MidpointRounding.AwayFromZero);
                await conn.ExecuteAsync(
                    """
                    INSERT INTO TDONHANGCHITIET
                    (ID, NOTE, STATUS, USERCREATEDID, TIMECREATED, TDONHANGID, DMATHANGID,
                     SLXUAT, DONGIA, THANHTIEN, DKHOHANGID, QUYDOI,
                     W_ORIGIN, W_DELETED)
                    VALUES
                    (@id, @note, 30, @uc, @tc, @dh, @mh,
                     @sl, @dongia, @thanhtien, @kho, 1,
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
                    thanhTien,
                    kho = req.KhoXuatId
                }, tx);

                await conn.ExecuteAsync(
                    """
                    INSERT INTO W_KHOLEDGER
                    (ID, ENTRY_DATE, DMathangId, DKhoHangId, DOC_TYPE, DOC_ID, DOC_NUMBER,
                     SL_NHAP, SL_XUAT, GIA_VON, ENTRY_SEQ, W_ORIGIN)
                    VALUES (@id, @ngay, @mh, @kho, 'XUATTRANCC', @dh, @sophieu, 0, @sl, @dongia, @seq, 'WEB')
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    ngay = ngay.Date,
                    mh = l.MatHangId,
                    kho = req.KhoXuatId,
                    dh = donHangId,
                    sophieu = soPhieu,
                    sl = l.SoLuong,
                    dongia = l.DonGia,
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
                    kho = req.KhoXuatId,
                    mh = l.MatHangId,
                    sl = l.SoLuong
                }, tx);
            }

            await tx.CommitAsync(ct);
            return new SupplierReturnResult(donHangId, soPhieu, tienHang, giam, tongCong, daThanhToan, conLai);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
