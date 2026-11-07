using Microsoft.EntityFrameworkCore;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Domain.Entities.Security;

namespace Perrari.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(PerrariDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        => await db.RefreshTokens.AddAsync(token, ct);

    public Task RevokeAllActiveAsync(Guid userId, DateTime now, CancellationToken ct = default)
        => db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
}
