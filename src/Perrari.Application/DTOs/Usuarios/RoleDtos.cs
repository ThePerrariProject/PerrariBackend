namespace Perrari.Application.DTOs.Usuarios;

public record PermissionDto(Guid Id, string Module, string Action, string Code, string? Description);

public record RoleDto(Guid Id, string Name, string? Description, IReadOnlyList<PermissionDto> Permissions);

public record SetRolePermissionsDto(IReadOnlyList<Guid> PermissionIds);