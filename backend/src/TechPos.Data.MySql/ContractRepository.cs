using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository hợp đồng (mục 4.9) — THOPDONG + THOPDONGCHITIET, ghi W_ORIGIN='WEB',
/// số phiếu HD, 1 transaction. Bảng riêng (không qua TDONHANG).
/// Head chỉ có TONGCONG + DATCOC (không có TIENHANG/TIENGIAMGIA như báo giá).
/// </summary>
public sealed class ContractRepository
{
    private readonly MySqlConnectionFactory _factory;
    public ContractRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<ContractResult> CreateAsync(
        ContractRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, "HD", ct);
            var id = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;

            var tienHang = Math.Round(req.Lines.Sum(l =>
                l.SoLuong * l.DonGia * (1 - Math.Clamp(l.TileGiamGia, 0, 100) / 100m)), 0, MidpointRounding.AwayFromZero);
            var giam = Math.Clamp(req.TienGiamGia, 0, tienHang);
            var tongCong = tienHang - giam;

            await conn.ExecuteAsync(
                """
                INSERT INTO THOPDONG
                (ID, NAME, NOTE, STATUS, NGAY, USERCREATEDID, TIMECREATED,
                 NGAYKY, TUNGAY, DENNGAY, DNHANVIENID, DKHACHHANGID, DLOAIHOPDONGID,
                 DTRANGTHAIHOPDONGID, DATCOC, TONGCONG, W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @ngay, @uc, @tc,
                 @ngayky, @tungay, @denngay, @nv, @kh, @loai,
                 @trangthai, @datcoc, @tongcong, 'WEB', 0)
                """,
                new
                {
                    id,
                    sophieu = soPhieu,
                    note = req.GhiChu ?? (object?)null,
                    ngay,
                    uc = userId,
                    tc = ngay,
                    ngayky = (object?)req.NgayKy ?? DBNull.Value,
                    tungay = (object?)req.TuNgay ?? DBNull.Value,
                    denngay = (object?)req.DenNgay ?? DBNull.Value,
                    nv = (object?)req.NhanVienId ?? DBNull.Value,
                    kh = (object?)req.KhachHangId ?? DBNull.Value,
                    loai = (object?)req.LoaiHopDongId ?? DBNull.Value,
                    trangthai = (object?)req.TrangThaiHopDongId ?? DBNull.Value,
                    datcoc = req.DatCoc,
                    tongcong = tongCong
                }, tx);

            foreach (var l in req.Lines)
            {
                var thanhTien = Math.Max(0, Math.Round(
                    l.SoLuong * l.DonGia * (1 - Math.Clamp(l.TileGiamGia, 0, 100) / 100m), 0, MidpointRounding.AwayFromZero));
                await conn.ExecuteAsync(
                    """
                    INSERT INTO THOPDONGCHITIET
                    (ID, NOTE, STATUS, USERCREATEDID, TIMECREATED, THOPDONGID, DMATHANGID,
                     SOLUONG, DONGIA, TILEGIAMGIA, THANHTIEN, W_ORIGIN, W_DELETED)
                    VALUES
                    (@id, @note, 30, @uc, @tc, @hd, @mh,
                     @sl, @dongia, @tile, @thanhtien, 'WEB', 0)
                    """, new
                {
                    id = Guid.NewGuid().ToString(),
                    note = l.Note ?? (object?)null,
                    uc = userId,
                    tc = ngay,
                    hd = id,
                    mh = l.MatHangId,
                    sl = l.SoLuong,
                    dongia = l.DonGia,
                    tile = l.TileGiamGia,
                    thanhTien
                }, tx);
            }

            await tx.CommitAsync(ct);
            return new ContractResult(id, soPhieu, tienHang, tongCong, req.Lines.Count);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}