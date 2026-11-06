using Perrari.Domain.Entities.Security;

namespace Perrari.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetEmailByAsync(string email, CancellationToken ct = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
}