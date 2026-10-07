using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository giao hàng theo xe (mục 4.10) — TGIAOHANGTHEOXE (head) + TGIAOHANGTHEOXECHITIET
/// (chi tiết: mỗi dòng = 1 đơn TDONHANG), ghi W_ORIGIN='WEB', số phiếu XE, 1 transaction.
/// </summary>
public sealed class VehicleDeliveryRepository
{
    private readonly MySqlConnectionFactory _factory;
    public VehicleDeliveryRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<VehicleDeliveryResult> CreateAsync(
        VehicleDeliveryRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, "XE", ct);
            var id = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;

            await conn.ExecuteAsync(
                """
                INSERT INTO TGIAOHANGTHEOXE
                (ID, NAME, NOTE, STATUS, NGAY, USERCREATEDID, TIMECREATED,
                 DNHANVIENID, DXEGIAOHANGID, W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @ngay, @uc, @tc,
                 @nv, @xe, 'WEB', 0)
                """,
                new
                {
                    id,
                    sophieu = soPhieu,
                    note = req.GhiChu ?? (object?)null,
                    ngay,
                    uc = userId,
                    tc = ngay,
                    nv = (object?)req.NhanVienId ?? DBNull.Value,
                    xe = req.XeGiaoHangId
                }, tx);

            var donHangIds = req.DonHangIds.Distinct().ToList();
            foreach (var donHangId in donHangIds)
            {
                await conn.ExecuteAsync(
                    """
                    INSERT INTO TGIAOHANGTHEOXECHITIET
                    (ID, STATUS, USERCREATEDID, TIMECREATED, TGIAOHANGTHEOXEID, TDONHANGID,
                     W_ORIGIN, W_DELETED)
                    VALUES
                    (@id, 30, @uc, @tc, @xe, @dh, 'WEB', 0)
                    """,
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        uc = userId,
                        tc = ngay,
                        xe = id,
                        dh = donHangId
                    }, tx);
            }

            await tx.CommitAsync(ct);
            return new VehicleDeliveryResult(id, soPhieu, donHangIds.Count);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}