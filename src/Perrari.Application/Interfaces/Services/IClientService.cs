using Perrari.Application.Common;
using Perrari.Application.DTOs.Clientes;

namespace Perrari.Application.Interfaces.Services;

public interface IClientService
{
    Task<PagedResult<ClientListItemDto>> ListAsync(ClientListQueryDto query, CancellationToken ct = default);
    Task<ClientDetailDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<ClientDetailDto> CreateAsync(CreateClientDto dto, CancellationToken ct = default);
    Task<ClientDetailDto> UpdateAsync(Guid id, UpdateClientDto dto, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
    Task ReactivateAsync(Guid id, CancellationToken ct = default);
}

