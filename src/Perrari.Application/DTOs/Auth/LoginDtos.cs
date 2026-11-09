namespace Perrari.Application.DTOs.Auth;

public record LoginDto(string Email, string Password);

public record UserDto(Guid Id, string Username, string Email, IReadOnlyList<string> Roles, bool TwoFactorEnabled);

public record AuthSessionDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User
);

public record LoginResponseDto(bool RequiresTwoFactor, string? TwoFactorToken, AuthSessionDto? Session);

public record RefreshTokenDto(string RefreshToken);