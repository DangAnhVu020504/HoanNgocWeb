using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository Notification (Phase 7 — EMAIL-ARCHITECTURE §5):
/// W_NOTIFICATION + W_NOTIFICATION_READ. Không sửa nội dung — chỉ trạng thái read.
/// </summary>
public sealed class NotificationRepository
{
    private readonly MySqlConnectionFactory _factory;
    public NotificationRepository(MySqlConnectionFactory factory) => _factory = factory;

    /// <summary>Tạo notification — dùng cho System/Email/Sync events (fire-and-forget: catch ở tầng gọi).</summary>
    public async Task<string?> CreateAsync(string group, string title, string? description,
        string? icon = null, string? actionUrl = null, string? referenceType = null,
        string? referenceId = null, CancellationToken ct = default)
    {
        try
        {
            var id = Guid.NewGuid().ToString();
            await using var conn = await _factory.OpenAsync(ct);
            await conn.ExecuteAsync(
                """
                INSERT INTO W_NOTIFICATION (ID, GROUP_, ICON, TITLE, DESCRIPTION, ACTION_URL, REFERENCE_TYPE, REFERENCE_ID, CREATED_AT)
                VALUES (@id, @g, @ic, @t, @d, @u, @rt, @ri, @now)
                """,
                new { id, g = group, ic = icon, t = title, d = description, u = actionUrl, rt = referenceType, ri = referenceId, now = DateTime.UtcNow });
            return id;
        }
        catch (Exception)
        {
            return null;   // không phá nghiệp vụ (mục 23)
        }
    }

    public async Task<(IReadOnlyList<NotificationDto> Items, long Total)> SearchAsync(
        NotificationListQuery q, string userId, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var where = "WHERE 1=1";
        var dp = new DynamicParameters();
        dp.Add("@uid", userId);
        if (!string.IsNullOrWhiteSpace(q.Group) && !string.Equals(q.Group, "ALL", StringComparison.OrdinalIgnoreCase))
        { where += " AND n.GROUP_ = @g"; dp.Add("@g", q.Group.ToUpperInvariant()); }

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM W_NOTIFICATION n {where}", dp);
        var items = (await conn.QueryAsync<NotificationDto>(
            $"""
            SELECT n.ID, n.GROUP_ AS `Group`, n.ICON AS Icon, n.TITLE AS Title,
                   n.DESCRIPTION AS Description, n.ACTION_URL AS ActionUrl,
                   n.CREATED_AT AS CreatedAt,
                   (r.USER_ID IS NOT NULL) AS IsRead
            FROM W_NOTIFICATION n
            LEFT JOIN W_NOTIFICATION_READ r ON r.NOTIFICATION_ID = n.ID AND r.USER_ID = @uid
            {where}
            ORDER BY n.CREATED_AT DESC LIMIT @lim OFFSET @off
            """, Merge(dp, new { lim = pageSize, off = (page - 1) * pageSize }))).ToList();
        return (items, total);
    }

    public async Task<long> UnreadCountAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*) FROM W_NOTIFICATION n
            LEFT JOIN W_NOTIFICATION_READ r ON r.NOTIFICATION_ID = n.ID AND r.USER_ID = @uid
            WHERE r.NOTIFICATION_ID IS NULL
            """, new { uid = userId });
    }

    public async Task MarkReadAsync(string id, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT IGNORE INTO W_NOTIFICATION_READ (NOTIFICATION_ID, USER_ID, READ_AT)
            VALUES (@id, @uid, @now)
            """, new { id, uid = userId, now = DateTime.UtcNow });
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT IGNORE INTO W_NOTIFICATION_READ (NOTIFICATION_ID, USER_ID, READ_AT)
            SELECT n.ID, @uid, NOW(3) FROM W_NOTIFICATION n
            LEFT JOIN W_NOTIFICATION_READ r ON r.NOTIFICATION_ID = n.ID AND r.USER_ID = @uid
            WHERE r.NOTIFICATION_ID IS NULL
            """, new { uid = userId });
    }

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }
}
