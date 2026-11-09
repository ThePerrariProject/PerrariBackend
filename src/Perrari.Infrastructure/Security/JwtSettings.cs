namespace Perrari.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = "Perrari.Api";
    public string Audience { get; init; } = "Perrari.Client";

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenHours { get; init; } = 8;

    public int TwoFactorTokenMinutes { get; init; } = 5;

    public string TwoFactorAudience => $"{Audience}.2fa";
}
