using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository phiếu chuyển kho (mục 4.6) — TDONHANG có cả DKHOXUATID + DKHONHAPID
/// + DNHANVIENXUATID + DNHANVIENNHAPID. Web ghi 2 dòng TDONHANGCHITIET/mặt hàng
/// (1 dòng SLXUAT tại kho xuất, 1 dòng SLNHAP tại kho nhập — quy ước web, chờ đối chiếu
/// production) + W_KHOLEDGER 2 dòng 'CHUYENKHO' + OTONKHO: kho xuất (−), kho nhập (+).
/// Có kiểm tra đủ tồn tại kho xuất. LOAI/MODE từ W_DOC_TYPE_MAP 'CHUYEN_KHO' (quy tắc vàng 5).
/// </summary>
public sealed class StockTransferRepository
{
    private readonly MySqlConnectionFactory _factory;
    public StockTransferRepository(MySqlConnectionFactory factory) => _factory = factory;

    private sealed class StockRow
    {
        public string DMathangId { get; set; } = string.Empty;
        public decimal Ton { get; set; }
        public decimal GiaVon { get; set; }
    }

    public async Task<StockTransferResult> CreateAsync(
        StockTransferRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var map = await DocRepositoryHelpers.GetDocTypeAsync(conn, tx, "CHUYEN_KHO", ct);

            var matHangIds = req.Lines.Select(l => l.MatHangId).Distinct().ToList();
            var stocks = (await conn.QueryAsync<StockRow>(
                "SELECT o.DMATHANGID AS DMathangId, COALESCE(o.TON,0) AS Ton, COALESCE(m.GIAVON,0) AS GiaVon " +
                "FROM OTONKHO o JOIN DMATHANG m ON m.ID = o.DMATHANGID " +
                "WHERE o.DKHOHANGID = @kho AND o.W_DELETED = 0 AND m.W_DELETED = 0 " +
                (matHangIds.Count == 0 ? "AND 1=0" : "AND o.DMATHANGID IN @ids"),
                new { kho = req.KhoXuatId, ids = matHangIds }, tx))
                .ToDictionary(r => r.DMathangId, r => r);
            var tonMap = stocks.ToDictionary(kv => kv.Key, kv => kv.Value.Ton);
            foreach (var l in req.Lines)
                DocRepositoryHelpers.EnsureStock(tonMap, l.MatHangId, l.SoLuong);

            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, map.Prefix, ct);
            var donHangId = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;
            var ngayMilli = new DateTimeOffset(ngay).ToUnixTimeMilliseconds();
            var lineIndex = 0;
            decimal tongSoLuong = 0;

            await conn.ExecuteAsync(
                """
                INSERT INTO TDONHANG
                (ID, NAME, NOTE, STATUS, USERCREATEDID, NGAY, TIMECREATED,
                 DKHOXUATID, DKHONHAPID, DNHANVIENXUATID, DNHANVIENNHAPID, DIENGIAI, LOAI, MODE,
                 W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @uc, @ngay, @tc,
                 @khoxuat, @khonhap, @nvx, @nvn, @diengiai, @loai, @mode,
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
                    khoxuat = req.KhoXuatId,
                    khonhap = req.KhoNhapId,
                    nvx = (object?)req.NhanVienXuatId ?? DBNull.Value,
                    nvn = (object?)req.NhanVienNhapId ?? DBNull.Value,
                    diengiai = "Chuyển kho",
                    loai = map.Loai,
                    mode = map.Mode
                }, tx);

            foreach (var l in req.Lines)
            {
                stocks.TryGetValue(l.MatHangId, out var cur);
                var giaVon = cur?.GiaVon ?? 0;
                tongSoLuong += l.SoLuong;

                // Dòng xuất tại kho xuất
                await conn.ExecuteAsync(
                    """
                    INSERT INTO TDONHANGCHITIET
                    (ID, NOTE, STATUS, USERCREATEDID, TIMECREATED, TDONHANGID, DMATHANGID,
                     SLXUAT, DONGIA, THANHTIEN, DKHOHANGID, QUYDOI,
                     W_ORIGIN, W_DELETED)
                    VALUES
                     (@id, @note, 30, @uc, @tc, @dh, @mh,
                     @sl, @giavon, @thanhtien, @khoxuat, 1,
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
                    giavon = giaVon,
                    thanhTien = Math.Round(l.SoLuong * giaVon, 0, MidpointRounding.AwayFromZero),
                    khoxuat = req.KhoXuatId
                }, tx);

                // Dòng nhập tại kho nhập
                await conn.ExecuteAsync(
                    """
                    INSERT INTO TDONHANGCHITIET
                    (ID, NOTE, STATUS, USERCREATEDID, TIMECREATED, TDONHANGID, DMATHANGID,
                     SLNHAP, DONGIA, THANHTIEN, DKHOHANGID, QUYDOI,
                     W_ORIGIN, W_DELETED)
                    VALUES
                     (@id, @note, 30, @uc, @tc, @dh, @mh,
                     @sl, @giavon, @thanhtien, @khonhap, 1,
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
                    giavon = giaVon,
                    thanhTien = Math.Round(l.SoLuong * giaVon, 0, MidpointRounding.AwayFromZero),
                    khonhap = req.KhoNhapId
                }, tx);

                // Ledger: xuất ở kho xuất + nhập ở kho nhập
                await conn.ExecuteAsync(
                    """
                    INSERT INTO W_KHOLEDGER
                    (ID, ENTRY_DATE, DMathangId, DKhoHangId, DOC_TYPE, DOC_ID, DOC_NUMBER,
                     SL_NHAP, SL_XUAT, GIA_VON, ENTRY_SEQ, W_ORIGIN)
                    VALUES (@id, @ngay, @mh, @kho, 'CHUYENKHO', @dh, @sophieu, 0, @sl, @giavon, @seq, 'WEB')
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    ngay = ngay.Date,
                    mh = l.MatHangId,
                    kho = req.KhoXuatId,
                    dh = donHangId,
                    sophieu = soPhieu,
                    sl = l.SoLuong,
                    giavon = giaVon,
                    seq = ngayMilli + lineIndex
                }, tx);
                lineIndex++;
                await conn.ExecuteAsync(
                    """
                    INSERT INTO W_KHOLEDGER
                    (ID, ENTRY_DATE, DMathangId, DKhoHangId, DOC_TYPE, DOC_ID, DOC_NUMBER,
                     SL_NHAP, SL_XUAT, GIA_VON, ENTRY_SEQ, W_ORIGIN)
                    VALUES (@id, @ngay, @mh, @kho, 'CHUYENKHO', @dh, @sophieu, @sl, 0, @giavon, @seq, 'WEB')
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    ngay = ngay.Date,
                    mh = l.MatHangId,
                    kho = req.KhoNhapId,
                    dh = donHangId,
                    sophieu = soPhieu,
                    sl = l.SoLuong,
                    giavon = giaVon,
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
                    sl = l.SoLuong
                }, tx);
            }

            await tx.CommitAsync(ct);
            return new StockTransferResult(donHangId, soPhieu, req.Lines.Count, tongSoLuong);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
