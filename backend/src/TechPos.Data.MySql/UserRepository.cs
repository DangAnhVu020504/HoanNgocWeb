using Dapper;
using MySqlConnector;

namespace TechPos.Data.MySql;

/// <summary>
/// Quy ước dữ liệu: ID là GUID dạng text (VARCHAR/CHAR 36) — giữ tương thích Firebird (mục 5).
/// Vì vậy tầng data dùng string cho mọi …ID, không dùng Guid để tránh lỗi convert.
/// </summary>
public sealed record WUserRow(
    string Id, string Username, string PasswordHash, string? FullName, string? Email,
    bool IsActive, string? SuserId, bool MustChangePassword,
    int FailedLoginCount, DateTime? LockedUntil, DateTime? LastLoginAt);

/// <summary>Repository W_USER + W_REFRESH_TOKEN cho Auth (mục 34).</summary>
public sealed class UserRepository
{
    private readonly MySqlConnectionFactory _factory;
    public UserRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<WUserRow?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<WUserRow>(
            "SELECT ID AS Id, USERNAME AS Username, PASSWORD_HASH AS PasswordHash, FULL_NAME AS FullName, " +
            "EMAIL AS Email, IS_ACTIVE AS IsActive, SUSER_ID AS SuserId, MUST_CHANGE_PASSWORD AS MustChangePassword, " +
            "FAILED_LOGIN_COUNT AS FailedLoginCount, LOCKED_UNTIL AS LockedUntil, LAST_LOGIN_AT AS LastLoginAt " +
            "FROM W_USER WHERE USERNAME = @u LIMIT 1", new { u = username });
    }

    public async Task UpdateSuccessfulLoginAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE W_USER SET FAILED_LOGIN_COUNT = 0, LOCKED_UNTIL = NULL, LAST_LOGIN_AT = NOW(3) WHERE ID = @id",
            new { id = userId });
    }

    public async Task RegisterFailedLoginAsync(string userId, int failedCount, DateTime? lockedUntil, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE W_USER SET FAILED_LOGIN_COUNT = @fc, LOCKED_UNTIL = @lu WHERE ID = @id",
            new { id = userId, fc = failedCount, lu = lockedUntil });
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var roles = await conn.QueryAsync<string>(
            "SELECT r.CODE FROM W_USER_ROLE ur JOIN W_ROLE r ON r.ID = ur.ROLE_ID WHERE ur.USER_ID = @id",
            new { id = userId });
        return roles.ToList();
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var perms = await conn.QueryAsync<string>(
            "SELECT DISTINCT p.CODE FROM W_USER_ROLE ur " +
            "JOIN W_ROLE_PERMISSION rp ON rp.ROLE_ID = ur.ROLE_ID " +
            "JOIN W_PERMISSION p ON p.ID = rp.PERMISSION_ID " +
            "WHERE ur.USER_ID = @id", new { id = userId });
        return perms.ToList();
    }

    public async Task SaveRefreshTokenAsync(string userId, string tokenHash, DateTime expiresAt,
        string? deviceInfo = null, string? ip = null, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            "INSERT INTO W_REFRESH_TOKEN (ID, USER_ID, TOKEN_HASH, EXPIRES_AT, DEVICE_INFO, IP, CREATED_AT) " +
            "VALUES (@id, @uid, @h, @e, @d, @i, NOW(3))",
            new { id = Guid.NewGuid().ToString(), uid = userId, h = tokenHash, e = expiresAt, d = deviceInfo, i = ip });
    }

    public async Task<(string UserId, bool IsValid)?> ValidateRefreshTokenAsync(string tokenHash, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<RefreshTokenRow>(
            "SELECT CAST(USER_ID AS CHAR(36)) AS UserId, (REVOKED_AT IS NULL AND EXPIRES_AT > NOW(3)) AS IsValid " +
            "FROM W_REFRESH_TOKEN WHERE TOKEN_HASH = @h LIMIT 1", new { h = tokenHash });
        return row is null ? null : (row.UserId, row.IsValid);
    }

    public async Task RevokeRefreshTokenAsync(string tokenHash, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE W_REFRESH_TOKEN SET REVOKED_AT = NOW(3) WHERE TOKEN_HASH = @h AND REVOKED_AT IS NULL",
            new { h = tokenHash });
    }

    public async Task RevokeAllUserTokensAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE W_REFRESH_TOKEN SET REVOKED_AT = NOW(3) WHERE USER_ID = @id AND REVOKED_AT IS NULL",
            new { id = userId });
    }

    private sealed class RefreshTokenRow
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsValid { get; set; }
    }
}