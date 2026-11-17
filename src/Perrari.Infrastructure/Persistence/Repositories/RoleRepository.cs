using Microsoft.EntityFrameworkCore;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Domain.Entities.Security;

namespace Perrari.Infrastructure.Persistence.Repositories;

public class RoleRepository(PerrariDbContext db) : IRoleRepository
{
    private IQueryable<Role> WithPermissions() => db.Roles
        .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
        .AsSplitQuery();

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
        => await WithPermissions().OrderBy(r => r.Name).ToListAsync(ct);

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => WithPermissions().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => await db.Roles.Where(r => ids.Contains(r.Id)).ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken ct = default)
        => await db.Permissions.AsNoTracking().OrderBy(p => p.Module).ThenBy(p => p.Action).ToListAsync(ct);

    public async Task<IReadOnlyList<Permission>> GetPermissionsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => await db.Permissions.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync(ct);
}
