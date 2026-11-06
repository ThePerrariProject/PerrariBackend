using Perrari.Domain.Entities.Security;

namespace Perrari.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    Task RevokeAllActiveAsync(Guid userId, DateTime now, CancellationToken ct = default);
}
