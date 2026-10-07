using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository phiếu kiểm kê (mục 4.7) — ghi TDONHANG LOAI=5 (đã xác minh trên demo: KTL, MODE=-1,
/// DKHONHAPID=kho được kiểm kê, dòng chênh lệch ghi SLNHAP (thiếu→nhập bù) / SLXUAT (dư→xuất bớt))
/// + W_KHOLEDGER + cập nhật OTONKHO về SL thực tế.
/// </summary>
public sealed class StocktakeRepository
{
    private readonly MySqlConnectionFactory _factory;
    public StocktakeRepository(MySqlConnectionFactory factory) => _factory = factory;

    private sealed class DocTypeMapRow
    {
        public int Loai { get; set; }
        public int? Mode { get; set; }
        public string SoHieuPrefix { get; set; } = string.Empty;
        public byte Verified { get; set; }
    }

    private sealed class CurrentStockRow
    {
        public string DMathangId { get; set; } = string.Empty;
        public decimal Ton { get; set; }
        public decimal GiaVon { get; set; }
    }

    public async Task<StocktakeResult> CreateStocktakeAsync(
        StocktakeRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var map = await conn.QueryFirstOrDefaultAsync<DocTypeMapRow>(
                "SELECT LOAI AS Loai, MODE_VAL AS Mode, PREFIX AS SoHieuPrefix, VERIFIED AS Verified FROM W_DOC_TYPE_MAP WHERE DOC_TYPE='KIEM_KE' LIMIT 1",
                new { }, tx);
            if (map is null)
                throw new InvalidOperationException("W_DOC_TYPE_MAP chưa có 'KIEM_KE' (quy tắc vàng 5).");
            if (map.Verified == 0)
                throw new InvalidOperationException("LOAI/MODE kiểm kê chưa xác minh trên DB production.");

            var matHangIds = req.Lines.Select(l => l.MatHangId).Distinct().ToList();
            var stocks = (await conn.QueryAsync<CurrentStockRow>(
                "SELECT DMATHANGID AS DMathangId, COALESCE(TON,0) AS Ton, COALESCE(m.GIAVON,0) AS GiaVon " +
                "FROM OTONKHO o JOIN DMATHANG m ON m.ID = o.DMATHANGID " +
                "WHERE o.DKHOHANGID = @kho AND o.W_DELETED = 0 AND m.W_DELETED = 0 " +
                (matHangIds.Count == 0 ? "AND 1=0" : "AND o.DMATHANGID IN @ids"),
                new { kho = req.KhoId, ids = matHangIds }, tx))
                .ToDictionary(r => r.DMathangId, r => r);

            var ngay = DateTime.Now;
            var donHangId = Guid.NewGuid().ToString();
            var ngayMilli = new DateTimeOffset(ngay).ToUnixTimeMilliseconds();

            var year = ngay.Year;
            await conn.ExecuteAsync(
                "INSERT INTO W_DOC_SEQUENCE (PREFIX_, YEAR_, LAST_SEQ) VALUES (@p, @y, 0) " +
                "ON DUPLICATE KEY UPDATE PREFIX_ = PREFIX_",
                new { p = map.SoHieuPrefix, y = year }, tx);
            var seq = await conn.ExecuteScalarAsync<long>(
                "UPDATE W_DOC_SEQUENCE SET LAST_SEQ = LAST_SEQ + 1 WHERE PREFIX_=@p AND YEAR_=@y; " +
                "SELECT LAST_SEQ FROM W_DOC_SEQUENCE WHERE PREFIX_=@p AND YEAR_=@y;",
                new { p = map.SoHieuPrefix, y = year }, tx);
            var soPhieu = $"{map.SoHieuPrefix}{year % 100:D2}/{seq:D5}";

            var tienTang = 0m;
            var tienGiam = 0m;
            var linesAdjusted = 0;

            await conn.ExecuteAsync(
                """
                INSERT INTO TDONHANG
                (ID, NAME, NOTE, STATUS, USERCREATEDID, NGAY, TIMECREATED,
                 TIENHANG, TONGCONG, DKHONHAPID, DIENGIAI, LOAI, MODE,
                 W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @uc, @ngay, @tc,
                 0, 0, @kho, @diengiai, @loai, @mode,
                 'WEB', 0)
                """,
                new
                {
                    id = donHangId,
                    sophieu = soPhieu,
                    note = req.GhiChu ?? (object?)DBNull.Value,
                    uc = userId,
                    ngay,
                    tc = ngay,
                    kho = req.KhoId,
                    diengiai = "Kiểm kê hàng hoá",
                    loai = map.Loai,
                    mode = map.Mode ?? -1
                }, tx);

            foreach (var l in req.Lines)
            {
                if (!stocks.TryGetValue(l.MatHangId, out var cur))
                    throw new InvalidOperationException(
                        $"Mặt hàng {l.MatHangId} không có dòng tồn tại kho này — kiểm kê chỉ hiệu chỉnh trên mặt hàng đã có tồn.");
                var chenhLech = l.SlThucTe - cur.Ton;
                if (chenhLech == 0) continue;

                var slNhap = chenhLech > 0 ? chenhLech : 0;
                var slXuat = chenhLech < 0 ? -chenhLech : 0;
                var thanhTien = Math.Round(Math.Abs(chenhLech) * cur.GiaVon, 0, MidpointRounding.AwayFromZero);
                if (chenhLech > 0) tienTang += thanhTien; else tienGiam += thanhTien;
                linesAdjusted++;

                await conn.ExecuteAsync(
                    """
                    INSERT INTO TDONHANGCHITIET
                    (ID, STATUS, USERCREATEDID, TIMECREATED, TDONHANGID, DMATHANGID,
                     SLNHAP, SLXUAT, SLTHUCTE, SLHETHONG, DONGIA, THANHTIEN, DKHOHANGID, QUYDOI,
                     W_ORIGIN, W_DELETED)
                    VALUES
                    (@id, 30, @uc, @tc, @dh, @mh,
                     @sln, @slx, @thucte, @hethong, @giavon, @thanhtien, @kho, 1,
                     'WEB', 0)
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    uc = userId,
                    tc = ngay,
                    dh = donHangId,
                    mh = l.MatHangId,
                    sln = slNhap,
                    slx = slXuat,
                    thucte = l.SlThucTe,
                    hethong = cur.Ton,
                    giavon = cur.GiaVon,
                    thanhTien,
                    kho = req.KhoId
                }, tx);

                if (slNhap > 0)
                {
                    await conn.ExecuteAsync(
                        """
                        INSERT INTO W_KHOLEDGER
                        (ID, ENTRY_DATE, DMathangId, DKhoHangId, DOC_TYPE, DOC_ID, DOC_NUMBER,
                         SL_NHAP, SL_XUAT, GIA_VON, ENTRY_SEQ, W_ORIGIN)
                        VALUES (@id, @ngay, @mh, @kho, 'KIEMKE', @dh, @sophieu, @sl, 0, @giavon, @seq, 'WEB')
                        """, new { id = Guid.NewGuid().ToString(), ngay = ngay.Date, mh = l.MatHangId, kho = req.KhoId, dh = donHangId, sophieu = soPhieu, sl = slNhap, giavon = cur.GiaVon, seq = ngayMilli + linesAdjusted }, tx);
                }
                else if (slXuat > 0)
                {
                    await conn.ExecuteAsync(
                        """
                        INSERT INTO W_KHOLEDGER
                        (ID, ENTRY_DATE, DMathangId, DKhoHangId, DOC_TYPE, DOC_ID, DOC_NUMBER,
                         SL_NHAP, SL_XUAT, GIA_VON, ENTRY_SEQ, W_ORIGIN)
                        VALUES (@id, @ngay, @mh, @kho, 'KIEMKE', @dh, @sophieu, 0, @sl, @giavon, @seq, 'WEB')
                        """, new { id = Guid.NewGuid().ToString(), ngay = ngay.Date, mh = l.MatHangId, kho = req.KhoId, dh = donHangId, sophieu = soPhieu, sl = slXuat, giavon = cur.GiaVon, seq = ngayMilli + linesAdjusted }, tx);
                }

                await conn.ExecuteAsync(
                    """
                    INSERT INTO OTONKHO (ID, STATUS, USERCREATEDID, TIMECREATED, DKHOHANGID, DMATHANGID, TON, W_SYNC_REV, W_ORIGIN, W_DELETED)
                    VALUES (@id, 30, @uc, @tc, @kho, @mh, @ton, 0, 'WEB', 0)
                    ON DUPLICATE KEY UPDATE TON = @ton, TIMEMODIFIED = NOW(3)
                    """, new { id = Guid.NewGuid().ToString(), uc = userId, tc = ngay, kho = req.KhoId, mh = l.MatHangId, ton = l.SlThucTe }, tx);
            }

            await conn.ExecuteAsync(
                "UPDATE TDONHANG SET TIENHANG = @t, TONGCONG = @t WHERE ID = @id",
                new { t = tienTang - tienGiam, id = donHangId }, tx);

            await tx.CommitAsync(ct);
            return new StocktakeResult(donHangId, soPhieu, tienTang, tienGiam, linesAdjusted);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
