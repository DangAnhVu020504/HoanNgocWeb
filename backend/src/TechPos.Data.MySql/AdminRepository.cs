using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>Repository quản trị người dùng + vai trò/quyền (W_USER / W_ROLE / W_PERMISSION).</summary>
public sealed class AdminRepository
{
    private readonly MySqlConnectionFactory _factory;
    public AdminRepository(MySqlConnectionFactory factory) => _factory = factory;

    // ---------- Users ----------

    private sealed class UserRow
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? RoleCodes { get; set; }
    }

    public async Task<(IReadOnlyList<AdminUserDto> Items, long Total)> SearchUsersAsync(
        AdminUserQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var where = "WHERE 1=1";
        var dp = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where += " AND (u.USERNAME LIKE @s OR u.FULL_NAME LIKE @s OR u.EMAIL LIKE @s)";
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }
        if (q.ActiveOnly == true) where += " AND u.IS_ACTIVE = 1";

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>($"SELECT COUNT(*) FROM W_USER u {where}", dp);
        var rows = (await conn.QueryAsync<UserRow>(
            $"""
            SELECT u.ID, u.USERNAME AS Username, u.FULL_NAME AS FullName, u.EMAIL AS Email,
                   u.IS_ACTIVE AS IsActive, u.MUST_CHANGE_PASSWORD AS MustChangePassword,
                   u.LAST_LOGIN_AT AS LastLoginAt, u.TIMECREATED AS CreatedAt,
                   COALESCE(GROUP_CONCAT(r.CODE ORDER BY r.CODE SEPARATOR ','), '') AS RoleCodes
            FROM W_USER u
            LEFT JOIN W_USER_ROLE ur ON ur.USER_ID = u.ID
            LEFT JOIN W_ROLE r ON r.ID = ur.ROLE_ID
            {where}
            GROUP BY u.ID, u.USERNAME, u.FULL_NAME, u.EMAIL, u.IS_ACTIVE,
                     u.MUST_CHANGE_PASSWORD, u.LAST_LOGIN_AT, u.TIMECREATED
            ORDER BY u.USERNAME
            LIMIT @lim OFFSET @off
            """, Merge(dp, new { lim = pageSize, off = (page - 1) * pageSize }))).ToList();
        return (rows.Select(ToDto).ToList(), total);
    }

    public async Task<AdminUserDto?> GetUserAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<UserRow>(
            """
            SELECT u.ID, u.USERNAME AS Username, u.FULL_NAME AS FullName, u.EMAIL AS Email,
                   u.IS_ACTIVE AS IsActive, u.MUST_CHANGE_PASSWORD AS MustChangePassword,
                   u.LAST_LOGIN_AT AS LastLoginAt, u.TIMECREATED AS CreatedAt,
                   COALESCE(GROUP_CONCAT(r.CODE ORDER BY r.CODE SEPARATOR ','), '') AS RoleCodes
            FROM W_USER u
            LEFT JOIN W_USER_ROLE ur ON ur.USER_ID = u.ID
            LEFT JOIN W_ROLE r ON r.ID = ur.ROLE_ID
            WHERE u.ID = @id
            GROUP BY u.ID, u.USERNAME, u.FULL_NAME, u.EMAIL, u.IS_ACTIVE,
                     u.MUST_CHANGE_PASSWORD, u.LAST_LOGIN_AT, u.TIMECREATED
            """, new { id });
        return row is null ? null : ToDto(row);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM W_USER WHERE USERNAME = @u", new { u = username }) > 0;
    }

    public async Task<string> CreateUserAsync(string username, string passwordHash,
        string? fullName, string? email, IReadOnlyList<string>? roles, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO W_USER
            (ID, USERNAME, PASSWORD_HASH, FULL_NAME, EMAIL, IS_ACTIVE, MUST_CHANGE_PASSWORD, TIMECREATED)
            VALUES (@id, @u, @hash, @fn, @em, 1, 0, @now)
            """,
            new { id, u = username, hash = passwordHash, fn = fullName, em = email, now = DateTime.UtcNow }, tx);
        await AssignRolesAsync(conn, tx, id, roles, ct);
        await tx.CommitAsync(ct);
        return id;
    }

    public async Task<int> UpdateUserAsync(string id, AdminUserUpdateRequest req, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        var rows = await conn.ExecuteAsync(
            """
            UPDATE W_USER SET
              FULL_NAME = COALESCE(@fn, FULL_NAME),
              EMAIL = COALESCE(@em, EMAIL),
              IS_ACTIVE = COALESCE(@active, IS_ACTIVE),
              TIMEMODIFIED = @now
            WHERE ID = @id
            """,
            new
            {
                id, fn = req.FullName, em = req.Email,
                active = req.IsActive.HasValue ? (object?)(req.IsActive.Value ? 1 : 0) : null,
                now = DateTime.UtcNow
            }, tx);

        if (req.Roles is not null) await AssignRolesAsync(conn, tx, id, req.Roles, ct);
        if (req.IsActive == false)
        {
            await conn.ExecuteAsync(
                "UPDATE W_REFRESH_TOKEN SET REVOKED_AT = NOW(3) WHERE USER_ID = @id AND REVOKED_AT IS NULL",
                new { id }, tx);
        }
        await tx.CommitAsync(ct);
        return rows;
    }

    public async Task ResetPasswordAsync(string id, string passwordHash, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE W_USER SET PASSWORD_HASH = @hash, MUST_CHANGE_PASSWORD = 0,
                FAILED_LOGIN_COUNT = 0, LOCKED_UNTIL = NULL, TIMEMODIFIED = @now
            WHERE ID = @id
            """, new { id, hash = passwordHash, now = DateTime.UtcNow }, tx);
        await conn.ExecuteAsync(
            "UPDATE W_REFRESH_TOKEN SET REVOKED_AT = NOW(3) WHERE USER_ID = @id AND REVOKED_AT IS NULL",
            new { id }, tx);
        await tx.CommitAsync(ct);
    }

    private static async Task AssignRolesAsync(MySqlConnection conn, MySqlTransaction tx,
        string userId, IReadOnlyList<string>? roles, CancellationToken ct)
    {
        if (roles is null) return;
        await conn.ExecuteAsync("DELETE FROM W_USER_ROLE WHERE USER_ID = @uid", new { uid = userId }, tx);
        var codes = roles.Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim().ToUpperInvariant()).Distinct().ToArray();
        if (codes.Length == 0) return;
        await conn.ExecuteAsync(
            "INSERT IGNORE INTO W_USER_ROLE (USER_ID, ROLE_ID) SELECT @uid, ID FROM W_ROLE WHERE CODE IN @codes",
            new { uid = userId, codes }, tx);
    }

    private static AdminUserDto ToDto(UserRow r) => new()
    {
        Id = r.Id, Username = r.Username, FullName = r.FullName, Email = r.Email,
        IsActive = r.IsActive, MustChangePassword = r.MustChangePassword,
        LastLoginAt = r.LastLoginAt, CreatedAt = r.CreatedAt,
        Roles = string.IsNullOrEmpty(r.RoleCodes)
            ? []
            : r.RoleCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    };

    // ---------- Roles / permissions ----------

    private sealed class RoleRow
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PermissionCodes { get; set; }
        public int UserCount { get; set; }
    }

    public async Task<IReadOnlyList<AdminRoleDto>> GetRolesAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<RoleRow>(
            """
            SELECT r.ID, r.CODE AS Code, r.NAME AS Name,
                   COALESCE(GROUP_CONCAT(DISTINCT p.CODE ORDER BY p.CODE SEPARATOR ','), '') AS PermissionCodes,
                   (SELECT COUNT(*) FROM W_USER_ROLE ur WHERE ur.ROLE_ID = r.ID) AS UserCount
            FROM W_ROLE r
            LEFT JOIN W_ROLE_PERMISSION rp ON rp.ROLE_ID = r.ID
            LEFT JOIN W_PERMISSION p ON p.ID = rp.PERMISSION_ID
            GROUP BY r.ID, r.CODE, r.NAME
            ORDER BY r.CODE
            """);
        return rows.Select(r => new AdminRoleDto
        {
            Id = r.Id, Code = r.Code, Name = r.Name, UserCount = r.UserCount,
            Permissions = string.IsNullOrEmpty(r.PermissionCodes)
                ? []
                : r.PermissionCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminPermissionDto>> GetPermissionsAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<AdminPermissionDto>(
            "SELECT ID AS Id, CODE AS Code, NAME AS Name FROM W_PERMISSION ORDER BY CODE")).ToList();
    }

    public async Task<string> SaveRoleAsync(AdminRoleSaveRequest req, CancellationToken ct = default)
    {
        var code = req.Code.Trim().ToUpperInvariant();
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var id = await conn.ExecuteScalarAsync<string?>(
            "SELECT ID FROM W_ROLE WHERE CODE = @c", new { c = code }, tx);
        if (id is null)
        {
            id = Guid.NewGuid().ToString();
            await conn.ExecuteAsync(
                "INSERT INTO W_ROLE (ID, CODE, NAME) VALUES (@id, @c, @n)",
                new { id, c = code, n = req.Name }, tx);
        }
        else
        {
            await conn.ExecuteAsync(
                "UPDATE W_ROLE SET NAME = @n WHERE ID = @id", new { id, n = req.Name }, tx);
        }

        await conn.ExecuteAsync("DELETE FROM W_ROLE_PERMISSION WHERE ROLE_ID = @id", new { id }, tx);
        var perms = req.Permissions.Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim()).Distinct().ToArray();
        if (perms.Length > 0)
        {
            await conn.ExecuteAsync(
                """
                INSERT IGNORE INTO W_ROLE_PERMISSION (ROLE_ID, PERMISSION_ID)
                SELECT @id, ID FROM W_PERMISSION WHERE CODE IN @perms
                """, new { id, perms }, tx);
        }
        await tx.CommitAsync(ct);
        return id;
    }

    public async Task<int> GetRoleUserCountAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM W_USER_ROLE WHERE ROLE_ID = @id", new { id });
    }

    public async Task DeleteRoleAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        await conn.ExecuteAsync("DELETE FROM W_ROLE_PERMISSION WHERE ROLE_ID = @id", new { id }, tx);
        await conn.ExecuteAsync("DELETE FROM W_USER_ROLE WHERE ROLE_ID = @id", new { id }, tx);
        await conn.ExecuteAsync("DELETE FROM W_ROLE WHERE ID = @id", new { id }, tx);
        await tx.CommitAsync(ct);
    }

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }
}
