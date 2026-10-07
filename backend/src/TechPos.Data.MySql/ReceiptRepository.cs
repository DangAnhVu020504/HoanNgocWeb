using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository phiếu nhập kho (mục 4.4) — ghi TDONHANG (LOAI từ W_DOC_TYPE_MAP 'NHAP_KHO',
/// đã xác minh VERIFIED=1) + TDONHANGCHITIET (SLNHAP) + W_KHOLEDGER + upsert OTONKHO (+).
/// </summary>
public sealed class ReceiptRepository
{
    private readonly MySqlConnectionFactory _factory;
    public ReceiptRepository(MySqlConnectionFactory factory) => _factory = factory;

    private sealed class DocTypeMapRow
    {
        public int Loai { get; set; }
        public int? Mode { get; set; }
        public string SoHieuPrefix { get; set; } = string.Empty;
        public byte Verified { get; set; }
    }

    /// <summary>Tạo phiếu nhập kho hoàn chỉnh trong 1 transaction (W_ORIGIN='WEB').</summary>
    public async Task<ReceiptResult> CreateReceiptAsync(
        ReceiptRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var donHangId = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;

            var map = await conn.QueryFirstOrDefaultAsync<DocTypeMapRow>(
                "SELECT LOAI AS Loai, MODE_VAL AS Mode, PREFIX AS SoHieuPrefix, VERIFIED AS Verified FROM W_DOC_TYPE_MAP WHERE DOC_TYPE='NHAP_KHO' LIMIT 1",
                new { }, tx);
            if (map is null)
                throw new InvalidOperationException("W_DOC_TYPE_MAP chưa có 'NHAP_KHO' (quy tắc vàng 5).");
            if (map.Verified == 0)
                throw new InvalidOperationException("LOAI/MODE nhập kho chưa xác minh trên DB production.");

            // Số phiếu PNyy/xxxxx
            var year = DateTime.Now.Year;
            await conn.ExecuteAsync(
                "INSERT INTO W_DOC_SEQUENCE (PREFIX_, YEAR_, LAST_SEQ) VALUES (@p, @y, 0) " +
                "ON DUPLICATE KEY UPDATE PREFIX_ = PREFIX_",
                new { p = map.SoHieuPrefix, y = year }, tx);
            var seq = await conn.ExecuteScalarAsync<long>(
                "UPDATE W_DOC_SEQUENCE SET LAST_SEQ = LAST_SEQ + 1 WHERE PREFIX_=@p AND YEAR_=@y; " +
                "SELECT LAST_SEQ FROM W_DOC_SEQUENCE WHERE PREFIX_=@p AND YEAR_=@y;",
                new { p = map.SoHieuPrefix, y = year }, tx);
            var soPhieu = $"{map.SoHieuPrefix}{year % 100:D2}/{seq:D5}";

            // Tiền hàng từ dòng (server tính, không tin client)
            var tienHang = Math.Round(req.Lines.Sum(l => l.SlNhap * l.DonGia), 0, MidpointRounding.AwayFromZero);
            var giam = Math.Clamp(req.TienGiamGia, 0, tienHang);
            var tongCong = tienHang - giam + Math.Max(0, req.TienThue) + Math.Max(0, req.PhiVanChuyen);
            var daThanhToan = Math.Clamp(req.DaThanhToan, 0, tongCong);
            var conLai = tongCong - daThanhToan;
            var ngayMilli = new DateTimeOffset(ngay).ToUnixTimeMilliseconds();
            var lineIndex = 0;

            await conn.ExecuteAsync(
                """
                INSERT INTO TDONHANG
                (ID, NAME, NOTE, STATUS, USERCREATEDID, NGAY, TIMECREATED,
                 TIENHANG, TIENGIAMGIA, TONGCONG, PHIVANCHUYEN, TIENTHUE,
                 DNHACUNGCAPID, DNHANVIENNHAPID, DKHONHAPID, DIENGIAI,
                 DATHANHTOAN, THANHTOAN, CONLAI, LOAI, MODE,
                 W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @uc, @ngay, @tc,
                 @tienhang, @giam, @tongcong, @phivc, @thue,
                 @ncc, @nv, @kho, 'Nhập mua hàng',
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
                    phivc = req.PhiVanChuyen,
                    thue = req.TienThue,
                    ncc = (object?)req.NhaCungCapId ?? DBNull.Value,
                    nv = (object?)req.NhanVienId ?? DBNull.Value,
                    kho = req.KhoNhapId,
                    dathanhtoan = daThanhToan,
                    conlai = conLai,
                    loai = map.Loai,
                    mode = map.Mode ?? -1
                }, tx);

            foreach (var l in req.Lines)
            {
                var thanhTien = Math.Round(l.SlNhap * l.DonGia, 0, MidpointRounding.AwayFromZero);
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
                        sl = l.SlNhap,
                        dongia = l.DonGia,
                        thanhTien,
                        kho = req.KhoNhapId
                    }, tx);

                await conn.ExecuteAsync(
                    """
                    INSERT INTO W_KHOLEDGER
                    (ID, ENTRY_DATE, DMathangId, DKhoHangId, DOC_TYPE, DOC_ID, DOC_NUMBER,
                     SL_NHAP, SL_XUAT, GIA_VON, ENTRY_SEQ, W_ORIGIN)
                    VALUES (@id, @ngay, @mh, @kho, 'NHAPKHO', @dh, @sophieu, @sl, 0, @dongia, @seq, 'WEB')
                    """, new
                    {
                        id = Guid.NewGuid().ToString(),
                        ngay = ngay.Date,
                        mh = l.MatHangId,
                        kho = req.KhoNhapId,
                        dh = donHangId,
                        sophieu = soPhieu,
                        sl = l.SlNhap,
                        dongia = l.DonGia,
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
                        kho = req.KhoNhapId,
                        mh = l.MatHangId,
                        sl = l.SlNhap
                    }, tx);
            }

            await tx.CommitAsync(ct);
            return new ReceiptResult(donHangId, soPhieu, tienHang, giam, tongCong, daThanhToan, conLai);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
