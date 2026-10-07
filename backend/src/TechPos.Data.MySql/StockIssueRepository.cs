using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository phiếu xuất kho (mục 4.5) — xuất nội bộ (không qua bán hàng):
/// TDONHANG DKHOXUATID + dòng SLXUAT + W_KHOLEDGER 'XUATKHO' + giảm OTONKHO trong
/// 1 transaction (W_ORIGIN='WEB'), có kiểm tra đủ tồn. LOAI/MODE từ W_DOC_TYPE_MAP
/// 'XUAT_KHO' (quy tắc vàng 5).
/// </summary>
public sealed class StockIssueRepository
{
    private readonly MySqlConnectionFactory _factory;
    public StockIssueRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<StockIssueResult> CreateAsync(
        StockIssueRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var map = await DocRepositoryHelpers.GetDocTypeAsync(conn, tx, "XUAT_KHO", ct);

            // Chỉ chặn tồn khi kho KHÔNG cho phép âm (CHOPHEPAMKHO=1 → bỏ chặn)
            var choPhepAm = await conn.ExecuteScalarAsync<long>(
                "SELECT COALESCE(CHOPHEPAMKHO,0) FROM DKHOHANG WHERE ID = @kho LIMIT 1",
                new { kho = req.KhoXuatId }, tx) == 1;
            if (!choPhepAm)
            {
                var stocks = await DocRepositoryHelpers.GetStockAsync(
                    conn, tx, req.KhoXuatId, req.Lines.Select(l => l.MatHangId).ToList(), ct);
                foreach (var l in req.Lines)
                    DocRepositoryHelpers.EnsureStock(stocks, l.MatHangId, l.SoLuong);
            }

            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, map.Prefix, ct);
            var donHangId = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;
            var ngayMilli = new DateTimeOffset(ngay).ToUnixTimeMilliseconds();

            var tienHang = Math.Round(req.Lines.Sum(l => l.SoLuong * l.DonGia), 0, MidpointRounding.AwayFromZero);
            var lineIndex = 0;

            await conn.ExecuteAsync(
                """
                INSERT INTO TDONHANG
                (ID, NAME, NOTE, STATUS, USERCREATEDID, NGAY, TIMECREATED,
                 TIENHANG, TONGCONG, DKHOXUATID, DNHANVIENXUATID, DIENGIAI, LOAI, MODE,
                 W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @uc, @ngay, @tc,
                 @tienhang, @tienhang, @kho, @nv, @diengiai, @loai, @mode,
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
                    kho = req.KhoXuatId,
                    nv = (object?)req.NhanVienId ?? DBNull.Value,
                    diengiai = string.IsNullOrWhiteSpace(req.DienGiai) ? "Xuất kho" : req.DienGiai.Trim(),
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
                    VALUES (@id, @ngay, @mh, @kho, 'XUATKHO', @dh, @sophieu, 0, @sl, @dongia, @seq, 'WEB')
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
            return new StockIssueResult(donHangId, soPhieu, tienHang, req.Lines.Count);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
