using Microsoft.AspNetCore.SignalR;
using TechPos.Application.Notifications;
using TechPos.Shared;

namespace TechPos.Api.Hubs;

/// <summary>Push notification realtime tới mọi client đang kết nối /hubs/notifications.</summary>
public sealed class SignalRNotificationBroadcaster(IHubContext<NotificationHub> hub) : INotificationBroadcaster
{
    public async Task BroadcastAsync(NotificationDto notification, CancellationToken ct = default)
    {
        try
        {
            await hub.Clients.All.SendAsync("notification", notification, ct);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "SignalR broadcast failed");
        }
    }
}
