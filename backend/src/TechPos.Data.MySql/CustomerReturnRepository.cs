using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository phiếu khách trả lại hàng (mục 4.3) — hàng về kho (SLNHAP, OTONKHO(+),
/// ledger 'TRALAIKH'), TRALAI = tiền shop trả lại khách. TDONHANG: DKHONHAPID=kho nhận
/// hàng về, DKHACHHANGID=khách trả. LOAI/MODE từ W_DOC_TYPE_MAP 'TRA_LAI_KH' (quy tắc vàng 5).
/// </summary>
public sealed class CustomerReturnRepository
{
    private readonly MySqlConnectionFactory _factory;
    public CustomerReturnRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<CustomerReturnResult> CreateAsync(
        CustomerReturnRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var map = await DocRepositoryHelpers.GetDocTypeAsync(conn, tx, "TRA_LAI_KH", ct);
            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, map.Prefix, ct);

            var donHangId = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;
            var ngayMilli = new DateTimeOffset(ngay).ToUnixTimeMilliseconds();

            var tienHang = Math.Round(req.Lines.Sum(l => l.SoLuong * l.DonGia), 0, MidpointRounding.AwayFromZero);
            var giam = Math.Clamp(req.TienGiamGia, 0, tienHang);
            var tongCong = tienHang - giam;
            var traLai = Math.Clamp(req.TienTraLai, 0, tongCong);
            var lineIndex = 0;

            await conn.ExecuteAsync(
                """
                INSERT INTO TDONHANG
                (ID, NAME, NOTE, STATUS, USERCREATEDID, NGAY, TIMECREATED,
                 TIENHANG, TIENGIAMGIA, TONGCONG,
                 DKHACHHANGID, DNHANVIENNHAPID, DKHONHAPID, DIENGIAI,
                 KHACHDUA, TRALAI, CONNO, TIENTHANHTOAN, LOAI, MODE,
                 W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @uc, @ngay, @tc,
                 @tienhang, @giam, @tongcong,
                 @kh, @nv, @kho, @diengiai,
                 0, @tralai, 0, @tralai, @loai, @mode,
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
                    kh = (object?)req.KhachHangId ?? DBNull.Value,
                    nv = (object?)req.NhanVienId ?? DBNull.Value,
                    kho = req.KhoNhanId,
                    diengiai = "Khách trả lại hàng",
                    tralai = traLai,
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
                     SLNHAP, DONGIA, THANHTIEN, DKHOHANGID, QUYDOI,
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
                    kho = req.KhoNhanId
                }, tx);

                await conn.ExecuteAsync(
                    """
                    INSERT INTO W_KHOLEDGER
                    (ID, ENTRY_DATE, DMathangId, DKhoHangId, DOC_TYPE, DOC_ID, DOC_NUMBER,
                     SL_NHAP, SL_XUAT, GIA_VON, ENTRY_SEQ, W_ORIGIN)
                    VALUES (@id, @ngay, @mh, @kho, 'TRALAIKH', @dh, @sophieu, @sl, 0, NULL, @seq, 'WEB')
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    ngay = ngay.Date,
                    mh = l.MatHangId,
                    kho = req.KhoNhanId,
                    dh = donHangId,
                    sophieu = soPhieu,
                    sl = l.SoLuong,
                    seq = ngayMilli + lineIndex
                }, tx);
                lineIndex++;

                await conn.ExecuteAsync(
                    """
                    INSERT INTO OTONKHO (ID, STATUS, USERCREATEDID, TIMECREATED, DKHOHANGID, DMATHANGID, TON, W_SYNC_REV, W_ORIGIN, W_DELETED)
                    VALUES (@id, 30, @uc, @tc, @kho, @mh, @sl, 0, 'WEB', 0)
                    ON DUPLICATE KEY UPDATE TON = TON + @sl, TIMEMODIFIED = NOW(3)
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    uc = userId,
                    tc = ngay,
                    kho = req.KhoNhanId,
                    mh = l.MatHangId,
                    sl = l.SoLuong
                }, tx);
            }

            await tx.CommitAsync(ct);
            return new CustomerReturnResult(donHangId, soPhieu, tienHang, giam, tongCong, traLai);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
