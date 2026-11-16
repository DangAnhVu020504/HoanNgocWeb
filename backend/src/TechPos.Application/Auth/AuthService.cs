using FluentValidation;
using Microsoft.IdentityModel.Tokens;
using TechPos.Shared;

namespace TechPos.Application.Auth;

public sealed record LoginRequest(string Username, string Password);
public sealed record TokenResponse(string AccessToken, string RefreshToken, int ExpiresInMinutes, UserInfo User);
public sealed record UserInfo(string Id, string Username, string? FullName, string? Email, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}

public sealed class AuthService
{
    private readonly TechPos.Data.MySql.UserRepository _users;
    private readonly JwtTokenService _tokens;

    public AuthService(TechPos.Data.MySql.UserRepository users, JwtTokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(request.Username, ct);
        if (user is null || !user.IsActive) return null;

        if (user.LockedUntil is { } until && until > DateTime.UtcNow)
            throw new ForbiddenException($"Tài khoản bị khóa đến {until:HH:mm:ss}.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            var failed = user.FailedLoginCount + 1;
            var lockUntil = failed >= 5 ? (DateTime?)DateTime.UtcNow.AddMinutes(15) : null;
            await _users.RegisterFailedLoginAsync(user.Id, failed, lockUntil, ct);
            return null;
        }

        var roles = await _users.GetRolesAsync(user.Id, ct);
        var permissions = await _users.GetPermissionsAsync(user.Id, ct);

        var (access, refresh, expiresIn) = _tokens.Issue(user.Id, user.Username, roles, permissions);
        await _users.SaveRefreshTokenAsync(user.Id,
            HashToken(refresh), DateTime.UtcNow.AddDays(7), null, null, ct);
        await _users.UpdateSuccessfulLoginAsync(user.Id, ct);

        return new TokenResponse(access, refresh, expiresIn,
            new UserInfo(user.Id, user.Username, user.FullName, user.Email, roles, permissions));
    }

    /// <summary>
    /// Đổi refresh token lấy cặp token mới (rotate: thu hồi token cũ, cấp token mới 7 ngày).
    /// Refresh token hợp lệ ⇒ người dùng không phải đăng nhập lại trong 7 ngày cuối hoạt động.
    /// </summary>
    public async Task<TokenResponse?> RefreshAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;

        var hash = HashToken(refreshToken);
        var stored = await _users.ValidateRefreshTokenAsync(hash, ct);
        if (stored is null || !stored.Value.IsValid) return null;

        var user = await _users.GetUserByIdAsync(stored.Value.UserId, ct);
        if (user is null || !user.IsActive) return null;

        // Nạp lại roles/permissions mới nhất (có thể đổi sau khi phát token)
        var roles = await _users.GetRolesAsync(user.Id, ct);
        var permissions = await _users.GetPermissionsAsync(user.Id, ct);

        var (access, refresh, expiresIn) = _tokens.Issue(user.Id, user.Username, roles, permissions);
        await _users.RevokeRefreshTokenAsync(hash, ct);
        await _users.SaveRefreshTokenAsync(user.Id,
            HashToken(refresh), DateTime.UtcNow.AddDays(7), null, null, ct);

        return new TokenResponse(access, refresh, expiresIn,
            new UserInfo(user.Id, user.Username, user.FullName, user.Email, roles, permissions));
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}