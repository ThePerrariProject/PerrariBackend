using Perrari.Domain.Common;
using Perrari.Domain.Exceptions;

namespace Perrari.Domain.Entities.Security;

public class Role : BaseEntity
{
    private readonly List<RolePermission> _rolePermissions = [];

    private Role() { }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions;

    public static Role Create(string name, string? description, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del rol es obligatorio");

        return new Role
        {
            Name = name.Trim().ToUpperInvariant(),
            Description = description,
            CreatedAt = now
        };
    }

    public void GrantPermission(Guid permissionId)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId))
            return;

        _rolePermissions.Add(new RolePermission(Id, permissionId));
    }
}