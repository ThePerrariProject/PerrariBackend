using System.Security;

namespace Perrari.Domain.Entities.Security;

public class RolePermission
{
    private RolePermission() { }

    internal RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    public Role Role { get; private set; } = default!;
    public Permission Permission { get; private set; } = default!;
}