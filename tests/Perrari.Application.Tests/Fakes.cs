using Perrari.Application.Interfaces.Repositories;
using Perrari.Application.Interfaces.Services;
using Perrari.Domain.Entities.Security;

namespace Perrari.Application.Tests;


internal static class Reflect
{
    public static void LoadRoles(User user, IEnumerable<Role> catalog)
    {
        foreach (var userRole in user.UserRoles)
            typeof(UserRole).GetProperty(nameof(UserRole.Role))!.SetValue(userRole, catalog.Single(r => r.Id == userRole.RoleId));
    }
}

internal sealed class FakeUserRepository(List<Role> roleCatalog) : IUserRepository
{
    public List<User> Store { get; } = [];

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(u => u.Email == email));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = Store.FirstOrDefault(u => u.Id == id);
        if (user is not null)
            Reflect.LoadRoles(user, roleCatalog); // EF lo hace al cargar con Include
        return Task.FromResult(user);
    }

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        Reflect.LoadRoles(user, roleCatalog);
        Store.Add(user);
        return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeUserId, CancellationToken ct = default)
        => Task.FromResult(Store.Any(u => u.Email == email && u.Id != excludeUserId));

    public Task<(IReadOnlyList<User> Items, int TotalCount)> SearchAsync(UserSearchCriteria criteria, CancellationToken ct = default)
        => Task.FromResult(((IReadOnlyList<User>)Store.ToList(), Store.Count));

    public Task<int> CountActiveInRoleAsync(string roleName, Guid? excludeUserId, CancellationToken ct = default)
        => Task.FromResult(Store.Count(u => u.IsActive && u.Id != excludeUserId && u.HasRole(roleName)));
}

internal sealed class FakeRoleRepository(List<Role> roles, List<Permission> permissions) : IRoleRepository
{
    public Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Role>)roles.ToList());

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(roles.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Role>)roles.Where(r => ids.Contains(r.Id)).ToList());

    public Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Permission>)permissions.ToList());

    public Task<IReadOnlyList<Permission>> GetPermissionsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Permission>)permissions.Where(p => ids.Contains(p.Id)).ToList());
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<Guid> RevokedForUsers { get; } = [];

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => Task.FromResult<RefreshToken?>(null);

    public Task AddAsync(RefreshToken token, CancellationToken ct = default) => Task.CompletedTask;

    public Task RevokeAllActiveAsync(Guid userId, DateTime now, CancellationToken ct = default)
    {
        RevokedForUsers.Add(userId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeAuthEventRepository : IAuthEventRepository
{
    public List<AuthEvent> Events { get; } = [];

    public Task AddAsync(AuthEvent authEvent, CancellationToken ct = default)
    {
        Events.Add(authEvent);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        Saves++;
        return Task.FromResult(1);
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;

    public bool Verify(string password, string hash) => hash == "hash:" + password;

    public void SimulateVerification(string password) { }
}

internal sealed class FakeCurrentUser : ICurrentUserService
{
    public Guid? UserId { get; set; }

    public string? IpAddress => "127.0.0.1";

    public string? UserAgent => "tests";
}
