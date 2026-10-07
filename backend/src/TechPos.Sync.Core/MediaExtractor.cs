using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace TechPos.Sync.Core;

/// <summary>
/// Media/BLOB extractor (mục 8/32) — chạy 02:30 hằng ngày + lệnh MEDIA_EXTRACT.
/// Trích cột BLOB (FieldType=261, đã xác minh Phase 0) khỏi Firebird → file storage,
/// ghi metadata vào W_MEDIA (PATH/HASH/SIZE/MIME — không lưu BLOB trong MySQL).
/// Mirror vẫn giữ cột LONGBLOB nguyên trạng (không đụng parity); extractor chỉ PHỤ.
/// </summary>
public sealed record MediaExtractResult(
    string TableName, string ColumnName, int Extracted, int Skipped, int Errors, TimeSpan Duration);

public sealed class MediaExtractor(
    IFirebirdSyncSource firebird,
    IMySqlSyncTarget mysql,
    SyncMetaRepository meta,
    Serilog.ILogger logger)
{
    /// <summary>Cột BLOB cần trích — đã xác minh từ discovery (FieldType=261). Bổ sung khi cần.</summary>
    public static readonly (string Table, string Column)[] BlobColumns =
    [
        ("DMATHANG", "ANH"),            // ảnh mặt hàng (ưu tiên cao — web bán hàng)
        ("SIMAGE", "IMAGE"),            // kho ảnh dùng chung
        ("SCONFIGGROUP", "IMAGE"),      // ảnh nhóm cấu hình / menu
        ("SMENU", "IMAGE"),             // icon menu
        ("SREPORT", "SQL"),             // SQL báo cáo CŨ — rủi ro #6: phải trích trước khi mất
        ("SREPORT", "PARAMS"),
        ("SREPORT", "FILTERCONFIG"),
        ("SREPORTTEMPLATE", "TEMPLATE"),
        ("TBANGLUONG", "CHITIET"),      // JSON bảng lương (HR module Phase 5)
        ("SQUICKNOTE", "NOTEDATA"),
        ("SCONFIG", "BLOBVALUE"),
        ("SANALYSIS", "CONFIG"),
    ];

    private readonly string _root = Path.Combine(AppContext.BaseDirectory, "media-store");

    /// <summary>Trích toàn bộ cột BLOB; mỗi cột ghi 1 dòng W_SYNC_LOG RUN_TYPE='MEDIA'.</summary>
    public async Task<List<MediaExtractResult>> ExtractAllAsync(CancellationToken ct = default)
    {
        var results = new List<MediaExtractResult>();
        foreach (var (table, column) in BlobColumns)
        {
            try
            {
                results.Add(await ExtractColumnAsync(table, column, ct));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Media extract failed: {Table}.{Column}", table, column);
                results.Add(new MediaExtractResult(table, column, 0, 0, 1, TimeSpan.Zero));
            }
        }
        return results;
    }

    public async Task<MediaExtractResult> ExtractColumnAsync(string table, string column, CancellationToken ct = default)
    {
        table = table.Trim().ToUpperInvariant();
        column = column.Trim().ToUpperInvariant();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int extracted = 0, skipped = 0, errors = 0;

        Directory.CreateDirectory(_root);

        await using var fbConn = await firebird.OpenAsync(ct);
        await using var cmd = fbConn.CreateCommand();
        // Đọc ID + cột BLOB + TIMEMODIFIED (dùng làm mốc log).
        cmd.CommandText = $"SELECT \"ID\", \"{column}\", \"TIMEMODIFIED\" FROM \"{table}\" " +
                          $"WHERE \"{column}\" IS NOT NULL";
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        await using var myConn = await mysql.OpenAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var rowId = reader.IsDBNull(0) ? null : reader.GetString(0)?.Trim();
                if (string.IsNullOrEmpty(rowId)) { skipped++; continue; }

                var blob = ReadBlobBytes(reader, 1);
                if (blob is null || blob.Length == 0) { skipped++; continue; }

                var hash = Convert.ToHexString(SHA256.HashData(blob));
                var ext = DetectExtension(blob);
                var relPath = Path.Combine(table, column, rowId + ext);
                var fullPath = Path.Combine(_root, relPath);

                // Đã trích & hash khớp → bỏ qua (không ghi lại file).
                var existing = await GetExistingHashAsync(myConn, table, column, rowId, ct);
                if (existing == hash && File.Exists(fullPath)) { skipped++; continue; }

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllBytesAsync(fullPath, blob, ct);
                await UpsertMediaAsync(myConn, relPath, hash, blob.Length, DetectMime(blob), table, column, rowId, ct);
                extracted++;
            }
            catch (Exception ex)
            {
                errors++;
                logger.Warning(ex, "Media row error {Table}.{Column}", table, column);
            }
        }

        var result = new MediaExtractResult(table, column, extracted, skipped, errors, sw.Elapsed);
        await meta.LogRunAsync(
            new SyncRunResult($"{table}.{column}", extracted + skipped, extracted, skipped, errors, sw.Elapsed,
                $"media-store={_root}"),
            "MEDIA", ct);
        logger.Information("Media {Table}.{Column}: extracted={E} skipped={S} errors={X}",
            table, column, extracted, skipped, errors);
        return result;
    }

    // ---------- Helpers ----------

    private static async Task<string?> GetExistingHashAsync(
        MySqlConnector.MySqlConnection conn, string table, string column, string rowId, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT HASH FROM W_MEDIA WHERE REFERENCE_TYPE = @t AND COLUMN_NAME = @c AND REFERENCE_ID = @r";
        var p1 = cmd.CreateParameter(); p1.ParameterName = "@t"; p1.Value = table; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "@c"; p2.Value = column; cmd.Parameters.Add(p2);
        var p3 = cmd.CreateParameter(); p3.ParameterName = "@r"; p3.Value = rowId; cmd.Parameters.Add(p3);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result as string;
    }

    private static async Task UpsertMediaAsync(MySqlConnector.MySqlConnection conn, string relPath, string hash,
        long size, string mime, string table, string column, string rowId, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO W_MEDIA (ID, PATH_, HASH, SIZE_, MIME_TYPE, REFERENCE_TYPE, COLUMN_NAME, REFERENCE_ID, CREATED_AT)
            VALUES (@id, @path, @hash, @size, @mime, @t, @c, @r, @now)
            ON DUPLICATE KEY UPDATE PATH_ = VALUES(PATH_), HASH = VALUES(HASH),
              SIZE_ = VALUES(SIZE_), MIME_TYPE = VALUES(MIME_TYPE), CREATED_AT = VALUES(CREATED_AT)
            """;
        void P(string n, object v) { var p = cmd.CreateParameter(); p.ParameterName = n; p.Value = v; cmd.Parameters.Add(p); }
        P("@id", Guid.NewGuid().ToString());
        P("@path", relPath.Replace('\\', '/'));
        P("@hash", hash);
        P("@size", size);
        P("@mime", mime);
        P("@t", table);
        P("@c", column);
        P("@r", rowId);
        P("@now", DateTime.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Provider 2.5 trả byte[] cho BLOB SUB_TYPE 0/1 qua GetValue; fallback stream.</summary>
    private static byte[]? ReadBlobBytes(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal)) return null;
        var value = reader.GetValue(ordinal);
        switch (value)
        {
            case byte[] b:
                return b.Length == 0 ? null : b;
            case string s:
                return string.IsNullOrEmpty(s) ? null : Encoding.UTF8.GetBytes(s);
        }
        // Fallback: stream (FbBlob implements as stream in some versions)
        try
        {
            using var ms = new MemoryStream();
            using var stream = reader.GetStream(ordinal);
            stream.CopyTo(ms);
            return ms.Length == 0 ? null : ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static string DetectExtension(byte[] b) => DetectMime(b) switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "application/pdf" => ".pdf",
        "text/plain" => ".txt",
        _ => ".bin"
    };

    private static string DetectMime(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
        if (b.Length >= 3 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F') return "image/gif";
        if (b.Length >= 4 && b[0] == '%' && b[1] == 'P' && b[2] == 'D' && b[3] == 'F') return "application/pdf";
        // Text blob (SQL báo cáo, JSON config…) — không có byte 0 trong 64 byte đầu
        if (b.AsSpan(0, Math.Min(b.Length, 64)).IndexOf((byte)0) < 0) return "text/plain";
        return "application/octet-stream";
    }
}