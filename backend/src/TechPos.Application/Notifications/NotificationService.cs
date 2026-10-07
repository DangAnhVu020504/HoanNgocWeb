using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Notifications;

/// <summary>
/// Use-case Notification Center (Phase 7 — mục 25/47): list/unread/mark-read.
/// Tạo notification từ event hệ thống qua CreateAsync (fire-and-forget).
/// </summary>
public sealed class NotificationService(NotificationRepository repo)
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
}
