using TechPos.Shared;

namespace TechPos.Application.Notifications;

/// <summary>
/// Trừu tượng push realtime (SignalR) — Application không phụ thuộc ASP.NET Core.
/// Implementation nằm ở TechPos.Api (IHubContext&lt;NotificationHub&gt;).
/// </summary>
public interface INotificationBroadcaster
{
    Task BroadcastAsync(NotificationDto notification, CancellationToken ct = default);
}

/// <summary>Không push (dùng cho test hoặc khi tắt SignalR).</summary>
public sealed class NullNotificationBroadcaster : INotificationBroadcaster
{
    public Task BroadcastAsync(NotificationDto notification, CancellationToken ct = default)
        => Task.CompletedTask;
}
