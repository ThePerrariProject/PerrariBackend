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

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await db.Users.AddAsync(user, ct);

    public Task<bool> EmailExistsAsync(string email, Guid? excludeUserId, CancellationToken ct = default)
        => db.Users.AnyAsync(u => u.Email == email && (excludeUserId == null || u.Id != excludeUserId), ct);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> SearchAsync(UserSearchCriteria c, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(c.Search))
        {
            // Se escapan % _ y \ para que el texto del usuario se busque tal cual y no como comodin
            var escaped = c.Search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{escaped}%";
            query = query.Where(u => EF.Functions.ILike(u.Username, pattern) || EF.Functions.ILike(u.Email, pattern));
        }

        if (c.RoleId is { } roleId)
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId));

        if (c.IsActive is { } isActive)
            query = query.Where(u => u.IsActive == isActive);

        var total = await query.CountAsync(ct);

        var items = await query
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Username).ThenBy(u => u.Id)
            .Skip((c.Page - 1) * c.PageSize)
            .Take(c.PageSize)
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<int> CountActiveInRoleAsync(string roleName, Guid? excludeUserId, CancellationToken ct = default)
        => db.Users.CountAsync(u =>
            u.IsActive
            && (excludeUserId == null || u.Id != excludeUserId)
            && u.UserRoles.Any(ur => ur.Role.Name == roleName), ct);
}
