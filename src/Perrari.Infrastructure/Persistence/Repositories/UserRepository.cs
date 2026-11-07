using Microsoft.EntityFrameworkCore;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Domain.Entities.Security;

namespace Perrari.Infrastructure.Persistence.Repositories;

public class UserRepository(PerrariDbContext db) : IUserRepository
{
    private IQueryable<User> WithAuthData() => db.Users
        .Include(u => u.Totp)
        .Include(u => u.RecoveryCodes)
        .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
        .AsSplitQuery();

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => WithAuthData().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => WithAuthData().FirstOrDefaultAsync(u => u.Id == id, ct);
}
