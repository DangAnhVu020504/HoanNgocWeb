using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Application.Auth;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Auth endpoints: login / refresh / me (mục 34).</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        if (result is null)
            return Ok(ApiResult<string>.Fail("Tên đăng nhập hoặc mật khẩu không đúng", HttpContext.TraceIdentifier));
        return Ok(ApiResult<TokenResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        // Phase 3 hoàn thiện: verify refresh token, rotate + revoke, issue mới.
        return StatusCode(501, ApiResult<string>.Fail("Chưa triển khai — Phase 3", HttpContext.TraceIdentifier));
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Unauthorized();
        return Ok(ApiResult<object>.Ok(new
        {
            name = User.Identity!.Name,
            roles = User.Claims.Where(c => c.Type.Contains("role")).Select(c => c.Value)
        }, HttpContext.TraceIdentifier));
    }
}

public sealed record RefreshRequest(string RefreshToken);