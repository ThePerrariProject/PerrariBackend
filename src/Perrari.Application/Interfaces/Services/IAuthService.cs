using Perrari.Application.DTOs.Auth;

namespace Perrari.Application.Interfaces.Services;

public interface IAuthService
{
    /// <summary>Paso 1: correo y contrasena. Con 2FA activo devuelve un token temporal en lugar de la sesion.</summary>
    Task<LoginResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);

    /// <summary>Paso 2: codigo TOTP o codigo de recuperacion. Devuelve la sesion.</summary>
    Task<AuthSessionDto> VerifyTwoFactorAsync(VerifyTwoFactorDto dto, CancellationToken ct = default);

    /// <summary>Cambia un refresh token por una sesion nueva (el anterior queda revocado).</summary>
    Task<AuthSessionDto> RefreshAsync(RefreshTokenDto dto, CancellationToken ct = default);

    /// <summary>Revoca el refresh token. No falla si ya no existe.</summary>
    Task LogoutAsync(RefreshTokenDto dto, CancellationToken ct = default);

    Task<UserDto> GetProfileAsync(CancellationToken ct = default);

    /// <summary>Genera el secreto y la URI para el QR. El 2FA todavia no queda activo.</summary>
    Task<SetupTwoFactorResponseDto> SetupTwoFactorAsync(CancellationToken ct = default);

    /// <summary>Confirma con un codigo valido, activa el 2FA y devuelve los codigos de recuperacion (solo se muestran esta vez).</summary>
    Task<RecoveryCodesResponseDto> EnableTwoFactorAsync(EnableTwoFactorDto dto, CancellationToken ct = default);

    Task DisableTwoFactorAsync(DisableTwoFactorDto dto, CancellationToken ct = default);
}
