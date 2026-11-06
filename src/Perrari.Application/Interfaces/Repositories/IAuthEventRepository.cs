using Perrari.Domain.Entities.Security;

namespace Perrari.Application.Interfaces.Repositories;

public interface IAuthEventRepository
{
    Task AddAsync(AuthEvent authEvent, CancellationToken ct = default);
}
