using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository tính lại giá vốn (mục 4.8) — đọc W_KHOLEDGER (chuỗi nhập/xuất từng mặt hàng
/// từ ngày tính) + DMATHANG.GIAVON làm fallback; ghi kết quả:
/// ① W_KHOLEDGER.GIA_VON cho mọi dòng xuất (web table — tự do ghi),
/// ② TDONHANGCHITIET.GIAVON chỉ cho dòng W_ORIGIN='WEB' (quy tắc vàng 1: không đụng desktop).
/// </summary>
public sealed class CostRecalcRepository
{
    private readonly MySqlConnectionFactory _factory;
    public CostRecalcRepository(MySqlConnectionFactory factory) => _factory = factory;

    private sealed class ProductRow
    {
        public string DMathangId { get; set; } = string.Empty;
        public decimal GiaVon { get; set; }
        public decimal OpeningQty { get; set; }
    }

    private sealed class LedgerRow
    {
        public string ID { get; set; } = string.Empty;
        public string DMathangId { get; set; } = string.Empty;
        public string? DOC_ID { get; set; }
        public decimal SL_NHAP { get; set; }
        public decimal SL_XUAT { get; set; }
        public decimal? GIA_VON { get; set; }
    }

    private sealed class OpeningRow
    {
        public string MatHangId { get; set; } = string.Empty;
        public decimal Qty { get; set; }
    }

    /// <summary>Dữ liệu 1 mặt hàng để tính: fallback giá vốn + tồn đầu kỳ + chuỗi movement.</summary>
    public sealed record ProductCostData(
        string MatHangId,
        decimal FallbackCost,
        decimal OpeningQty,
        List<CostMovement> Movements)
    {
        /// <summary>Map ledgerId → (DOC_ID) để ghi kết quả về TDONHANGCHITIET web.</summary>
        public Dictionary<string, string?> DocIds { get; } = [];
    }

    /// <summary>Tải dữ liệu tính giá vốn mọi mặt hàng có phát sinh từ TuNgay.</summary>
    public async Task<IReadOnlyList<ProductCostData>> LoadAsync(DateTime tuNgay, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);

        var products = (await conn.QueryAsync<ProductRow>(
            """
            SELECT DISTINCT l.DMathangId, COALESCE(m.GIAVON, 0) AS GiaVon
            FROM W_KHOLEDGER l
            JOIN DMATHANG m ON m.ID = l.DMathangId AND m.W_DELETED = 0
            WHERE l.ENTRY_DATE >= @tu
            """, new { tu = tuNgay.Date })).ToList();
        if (products.Count == 0) return [];
        var giaVonMap = products.ToDictionary(p => p.DMathangId, p => p.GiaVon);

        var openQty = (await conn.QueryAsync<OpeningRow>(
            """
            SELECT DMathangId AS MatHangId, COALESCE(SUM(SL_NHAP - SL_XUAT), 0) AS Qty
            FROM W_KHOLEDGER
            WHERE ENTRY_DATE < @tu
            GROUP BY DMathangId
            """, new { tu = tuNgay.Date }))
            .ToDictionary(r => r.MatHangId, r => r.Qty);

        var rows = (await conn.QueryAsync<LedgerRow>(
            """
            SELECT ID, DMathangId, DOC_ID, SL_NHAP, SL_XUAT, GIA_VON
            FROM W_KHOLEDGER
            WHERE ENTRY_DATE >= @tu
            ORDER BY DMathangId, ENTRY_DATE, ENTRY_SEQ, ID
            """, new { tu = tuNgay.Date }))
            .GroupBy(r => r.DMathangId);

        var result = new List<ProductCostData>();
        foreach (var g in rows)
        {
            var data = new ProductCostData(
                g.Key,
                giaVonMap.GetValueOrDefault(g.Key),
                openQty.GetValueOrDefault(g.Key),
                []);
            foreach (var r in g)
            {
                var giaNhap = r.GIA_VON ?? giaVonMap.GetValueOrDefault(g.Key);
                data.Movements.Add(new CostMovement(r.ID, r.SL_NHAP, r.SL_XUAT, giaNhap));
                data.DocIds[r.ID] = r.DOC_ID;
            }
            result.Add(data);
        }
        return result;
    }

    public sealed record OutboundCost(string LedgerId, string? DocId, string MatHangId, decimal Cost);

    /// <summary>Ghi kết quả trong 1 transaction: W_KHOLEDGER.GIA_VON (mọi dòng xuất)
    /// + TDONHANGCHITIET.GIAVON (chỉ dòng W_ORIGIN='WEB'). Trả về số dòng W_KHOLEDGER đã cập nhật.</summary>
    public async Task<int> SaveAsync(IReadOnlyList<OutboundCost> costs, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var updated = 0;
            foreach (var c in costs)
            {
                updated += await conn.ExecuteAsync(
                    "UPDATE W_KHOLEDGER SET GIA_VON = @cost WHERE ID = @id",
                    new { cost = c.Cost, id = c.LedgerId }, tx);

                if (!string.IsNullOrEmpty(c.DocId))
                    await conn.ExecuteAsync(
                        """
                        UPDATE TDONHANGCHITIET SET GIAVON = @cost, TIMEMODIFIED = NOW(3)
                        WHERE TDONHANGID = @doc AND DMATHANGID = @mh
                          AND W_ORIGIN = 'WEB' AND W_DELETED = 0
                        """,
                        new { cost = c.Cost, doc = c.DocId, mh = c.MatHangId }, tx);
            }
            await tx.CommitAsync(ct);
            return updated;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}