using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Notifications;

/// <summary>
/// Use-case Notification Center (Phase 7 — mục 25/47): list/unread/mark-read.
/// Tạo notification từ event hệ thống qua CreateAsync (fire-and-forget) + push SignalR.
/// </summary>
public sealed class NotificationService(NotificationRepository repo, INotificationBroadcaster broadcaster)
{
    public Task<(IReadOnlyList<NotificationDto> Items, long Total)> SearchAsync(
        NotificationListQuery q, string userId, CancellationToken ct = default)
        => repo.SearchAsync(q, userId, ct);

    public Task<long> UnreadCountAsync(string userId, CancellationToken ct = default)
        => repo.UnreadCountAsync(userId, ct);

    public Task MarkReadAsync(string id, string userId, CancellationToken ct = default)
        => repo.MarkReadAsync(id, userId, ct);

    public Task MarkAllReadAsync(string userId, CancellationToken ct = default)
        => repo.MarkAllReadAsync(userId, ct);

    /// <summary>Tạo thông báo + push realtime. Không ném exception ra caller.</summary>
    public async Task<string?> CreateAndBroadcastAsync(string group, string title, string? description,
        string? icon = null, string? actionUrl = null, string? referenceType = null,
        string? referenceId = null, CancellationToken ct = default)
    {
        var id = await repo.CreateAsync(group, title, description, icon, actionUrl,
            referenceType, referenceId, ct);
        if (id is null) return null;

        await broadcaster.BroadcastAsync(new NotificationDto
        {
            Id = id, Group = group, Icon = icon, Title = title, Description = description,
            ActionUrl = actionUrl, CreatedAt = DateTime.UtcNow, IsRead = false
        }, ct);
        return id;
    }
}
