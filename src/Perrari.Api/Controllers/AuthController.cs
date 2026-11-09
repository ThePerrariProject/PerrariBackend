using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Perrari.Application.DTOs.Auth;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto, CancellationToken ct)
        => Ok(await auth.LoginAsync(dto, ct));

    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthSessionDto>> VerifyTwoFactor(VerifyTwoFactorDto dto, CancellationToken ct)
        => Ok(await auth.VerifyTwoFactorAsync(dto, ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthSessionDto>> Refresh(RefreshTokenDto dto, CancellationToken ct)
        => Ok(await auth.RefreshAsync(dto, ct));

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshTokenDto dto, CancellationToken ct)
    {
        await auth.LogoutAsync(dto, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
        => Ok(await auth.GetProfileAsync(ct));

    [HttpPost("2fa/setup")]
    [Authorize]
    public async Task<ActionResult<SetupTwoFactorResponseDto>> SetupTwoFactor(CancellationToken ct)
        => Ok(await auth.SetupTwoFactorAsync(ct));

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<ActionResult<RecoveryCodesResponseDto>> EnableTwoFacotr(EnableTwoFactorDto dto, CancellationToken ct)
        => Ok(await auth.EnableTwoFactorAsync(dto, ct));

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor(DisableTwoFactorDto dto, CancellationToken ct)
    {
        await auth.DisableTwoFactorAsync(dto, ct);
        return NoContent();
    }
}