using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TechPos.Shared;

namespace TechPos.Application.Auth;

/// <summary>Sinh JWT access token + refresh token (mục 34). Dùng chung Api + worker.</summary>
public sealed class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options) => _options = options;

    public (string AccessToken, string RefreshToken, int ExpiresInMinutes) Issue(
        string userId, string username, IReadOnlyList<string> roles, IReadOnlyList<string>? permissions = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));
        if (permissions is { Count: > 0 })
            claims.Add(new Claim("perms", string.Join(';', permissions)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer, audience: _options.Audience, claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes), signingCredentials: creds);

        var refresh = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            + Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        return (new JwtSecurityTokenHandler().WriteToken(token), refresh, _options.AccessTokenMinutes);
    }
}