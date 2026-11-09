using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Infrastructure.Security;

public class RefreshTokenGenerator(IOptions<JwtSettings> options) : IRefreshTokenGenerator
{
    public TimeSpan Lifetime => TimeSpan.FromHours(options.Value.RefreshTokenHours);

    public (string Token, string Hash) Generate()
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
        return (token, Hash(token));
    }

    // SHA-256 en hexadecimal (64 caracteres). Al ser un token aleatorio de alta entropia no necesita salteo
    public string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
