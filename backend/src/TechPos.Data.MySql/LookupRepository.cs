using Dapper;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository tra cứu (read-only) phục vụ module giao hàng 4.10: đơn vị chuyển phát,
/// xe giao hàng và danh sách đơn hàng web (TDONHANG W_ORIGIN='WEB') để gắn vào phiếu.
/// </summary>
public sealed class LookupRepository
{
    private readonly MySqlConnectionFactory _factory;
    public LookupRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<LookupItem>> GetDeliveryPartnersAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<LookupItem>(
            "SELECT ID AS Id, NAME AS Name FROM DDONVICHUYENPHAT " +
            "WHERE STATUS = 30 AND W_DELETED = 0 ORDER BY NAME")).ToList();
    }

    public async Task<IReadOnlyList<LookupItem>> GetVehiclesAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<LookupItem>(
            "SELECT ID AS Id, CONCAT(NAME, IFNULL(CONCAT(' — ', BIENSO), '')) AS Name FROM DXEGIAOHANG " +
            "WHERE W_DELETED = 0 ORDER BY NAME")).ToList();
    }

    /// <summary>Đơn hàng tạo từ web (W_ORIGIN='WEB') để gắn vào phiếu COD/giao xe.</summary>
    public async Task<IReadOnlyList<WebOrderItem>> GetWebOrdersAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<WebOrderItem>(
            """
            SELECT ID AS Id, NAME AS SoPhieu, NGAY AS Ngay, TONGCONG AS TongCong,
                   (SELECT NAME FROM DKHACHHANG k WHERE k.ID = h.DKHACHHANGID) AS KhachHang
            FROM TDONHANG h
            WHERE W_ORIGIN = 'WEB' AND W_DELETED = 0 AND STATUS = 30
            ORDER BY TIMECREATED DESC
            LIMIT 200
            """)).ToList();
    }
}