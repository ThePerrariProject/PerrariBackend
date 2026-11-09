namespace Perrari.Application.DTOs.Auth;

public record VerifyTwoFactorDto(string TwoFactorToken, string Code);

public record SetupTwoFactorResponseDto(string Secret, string OtpAuthUri);

public record EnableTwoFactorDto(string Code);

public record RecoveryCodesResponseDto(IReadOnlyList<string> Codes);

public record DisableTwoFactorDto(string Password, string Code);