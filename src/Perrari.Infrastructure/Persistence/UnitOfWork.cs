using Perrari.Application.Interfaces.Repositories;

namespace Perrari.Infrastructure.Persistence;

public class UnitOfWork(PerrariDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
