using Microsoft.EntityFrameworkCore;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Domain.Entities.Clientes;

namespace Perrari.Infrastructure.Persistence.Repositories;

public class ClientRepository(PerrariDbContext db) : IClientRepository
{
    public Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Client?> GetByRfcAsync(string rfc, CancellationToken ct = default)
        => db.Clients.FirstOrDefaultAsync(c => c.Rfc == rfc, ct);

    public Task<bool> RfcExistsAsync(string rfc, Guid? excludeClientId = null, CancellationToken ct = default)
        => db.Clients.AnyAsync(c => c.Rfc == rfc && (excludeClientId == null || c.Id != excludeClientId), ct);

    public async Task AddAsync(Client client, CancellationToken ct = default)
        => await db.Clients.AddAsync(client, ct);

    public async Task<(IReadOnlyList<Client> Items, int TotalCount)> SearchAsync(ClientSearchCriteria criteria, CancellationToken ct = default)
    {
        var query = db.Clients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            // Escapar comodines de LIKE (\ % _) para busqueda literal
            var escaped = criteria.Search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{escaped}%";
            query = query.Where(c => EF.Functions.ILike(c.Name, pattern) || EF.Functions.ILike(c.Rfc, pattern));
        }

        if (criteria.IsActive is { } isActive)
            query = query.Where(c => c.IsActive == isActive);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name).ThenBy(c => c.Id)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}

