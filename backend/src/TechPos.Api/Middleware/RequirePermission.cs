using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TechPos.Api.Middleware;

/// <summary>
/// Filter kiểm tra quyền theo permission claim "perms" (chuỗi ';'-separated, xem JwtTokenService).
/// SUPER_ADMIN luôn đi qua. Dùng: [RequirePermission("SALES.PRODUCT.VIEW")].
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string permission) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (user.IsInRole("SUPER_ADMIN")) return;

        var perms = user.FindFirst("perms")?.Value ?? "";
        var has = perms.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(permission, StringComparer.OrdinalIgnoreCase);
        if (!has)
            context.Result = new ForbidResult();
    }
}
