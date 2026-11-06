using Perrari.Domain.Entities.Security;

namespace Perrari.Application.Interfaces.Services;

public interface IJwtTokenGenerator
{
    (String Token, DateTime ExpiresAt) GenerateAccessToken(User user);

    string GenerateTwoFactorToken(User user);

    Task<Guid?> ValidateTwoFactorTokenAsync(string token);
}