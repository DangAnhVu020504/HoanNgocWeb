using System.Text.RegularExpressions;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Admin;

/// <summary>Use-case quản trị người dùng + vai trò/quyền. Mật khẩu hash BCrypt.</summary>
public sealed class AdminService(AdminRepository repo)
{
    private static readonly Regex UsernameRx = new("^[a-zA-Z0-9._-]{3,64}$", RegexOptions.Compiled);
    private static readonly Regex RoleCodeRx = new("^[A-Z0-9_]{2,32}$", RegexOptions.Compiled);
    private static readonly string[] SystemRoleCodes =
        ["SUPER_ADMIN", "ADMIN", "MANAGER", "SALES", "CASHIER", "WAREHOUSE", "ACCOUNTANT", "HR", "VIEWER"];

    public async Task<PageResult<AdminUserDto>> SearchUsersAsync(AdminUserQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchUsersAsync(q, ct);
        return new PageResult<AdminUserDto>
        {
            Items = items, Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize), Total = total
        };
    }

    public async Task<AdminUserDto> GetUserAsync(string id, CancellationToken ct = default)
        => await repo.GetUserAsync(id, ct) ?? throw new NotFoundException($"Không tìm thấy người dùng {id}");

    public async Task<AdminUserDto> CreateUserAsync(AdminUserCreateRequest req, CancellationToken ct = default)
    {
        var username = req.Username.Trim();
        if (!UsernameRx.IsMatch(username))
            throw new ValidationException("Tên đăng nhập chỉ gồm chữ, số, dấu chấm, gạch dưới (3-64 ký tự)",
                [new ApiError(nameof(req.Username), "USERNAME_INVALID", "Tên đăng nhập không hợp lệ")]);
        if (req.Password.Length < 6)
            throw new ValidationException("Mật khẩu phải có ít nhất 6 ký tự",
                [new ApiError(nameof(req.Password), "PASSWORD_TOO_SHORT", "Mật khẩu quá ngắn")]);
        if (await repo.UsernameExistsAsync(username, ct))
            throw new ConflictException($"Tên đăng nhập '{username}' đã tồn tại");

        await EnsureRolesExistAsync(req.Roles, ct);
        var id = await repo.CreateUserAsync(username, BCrypt.Net.BCrypt.HashPassword(req.Password),
            req.FullName, req.Email, req.Roles, ct);
        return await GetUserAsync(id, ct);
    }

    public async Task<AdminUserDto> UpdateUserAsync(string id, AdminUserUpdateRequest req, CancellationToken ct = default)
    {
        _ = await GetUserAsync(id, ct);
        await EnsureRolesExistAsync(req.Roles, ct);
        var rows = await repo.UpdateUserAsync(id, req, ct);
        if (rows == 0) throw new NotFoundException($"Không tìm thấy người dùng {id}");
        return await GetUserAsync(id, ct);
    }

    public async Task ResetPasswordAsync(string id, AdminUserResetPasswordRequest req, CancellationToken ct = default)
    {
        _ = await GetUserAsync(id, ct);
        if (req.NewPassword.Length < 6)
            throw new ValidationException("Mật khẩu phải có ít nhất 6 ký tự",
                [new ApiError(nameof(req.NewPassword), "PASSWORD_TOO_SHORT", "Mật khẩu quá ngắn")]);
        await repo.ResetPasswordAsync(id, BCrypt.Net.BCrypt.HashPassword(req.NewPassword), ct);
    }

    public async Task DeactivateUserAsync(string id, string currentUserId, CancellationToken ct = default)
    {
        if (id == currentUserId)
            throw new ForbiddenException("Không thể khóa tài khoản đang đăng nhập");
        _ = await GetUserAsync(id, ct);
        await repo.UpdateUserAsync(id, new AdminUserUpdateRequest(null, null, false, null), ct);
    }

    public Task<IReadOnlyList<AdminRoleDto>> GetRolesAsync(CancellationToken ct = default)
        => repo.GetRolesAsync(ct);

    public Task<IReadOnlyList<AdminPermissionDto>> GetPermissionsAsync(CancellationToken ct = default)
        => repo.GetPermissionsAsync(ct);

    public async Task<AdminRoleDto> SaveRoleAsync(AdminRoleSaveRequest req, CancellationToken ct = default)
    {
        var code = req.Code.Trim().ToUpperInvariant();
        if (!RoleCodeRx.IsMatch(code))
            throw new ValidationException("Mã vai trò chỉ gồm chữ in hoa, số, gạch dưới (2-32 ký tự)",
                [new ApiError(nameof(req.Code), "ROLE_CODE_INVALID", "Mã vai trò không hợp lệ")]);
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ValidationException("Tên vai trò bắt buộc",
                [new ApiError(nameof(req.Name), "ROLE_NAME_REQUIRED", "Tên vai trò bắt buộc")]);

        var known = (await repo.GetPermissionsAsync(ct)).Select(p => p.Code).ToHashSet(StringComparer.Ordinal);
        var unknown = req.Permissions.Where(p => !known.Contains(p)).ToList();
        if (unknown.Count > 0)
            throw new ValidationException($"Quyền không tồn tại: {string.Join(", ", unknown)}", []);

        var id = await repo.SaveRoleAsync(req with { Code = code }, ct);
        var all = await repo.GetRolesAsync(ct);
        return all.First(r => r.Id == id);
    }

    public async Task DeleteRoleAsync(string id, CancellationToken ct = default)
    {
        var role = (await repo.GetRolesAsync(ct)).FirstOrDefault(r => r.Id == id)
                   ?? throw new NotFoundException($"Không tìm thấy vai trò {id}");
        if (SystemRoleCodes.Contains(role.Code))
            throw new ForbiddenException($"Không thể xóa vai trò hệ thống {role.Code}");
        if (await repo.GetRoleUserCountAsync(id, ct) > 0)
            throw new ConflictException($"Vai trò {role.Code} đang được gán cho người dùng");
        await repo.DeleteRoleAsync(id, ct);
    }

    private async Task EnsureRolesExistAsync(IReadOnlyList<string>? roles, CancellationToken ct)
    {
        if (roles is null || roles.Count == 0) return;
        var known = (await repo.GetRolesAsync(ct)).Select(r => r.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = roles.Where(r => !known.Contains(r)).ToList();
        if (unknown.Count > 0)
            throw new ValidationException($"Vai trò không tồn tại: {string.Join(", ", unknown)}", []);
    }
}
