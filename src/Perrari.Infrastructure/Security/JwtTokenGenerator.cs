using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Perrari.Application.Common;
using Perrari.Application.Interfaces.Services;
using Perrari.Domain.Entities.Security;

namespace Perrari.Infrastructure.Security;

public class JwtTokenGenerator(IOptions<JwtSettings> options, TimeProvider timeProvider) : IJwtTokenGenerator
{
    private const string ClaimPurpose = "purpose";
    private const string PurposeTwoFactor = "2fa";

    private readonly JwtSettings _settings = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(user.GetRoleNames().Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(user.GetPermissionCodes().Select(p => new Claim(AuthClaims.Permission, p)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(Key(), SecurityAlgorithms.HmacSha256)
        };

        return (_handler.CreateToken(descriptor), expires);
    }

    public string GenerateTwoFactorToken(User user)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimPurpose, PurposeTwoFactor)
            }),
            Issuer = _settings.Issuer,
            Audience = _settings.TwoFactorAudience,
            NotBefore = now,
            Expires = now.AddMinutes(_settings.TwoFactorTokenMinutes),
            SigningCredentials = new SigningCredentials(Key(), SecurityAlgorithms.HmacSha256)
        };

        return _handler.CreateToken(descriptor);
    }

    public async Task<Guid?> ValidateTwoFactorTokenAsync(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.TwoFactorAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = Key(),
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        var result = await _handler.ValidateTokenAsync(token, parameters);
        if (!result.IsValid)
            return null;

        if (result.ClaimsIdentity.FindFirst(ClaimPurpose)?.Value != PurposeTwoFactor)
            return null;

        var sub = result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private SymmetricSecurityKey Key() => new(Encoding.UTF8.GetBytes(_settings.Key));
}
