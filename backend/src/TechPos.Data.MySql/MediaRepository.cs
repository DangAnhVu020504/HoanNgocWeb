using Dapper;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>Đọc metadata thư viện media (W_MEDIA) — file vật lý do Sync MediaExtractor ghi.</summary>
public sealed class MediaRepository
{
    private readonly MySqlConnectionFactory _factory;
    public MediaRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<(IReadOnlyList<MediaItemDto> Items, long Total)> SearchAsync(
        MediaQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var where = "WHERE 1=1";
        var dp = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where += " AND (PATH_ LIKE @s OR REFERENCE_TYPE LIKE @s OR REFERENCE_ID LIKE @s)";
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(q.ReferenceType))
        {
            where += " AND REFERENCE_TYPE = @rt";
            dp.Add("@rt", q.ReferenceType.Trim().ToUpperInvariant());
        }
        if (!string.IsNullOrWhiteSpace(q.MimeType))
        {
            where += " AND MIME_TYPE LIKE @mt";
            dp.Add("@mt", $"{q.MimeType.Trim()}%");
        }

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM W_MEDIA {where}", dp);
        var rows = (await conn.QueryAsync<MediaItemDto>(
            $"""
            SELECT ID, PATH_ AS Path, HASH, SIZE_ AS Size, MIME_TYPE AS MimeType,
                   REFERENCE_TYPE AS ReferenceType, COLUMN_NAME AS ColumnName,
                   REFERENCE_ID AS ReferenceId, CREATED_AT AS CreatedAt
            FROM W_MEDIA {where}
            ORDER BY CREATED_AT DESC, PATH_ ASC
            LIMIT @lim OFFSET @off
            """, Merge(dp, new { lim = pageSize, off = (page - 1) * pageSize }))).ToList();

        foreach (var m in rows) m.Url = $"/api/media/{m.Id}/file";
        return (rows, total);
    }

    public async Task<MediaItemDto?> GetAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var item = await conn.QueryFirstOrDefaultAsync<MediaItemDto>(
            """
            SELECT ID, PATH_ AS Path, HASH, SIZE_ AS Size, MIME_TYPE AS MimeType,
                   REFERENCE_TYPE AS ReferenceType, COLUMN_NAME AS ColumnName,
                   REFERENCE_ID AS ReferenceId, CREATED_AT AS CreatedAt
            FROM W_MEDIA WHERE ID = @id
            """, new { id });
        if (item is not null) item.Url = $"/api/media/{item.Id}/file";
        return item;
    }

    public async Task<IReadOnlyList<string>> GetReferenceTypesAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var types = await conn.QueryAsync<string>(
            "SELECT DISTINCT REFERENCE_TYPE FROM W_MEDIA WHERE REFERENCE_TYPE IS NOT NULL ORDER BY REFERENCE_TYPE");
        return types.ToList();
    }

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }
}
