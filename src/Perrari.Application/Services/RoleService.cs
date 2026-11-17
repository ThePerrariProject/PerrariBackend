using FluentValidation;
using FluentValidation.Results;
using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Usuarios;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Application.Interfaces.Services;
using Perrari.Domain.Common;
using Perrari.Domain.Entities.Security;

namespace Perrari.Application.Services;

public class RoleService(
    IRoleRepository roles,
    IUnitOfWork unitOfWork,
    IValidator<SetRolePermissionsDto> permissionsValidator) : IRoleService
{
    /// <summary>Modulo cuyos permisos nunca se le pueden quitar al rol ADMIN: sin ellos nadie podria volver a administrar el acceso.</summary>
    private const string ProtectedModule = "USERS";

    public async Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default)
        => (await roles.GetAllAsync(ct)).Select(ToDto).ToList();

    public async Task<RoleDto> GetRoleAsync(Guid id, CancellationToken ct = default)
        => ToDto(await GetAsync(id, ct));

    public async Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default)
        => (await roles.GetAllPermissionsAsync(ct)).Select(ToDto).ToList();

    public async Task<RoleDto> SetPermissionsAsync(Guid id, SetRolePermissionsDto dto, CancellationToken ct = default)
    {
        await permissionsValidator.ValidateAndThrowAsync(dto, ct);

        var role = await GetAsync(id, ct);
        var ids = dto.PermissionIds.Distinct().ToList();

        var found = await roles.GetPermissionsByIdsAsync(ids, ct);
        if (found.Count != ids.Count)
            throw new ValidationException(new[] { new ValidationFailure(nameof(dto.PermissionIds), "Uno o más permisos no existen.") });

        if (role.Name == RoleNames.Admin)
        {
            var required = (await roles.GetAllPermissionsAsync(ct))
                .Where(p => p.Module == ProtectedModule)
                .Select(p => p.Id);

            if (required.Any(requiredId => !ids.Contains(requiredId)))
                throw new ConflictException("El rol ADMIN debe conservar todos los permisos del módulo USERS.");
        }

        role.SetPermissions(ids);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDto(await GetAsync(id, ct));
    }

    private async Task<Role> GetAsync(Guid id, CancellationToken ct)
        => await roles.GetByIdAsync(id, ct) ?? throw new NotFoundException("Rol no encontrado.");

    private static RoleDto ToDto(Role role)
        => new(
            role.Id,
            role.Name,
            role.Description,
            role.RolePermissions.Select(rp => ToDto(rp.Permission)).OrderBy(p => p.Code).ToList());

    private static PermissionDto ToDto(Permission p)
        => new(p.Id, p.Module, p.Action, p.Code, p.Description);
}
