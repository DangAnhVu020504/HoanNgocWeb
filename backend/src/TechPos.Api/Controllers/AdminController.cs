using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Admin;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Quản trị người dùng (W_USER) — CRUD, gán vai trò, reset mật khẩu.</summary>
[ApiController]
[Route("api/admin/users")]
[Authorize]
public sealed class AdminUsersController(AdminService admin, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("ADMIN.USER.MANAGE")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] bool? activeOnly,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(ApiResult<PageResult<AdminUserDto>>.Ok(
            await admin.SearchUsersAsync(new AdminUserQuery(search, activeOnly, page, pageSize), ct),
            HttpContext.TraceIdentifier));

    [HttpGet("{id}")]
    [RequirePermission("ADMIN.USER.MANAGE")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
        => Ok(ApiResult<AdminUserDto>.Ok(await admin.GetUserAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("ADMIN.USER.MANAGE")]
    public async Task<IActionResult> Create([FromBody] AdminUserCreateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await admin.CreateUserAsync(req, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "CREATE", "W_USER", result.Id,
            newValue: new { req.Username, req.FullName, req.Email, req.Roles },
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<AdminUserDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id}")]
    [RequirePermission("ADMIN.USER.MANAGE")]
    public async Task<IActionResult> Update(string id, [FromBody] AdminUserUpdateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await admin.UpdateUserAsync(id, req, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "UPDATE", "W_USER", id,
            newValue: req, ip: ip, userAgent: ua,
            correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<AdminUserDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id}/reset-password")]
    [RequirePermission("ADMIN.USER.MANAGE")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminUserResetPasswordRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        await admin.ResetPasswordAsync(id, req, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "RESET_PASSWORD", "W_USER", id,
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id}")]
    [RequirePermission("ADMIN.USER.MANAGE")]
    public async Task<IActionResult> Deactivate(string id, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        await admin.DeactivateUserAsync(id, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "DEACTIVATE", "W_USER", id,
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}

/// <summary>Quản trị vai trò/quyền.</summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public sealed class AdminRolesController(AdminService admin, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet("roles")]
    [RequirePermission("ADMIN.ROLE.MANAGE")]
    public async Task<IActionResult> Roles(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<AdminRoleDto>>.Ok(await admin.GetRolesAsync(ct),
            HttpContext.TraceIdentifier));

    [HttpGet("permissions")]
    [RequirePermission("ADMIN.ROLE.MANAGE")]
    public async Task<IActionResult> Permissions(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<AdminPermissionDto>>.Ok(await admin.GetPermissionsAsync(ct),
            HttpContext.TraceIdentifier));

    [HttpPost("roles")]
    [RequirePermission("ADMIN.ROLE.MANAGE")]
    public async Task<IActionResult> SaveRole([FromBody] AdminRoleSaveRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await admin.SaveRoleAsync(req, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "UPSERT", "W_ROLE", result.Id,
            newValue: req, ip: ip, userAgent: ua,
            correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<AdminRoleDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("roles/{id}")]
    [RequirePermission("ADMIN.ROLE.MANAGE")]
    public async Task<IActionResult> DeleteRole(string id, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        await admin.DeleteRoleAsync(id, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "DELETE", "W_ROLE", id,
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}
