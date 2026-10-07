using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository tồn kho + thẻ kho (mục 4.8) — đọc OTONKHO + W_KHOLEDGER từ mirror.
/// </summary>
public sealed class InventoryRepository
{
    private readonly MySqlConnectionFactory _factory;
    public InventoryRepository(MySqlConnectionFactory factory) => _factory = factory;

    /// <summary>Row pivot tồn theo kho (dùng nội bộ cho Tồn tất cả các kho).</summary>
    private sealed class StockCellRow
    {
        public string MatHangId { get; set; } = string.Empty;
        public string KhoId { get; set; } = string.Empty;
        public string? Kho { get; set; }
        public decimal Ton { get; set; }
    }

    public async Task<(IReadOnlyList<InventoryDto> Items, long Total)> SearchAsync(
        InventoryListQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "o.W_DELETED = 0", "m.W_DELETED = 0" };
        var dp = new DynamicParameters();

        if (q.NonZeroOnly == true) where.Add("o.TON <> 0");
        if (!string.IsNullOrWhiteSpace(q.KhoId)) { where.Add("o.DKHOHANGID = @kho"); dp.Add("@kho", q.KhoId); }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(m.NAME LIKE @s OR m.CODE LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }

        var whereSql = string.Join(" AND ", where);
        var sort = q.Sort?.Trim() switch
        {
            "CODE" => "m.CODE",
            "TON" => "o.TON",
            _ => "m.NAME"
        };

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"""
            SELECT COUNT(*) FROM OTONKHO o
            JOIN DMATHANG m ON m.ID = o.DMATHANGID
            WHERE {whereSql}
            """, dp);
        var rows = await conn.QueryAsync<InventoryDto>(
            $"""
            SELECT o.DMATHANGID AS MatHangId, m.CODE, m.NAME, u.NAME AS DonViTinh,
                   o.DKHOHANGID AS KhoId, k.NAME AS Kho, COALESCE(o.TON,0) AS Ton,
                   COALESCE(m.GIAVON,0) AS GiaVon, COALESCE(m.GIABAN,0) AS GiaBan,
                   COALESCE(o.TON,0) * COALESCE(m.GIAVON,0) AS GiaTriTon, m.STATUS
            FROM OTONKHO o
            JOIN DMATHANG m ON m.ID = o.DMATHANGID
            LEFT JOIN DKHOHANG k ON k.ID = o.DKHOHANGID
            LEFT JOIN DDONVITINH u ON u.ID = m.DDONVITINHID
            WHERE {whereSql}
            ORDER BY {sort} ASC
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }));

        return (rows.ToList(), total);
    }

    /// <summary>Thẻ kho: W_KHOLEDGER + tồn lũy kế tính chạy (opening + in − out).</summary>
    public async Task<IReadOnlyList<LedgerEntryDto>> GetLedgerAsync(LedgerQuery q, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);

        var dp = new DynamicParameters();
        dp.Add("@mh", q.MatHangId);
        var khoFilter = "";
        if (!string.IsNullOrWhiteSpace(q.KhoId)) { khoFilter = "AND DKhoHangId = @kho"; dp.Add("@kho", q.KhoId); }

        // Tồn đầu kỳ = tổng SL_NHAP − SL_XUAT trước FromDate (không có FromDate ⇒ opening = 0,
        // tránh double-count toàn bộ ledger vào dòng đầu tiên)
        var opening = 0m;
        if (q.FromDate.HasValue)
        {
            var dp0 = new DynamicParameters();
            dp0.Add("@mh", q.MatHangId);
            if (!string.IsNullOrWhiteSpace(q.KhoId)) dp0.Add("@kho", q.KhoId);
            dp0.Add("@from", q.FromDate.Value.Date);
            opening = await conn.ExecuteScalarAsync<decimal>(
                $"""
                SELECT COALESCE(SUM(SL_NHAP - SL_XUAT), 0) FROM W_KHOLEDGER
                WHERE DMathangId = @mh {khoFilter} AND ENTRY_DATE < @from
                """, dp0);
        }

        var dp2 = new DynamicParameters();
        dp2.Add("@mh", q.MatHangId);
        var khoFilter2 = "";
        if (!string.IsNullOrWhiteSpace(q.KhoId)) { khoFilter2 = "AND l.DKhoHangId = @kho"; dp2.Add("@kho", q.KhoId); }
        var dateFilter2 = "";
        if (q.FromDate.HasValue) { dateFilter2 += "AND l.ENTRY_DATE >= @from"; dp2.Add("@from", q.FromDate.Value.Date); }
        if (q.ToDate.HasValue) { dateFilter2 += "AND l.ENTRY_DATE < DATE_ADD(@to, INTERVAL 1 DAY)"; dp2.Add("@to", q.ToDate.Value.Date); }

        var rows = await conn.QueryAsync<LedgerEntryDto>(
            $"""
            SELECT l.ENTRY_DATE AS EntryDate, l.DOC_NUMBER AS SoPhieu, l.DOC_TYPE AS DocType,
                   l.DOC_TYPE AS DienGiai, l.SL_NHAP AS SlNhap, l.SL_XUAT AS SlXuat, l.GIA_VON AS GiaVon
            FROM W_KHOLEDGER l
            WHERE l.DMathangId = @mh {khoFilter2} {dateFilter2}
            ORDER BY l.ENTRY_DATE ASC, l.ENTRY_SEQ ASC, l.DOC_NUMBER ASC
            """, dp2);

        // Tính tồn lũy kế
        var list = rows.ToList();
        var running = opening;
        foreach (var r in list)
        {
            running += r.SlNhap - r.SlXuat;
            r.TonLuyKe = running;
        }
        return list;
    }

    /// <summary>Tồn tất cả các kho (mục 4.8): mỗi dòng 1 mặt hàng + tồn chi tiết theo từng kho.</summary>
    public async Task<(IReadOnlyList<AllWarehouseStockDto> Items, long Total)> SearchAllWarehousesAsync(
        AllWarehouseStockQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "o.W_DELETED = 0", "m.W_DELETED = 0" };
        var dp = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(m.NAME LIKE @s OR m.CODE LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }
        var having = q.NonZeroOnly == true ? "HAVING SUM(COALESCE(o.TON,0)) <> 0" : "";
        var whereSql = string.Join(" AND ", where);

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"""
            SELECT COUNT(*) FROM (
                SELECT o.DMATHANGID
                FROM OTONKHO o JOIN DMATHANG m ON m.ID = o.DMATHANGID
                WHERE {whereSql}
                GROUP BY o.DMATHANGID
                {having}
            ) t
            """, dp);

        var heads = (await conn.QueryAsync<AllWarehouseStockDto>(
            $"""
            SELECT o.DMATHANGID AS MatHangId, m.CODE AS Code, m.NAME AS Name,
                   u.NAME AS DonViTinh, SUM(COALESCE(o.TON,0)) AS TongTon,
                   COALESCE(m.GIAVON,0) AS GiaVon,
                   SUM(COALESCE(o.TON,0)) * COALESCE(m.GIAVON,0) AS GiaTriTon
            FROM OTONKHO o
            JOIN DMATHANG m ON m.ID = o.DMATHANGID
            LEFT JOIN DDONVITINH u ON u.ID = m.DDONVITINHID
            WHERE {whereSql}
            GROUP BY o.DMATHANGID, m.CODE, m.NAME, u.NAME, m.GIAVON
            {having}
            ORDER BY m.NAME
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }))).ToList();

        // Pivot tồn theo kho cho các mặt hàng của trang này
        var ids = heads.Select(h => h.MatHangId).ToList();
        if (ids.Count > 0)
        {
            var cells = (await conn.QueryAsync<StockCellRow>(
                """
                SELECT o.DMATHANGID AS MatHangId, o.DKHOHANGID AS KhoId, k.NAME AS Kho, COALESCE(o.TON,0) AS Ton
                FROM OTONKHO o LEFT JOIN DKHOHANG k ON k.ID = o.DKHOHANGID
                WHERE o.W_DELETED = 0 AND o.DMATHANGID IN @ids
                ORDER BY k.NAME
                """, new { ids })).ToList();
            foreach (var h in heads)
                h.Stocks = cells.Where(c => c.MatHangId == h.MatHangId)
                    .Select(c => new WarehouseStockCell { KhoId = c.KhoId, Kho = c.Kho, Ton = c.Ton })
                    .ToList();
        }
        return (heads, total);
    }

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }
}
