using Perrari.Domain.Entities.Clientes;

namespace Perrari.Application.Interfaces.Repositories;

public record ClientSearchCriteria(string? Search, bool? IsActive, int Page, int PageSize);

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Client?> GetByRfcAsync(string rfc, CancellationToken ct = default);
    Task<bool> RfcExistsAsync(string rfc, Guid? excludeClientId = null, CancellationToken ct = default);
    Task AddAsync(Client client, CancellationToken ct = default);
    Task<(IReadOnlyList<Client> Items, int TotalCount)> SearchAsync(ClientSearchCriteria criteria, CancellationToken ct = default);
}

