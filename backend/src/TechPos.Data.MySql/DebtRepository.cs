using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository công nợ (mục 5.2) — đọc từ mirror TDONHANG:
/// nợ KH = SUM(CONNO) theo DKHACHHANGID (hóa đơn bán chưa trả hết),
/// nợ NCC = SUM(CONLAI) theo DNHACUNGCAPID (phiếu nhập chưa trả hết).
/// Desktop tự giảm CONNO/CONLAI khi thu/chi trên desktop nên mirror luôn là nợ hiện hành.
/// </summary>
public sealed class DebtRepository
{
    private readonly MySqlConnectionFactory _factory;
    public DebtRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<(IReadOnlyList<CustomerDebtDto> Items, long Total)> SearchCustomerDebtsAsync(
        CustomerDebtQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "h.W_DELETED = 0", "h.DKHACHHANGID IS NOT NULL" };
        var dp = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(k.NAME LIKE @s OR k.MAKHACH LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }
        var whereSql = string.Join(" AND ", where);

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"""
            SELECT COUNT(*) FROM (
                SELECT h.DKHACHHANGID
                FROM TDONHANG h JOIN DKHACHHANG k ON k.ID = h.DKHACHHANGID
                WHERE {whereSql} AND COALESCE(h.CONNO, 0) <> 0
                GROUP BY h.DKHACHHANGID
            ) t
            """, dp);
        var rows = (await conn.QueryAsync<CustomerDebtDto>(
            $"""
            SELECT h.DKHACHHANGID AS KhachHangId, k.MAKHACH AS MaKhach, k.NAME AS TenKhach,
                   k.DIENTHOAI AS DienThoai, SUM(COALESCE(h.CONNO,0)) AS CongNo,
                   SUM(CASE WHEN COALESCE(h.CONNO,0) <> 0 THEN 1 ELSE 0 END) AS SoHoaDon
            FROM TDONHANG h
            JOIN DKHACHHANG k ON k.ID = h.DKHACHHANGID
            WHERE {whereSql}
            GROUP BY h.DKHACHHANGID, k.MAKHACH, k.NAME, k.DIENTHOAI
            HAVING SUM(COALESCE(h.CONNO,0)) <> 0
            ORDER BY CongNo DESC
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }))).ToList();
        return (rows, total);
    }

    public async Task<(IReadOnlyList<SupplierDebtDto> Items, long Total)> SearchSupplierDebtsAsync(
        SupplierDebtQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "h.W_DELETED = 0", "h.DNHACUNGCAPID IS NOT NULL" };
        var dp = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(n.NAME LIKE @s OR n.MANHACUNGCAP LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }
        var whereSql = string.Join(" AND ", where);

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"""
            SELECT COUNT(*) FROM (
                SELECT h.DNHACUNGCAPID
                FROM TDONHANG h JOIN DNHACUNGCAP n ON n.ID = h.DNHACUNGCAPID
                WHERE {whereSql} AND COALESCE(h.CONLAI, 0) <> 0
                GROUP BY h.DNHACUNGCAPID
            ) t
            """, dp);
        var rows = (await conn.QueryAsync<SupplierDebtDto>(
            $"""
            SELECT h.DNHACUNGCAPID AS NhaCungCapId, n.MANHACUNGCAP AS MaNhaCungCap, n.NAME AS TenNhaCungCap,
                   n.DIENTHOAI AS DienThoai, SUM(COALESCE(h.CONLAI,0)) AS CongNo,
                   SUM(CASE WHEN COALESCE(h.CONLAI,0) <> 0 THEN 1 ELSE 0 END) AS SoPhieu
            FROM TDONHANG h
            JOIN DNHACUNGCAP n ON n.ID = h.DNHACUNGCAPID
            WHERE {whereSql}
            GROUP BY h.DNHACUNGCAPID, n.MANHACUNGCAP, n.NAME, n.DIENTHOAI
            HAVING SUM(COALESCE(h.CONLAI,0)) <> 0
            ORDER BY CongNo DESC
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }))).ToList();
        return (rows, total);
    }

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }
}