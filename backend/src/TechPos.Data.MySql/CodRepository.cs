using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository giao hàng COD (mục 4.10) — TGUIHANGCOD (gửi hàng) + TKETQUAGUICOD (kết quả),
/// ghi W_ORIGIN='WEB', số phiếu COD/KQ, 1 transaction. Bảng riêng (không qua TDONHANG).
/// Liên kết đơn vào phiếu qua TDONHANG (cập nhật DIENGIAI/TGUIHANGCODID nếu có cột).
/// </summary>
public sealed class CodRepository
{
    private readonly MySqlConnectionFactory _factory;
    public CodRepository(MySqlConnectionFactory factory) => _factory = factory;

    /// <summary>Tạo phiếu gửi hàng COD — ghi TGUIHANGCOD.</summary>
    public async Task<CodShipmentResult> CreateShipmentAsync(
        CodShipmentRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, "COD", ct);
            var id = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;

            await conn.ExecuteAsync(
                """
                INSERT INTO TGUIHANGCOD
                (ID, NAME, NOTE, STATUS, NGAY, USERCREATEDID, TIMECREATED,
                 DDONVICHUYENPHATID, DNHANVIENID, W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @ngay, @uc, @tc,
                 @dvcp, @nv, 'WEB', 0)
                """,
                new
                {
                    id,
                    sophieu = soPhieu,
                    note = req.GhiChu ?? (object?)null,
                    ngay,
                    uc = userId,
                    tc = ngay,
                    dvcp = req.DonViChuyenPhatId,
                    nv = (object?)req.NhanVienId ?? DBNull.Value
                }, tx);

            await MarkOrdersAsync(conn, tx, req.DonHangIds, soPhieu, ct);
            await tx.CommitAsync(ct);
            return new CodShipmentResult(id, soPhieu, req.DonHangIds.Count);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>Tạo phiếu kết quả gửi COD — ghi TKETQUAGUICOD.</summary>
    public async Task<CodResultEntry> CreateResultAsync(
        CodResultRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, "KQ", ct);
            var id = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;

            await conn.ExecuteAsync(
                """
                INSERT INTO TKETQUAGUICOD
                (ID, NAME, NOTE, STATUS, NGAY, USERCREATEDID, TIMECREATED,
                 DNHANVIENID, DDONVICHUYENPHATID, W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @ngay, @uc, @tc,
                 @nv, @dvcp, 'WEB', 0)
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
                    dvcp = (object?)req.DonViChuyenPhatId ?? DBNull.Value
                }, tx);

            await tx.CommitAsync(ct);
            return new CodResultEntry(id, soPhieu, req.DonHangIds.Count);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>Gắn số phiếu COD vào diễn giải các đơn hàng (web) — chỉ đơn W_ORIGIN='WEB'.</summary>
    private static async Task MarkOrdersAsync(
        MySqlConnection conn, MySqlTransaction tx, IReadOnlyList<string> donHangIds, string soPhieu, CancellationToken ct)
    {
        var ids = donHangIds.Distinct().ToList();
        if (ids.Count == 0) return;
        await conn.ExecuteAsync(
            "UPDATE TDONHANG SET DIENGIAI = @dg, TIMEMODIFIED = NOW(3) " +
            "WHERE ID IN @ids AND W_ORIGIN = 'WEB' AND W_DELETED = 0",
            new { dg = $"Gửi COD {soPhieu}", ids }, tx);
    }
}