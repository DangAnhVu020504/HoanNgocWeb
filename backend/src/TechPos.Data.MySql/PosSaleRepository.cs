using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository POS bán lẻ (mục 4.1) — ghi TDONHANG + TDONHANGCHITIET
/// + W_KHOLEDGER + upsert OTONKHO trong 1 transaction (W_ORIGIN='WEB').
/// LOAI/MODE đọc từ W_DOC_TYPE_MAP (không hard-code — quy tắc vàng 5).
/// </summary>
public sealed class PosSaleRepository
{
    private readonly MySqlConnectionFactory _factory;
    public PosSaleRepository(MySqlConnectionFactory factory) => _factory = factory;

    /// <summary>Row W_DOC_TYPE_MAP (class có public set — Dapper map trực tiếp, xem ProductDtos.cs).</summary>
    private sealed class DocTypeMapRow
    {
        public int Loai { get; set; }
        public int? Mode { get; set; }
        public string SoHieuPrefix { get; set; } = string.Empty;
        public byte Verified { get; set; }
    }

    /// <summary>Số phiếu kế tiếp theo prefix + năm (BL26/00001). Khỏi race nhờ transaction.</summary>
    public async Task<string> NextSoPhieuAsync(MySqlConnection conn, MySqlTransaction tx, string prefix, CancellationToken ct)
    {
        var year = DateTime.Now.Year;
        await conn.ExecuteAsync(
            """
            INSERT INTO W_DOC_SEQUENCE (PREFIX_, YEAR_, LAST_SEQ) VALUES (@p, @y, 0)
            ON DUPLICATE KEY UPDATE PREFIX_ = PREFIX_
            """, new { p = prefix, y = year }, tx);
        var seq = await conn.ExecuteScalarAsync<int>(
            "UPDATE W_DOC_SEQUENCE SET LAST_SEQ = LAST_SEQ + 1 WHERE PREFIX_=@p AND YEAR_=@y; " +
            "SELECT LAST_SEQ FROM W_DOC_SEQUENCE WHERE PREFIX_=@p AND YEAR_=@y;",
            new { p = prefix, y = year }, tx);
        return $"{prefix}{year % 100:D2}/{seq:D5}";
    }

    /// <summary>Tạo hoá đơn bán lẻ hoàn chỉnh trong 1 transaction (W_ORIGIN='WEB').</summary>
    public async Task<PosSaleResult> CreateSaleAsync(
        PosSaleRequest req, SaleTotals totals, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var donHangId = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;
            var ngayMilli = new DateTimeOffset(ngay).ToUnixTimeMilliseconds();
            var lineIndex = 0;

            // LOAI/MODE từ W_DOC_TYPE_MAP — không đoán (quy tắc vàng 5).
            // BAN_LE phải có VERIFIED=1 (đã xác minh trên DB production) mới cho phép ghi.
            var map = await conn.QueryFirstOrDefaultAsync<DocTypeMapRow>(
                "SELECT LOAI AS Loai, MODE_VAL AS Mode, PREFIX AS SoHieuPrefix, VERIFIED AS Verified FROM W_DOC_TYPE_MAP WHERE DOC_TYPE='BAN_LE' LIMIT 1",
                new { }, tx);
            if (map is null)
                throw new InvalidOperationException(
                    "W_DOC_TYPE_MAP chưa có cấu hình 'BAN_LE' — không thể xác định LOAI/MODE (quy tắc vàng 5).");
            if (map.Verified == 0)
                throw new InvalidOperationException(
                    "LOAI/MODE cho bán lẻ chưa được xác minh trên DB production (VERIFIED=0). " +
                    "Hãy xác minh tập giá trị LOAI/MODE trên DB thật rồi UPDATE W_DOC_TYPE_MAP SET VERIFIED=1.");

            // Chặn bán vượt tồn — trừ kho có CHOPHEPAMKHO=1 (cho phép tồn âm)
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

            var soPhieu = await NextSoPhieuAsync(conn, tx, map.SoHieuPrefix, ct);

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
                 @kh, @nv, @kho, NULL,
                 @khachdua, @tralai, @conno, @tongcong, @loai, @mode,
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
                    khachdua = totals.KhachDua,
                    tralai = totals.TraLai,
                    conno = totals.ConNo,
                    loai = map.Loai,
                    mode = map.Mode ?? -1
                }, tx);

            // Chi tiết hàng — bán hàng: SLXUAT, thành tiền từ PosCalculator
            foreach (var l in req.Lines)
            {
                var khoId = l.KhoHangId ?? req.KhoId;
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

                // Ledger kho (web-origin)
                await conn.ExecuteAsync(
                    """
                    INSERT INTO W_KHOLEDGER
                    (ID, ENTRY_DATE, DMathangId, DKhoHangId, DOC_TYPE, DOC_ID, DOC_NUMBER,
                     SL_NHAP, SL_XUAT, GIA_VON, ENTRY_SEQ, W_ORIGIN)
                    VALUES (@id, @ngay, @mh, @kho, 'BANLE', @dh, @sophieu, 0, @sl, NULL, @seq, 'WEB')
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

                // Upsert tồn kho: giảm SL bán
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
                        kho = khoId,
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
