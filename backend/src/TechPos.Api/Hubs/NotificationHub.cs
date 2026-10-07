using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TechPos.Api.Hubs;

/// <summary>
/// SignalR hub realtime (Phase 7 — EMAIL-ARCHITECTURE §5): server push "notification"
/// khi có event mới; client chỉ nhận (không cần method invoke).
/// JWT từ query string (?access_token=) vì WebSocket không gửi được Authorization header.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public override Task OnConnectedAsync()
    {
        Serilog.Log.Information("NotificationHub connected {ConnectionId} user={User}",
            Context.ConnectionId, Context.User?.Identity?.Name);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Serilog.Log.Information("NotificationHub disconnected {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
