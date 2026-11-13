using Perrari.Application.DTOs.Usuarios;

namespace Perrari.Application.Interfaces.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default);

    Task<RoleDto> GetRoleAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default);

    /// <summary>Reemplaza los permisos del rol. El rol ADMIN conserva siempre los permisos del modulo USERS.</summary>
    Task<RoleDto> SetPermissionsAsync(Guid id, SetRolePermissionsDto dto, CancellationToken ct = default);
}
