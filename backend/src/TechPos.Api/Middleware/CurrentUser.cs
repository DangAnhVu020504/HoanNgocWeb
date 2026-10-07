using System.Security.Claims;

namespace TechPos.Api.Middleware;

/// <summary>Helper lấy userId/username/IP từ HttpContext cho audit log.</summary>
public static class CurrentUser
{
    public static string GetUserId(HttpContext ctx)
        => ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
           ?? ctx.User.FindFirst("sub")?.Value ?? "";

    public static string GetUsername(HttpContext ctx)
        => ctx.User.FindFirst(ClaimTypes.Name)?.Value ?? "";

    public static (string UserId, string Username, string Ip, string UserAgent) Info(HttpContext ctx)
        => (GetUserId(ctx), GetUsername(ctx),
            ctx.Connection.RemoteIpAddress?.ToString(),
            ctx.Request.Headers.UserAgent.ToString());
}
