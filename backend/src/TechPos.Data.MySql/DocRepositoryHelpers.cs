using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Hỗ trợ chung cho repository chứng từ Phase 4: đọc W_DOC_TYPE_MAP (quy tắc vàng 5 —
/// chặn ghi khi LOAI/MODE chưa xác minh), lấy số phiếu từ W_DOC_SEQUENCE (prefix+năm),
/// đọc tồn kho hiện tại để validate trước khi xuất.
/// </summary>
public static class DocRepositoryHelpers
{
    public sealed record DocTypeMap(int Loai, int Mode, string Prefix, byte Verified);

    private sealed class DocTypeMapRow
    {
        public int Loai { get; set; }
        public int? Mode { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public byte Verified { get; set; }
    }

    /// <summary>
    /// Đọc cấu hình LOAI/MODE/PREFIX theo DOC_TYPE; ném InvalidOperationException khi
    /// thiếu cấu hình hoặc chưa xác minh (VERIFIED=0) — giống PosSaleRepository/ReceiptRepository.
    /// </summary>
    public static async Task<DocTypeMap> GetDocTypeAsync(
        MySqlConnection conn, MySqlTransaction tx, string docType, CancellationToken ct = default)
    {
        var row = await conn.QueryFirstOrDefaultAsync<DocTypeMapRow>(
            "SELECT LOAI AS Loai, MODE_VAL AS Mode, PREFIX AS Prefix, VERIFIED AS Verified " +
            "FROM W_DOC_TYPE_MAP WHERE DOC_TYPE=@t LIMIT 1",
            new { t = docType }, tx);
        if (row is null || string.IsNullOrEmpty(row.Prefix))
            throw new ConflictException(
                $"W_DOC_TYPE_MAP chưa có cấu hình '{docType}' — không thể xác định LOAI/MODE (quy tắc vàng 5).");
        if (row.Verified == 0)
            throw new ConflictException(
                $"LOAI/MODE cho '{docType}' chưa được xác minh trên DB production (VERIFIED=0). " +
                "Hãy xác minh tập giá trị LOAI/MODE trên DB thật rồi UPDATE W_DOC_TYPE_MAP SET VERIFIED=1.");
        return new DocTypeMap(row.Loai, row.Mode ?? -1, row.Prefix, row.Verified);
    }

    /// <summary>Số phiếu kế tiếp theo prefix + năm (BL26/00001) — tăng trong transaction nên không race.</summary>
    public static async Task<string> NextSoPhieuAsync(
        MySqlConnection conn, MySqlTransaction tx, string prefix, CancellationToken ct = default)
    {
        var year = DateTime.Now.Year;
        await conn.ExecuteAsync(
            "INSERT INTO W_DOC_SEQUENCE (PREFIX_, YEAR_, LAST_SEQ) VALUES (@p, @y, 0) " +
            "ON DUPLICATE KEY UPDATE PREFIX_ = PREFIX_",
            new { p = prefix, y = year }, tx);
        var seq = await conn.ExecuteScalarAsync<long>(
            "UPDATE W_DOC_SEQUENCE SET LAST_SEQ = LAST_SEQ + 1 WHERE PREFIX_=@p AND YEAR_=@y; " +
            "SELECT LAST_SEQ FROM W_DOC_SEQUENCE WHERE PREFIX_=@p AND YEAR_=@y;",
            new { p = prefix, y = year }, tx);
        return $"{prefix}{year % 100:D2}/{seq:D5}";
    }

    private sealed class StockRow
    {
        public string MatHangId { get; set; } = string.Empty;
        public decimal Ton { get; set; }
    }

    /// <summary>Đọc tồn (OTONKHO) của các mặt hàng tại 1 kho — key = MatHangId.</summary>
    public static async Task<Dictionary<string, decimal>> GetStockAsync(
        MySqlConnection conn, MySqlTransaction tx, string khoId,
        IReadOnlyList<string> matHangIds, CancellationToken ct = default)
    {
        var ids = matHangIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        var rows = await conn.QueryAsync<StockRow>(
            "SELECT DMATHANGID AS MatHangId, COALESCE(TON,0) AS Ton FROM OTONKHO " +
            "WHERE DKHOHANGID = @kho AND W_DELETED = 0 AND DMATHANGID IN @ids",
            new { kho = khoId, ids }, tx);
        return rows.ToDictionary(r => r.MatHangId, r => r.Ton);
    }

    /// <summary>Kiểm tra đủ tồn cho mỗi dòng xuất — thiếu thì ném ValidationException (400).</summary>
    public static void EnsureStock(IReadOnlyDictionary<string, decimal> stocks, string matHangId, decimal soLuong)
    {
        stocks.TryGetValue(matHangId, out var ton);
        if (ton < soLuong)
            throw new ValidationException("Không đủ tồn kho để xuất", [
                new ApiError(matHangId, "TON_KHONG_DU",
                    $"Tồn hiện tại {ton:0.###} < số lượng xuất {soLuong:0.###}")
            ]);
    }
}
