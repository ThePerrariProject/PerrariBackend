using Perrari.Application.Interfaces.Repositories;
using Perrari.Domain.Entities.Security;

namespace Perrari.Infrastructure.Persistence.Repositories;

public class AuthEventRepository(PerrariDbContext db) : IAuthEventRepository
{
    public async Task AddAsync(AuthEvent authEvent, CancellationToken ct = default)
        => await db.AuthEvents.AddAsync(authEvent, ct);
}
