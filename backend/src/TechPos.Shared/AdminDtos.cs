namespace TechPos.Shared;

// =====================================================================
// Phase 8 — Quản trị người dùng + phân quyền (W_USER / W_ROLE / W_PERMISSION).
// Mật khẩu hash BCrypt; reset mật khẩu thu hồi toàn bộ refresh token.
// =====================================================================

public sealed class AdminUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public DateTime? LastLoginAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed record AdminUserQuery(
    string? Search = null,
    bool? ActiveOnly = null,
    int Page = 1,
    int PageSize = 20);

public sealed record AdminUserCreateRequest(
    string Username,
    string Password,
    string? FullName,
    string? Email,
    IReadOnlyList<string>? Roles);

public sealed record AdminUserUpdateRequest(
    string? FullName,
    string? Email,
    bool? IsActive,
    IReadOnlyList<string>? Roles);

public sealed record AdminUserResetPasswordRequest(string NewPassword);

public sealed class AdminRoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<string> Permissions { get; set; } = [];
    public int UserCount { get; set; }
}

public sealed record AdminRoleSaveRequest(
    string Code,
    string Name,
    IReadOnlyList<string> Permissions);

public sealed class AdminPermissionDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
