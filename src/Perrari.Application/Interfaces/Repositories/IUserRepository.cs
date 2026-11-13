using Perrari.Domain.Entities.Security;

namespace Perrari.Application.Interfaces.Repositories;

/// <summary>Filtros del listado de usuarios. Search se busca en username y email.</summary>
public record UserSearchCriteria(string? Search, Guid? RoleId, bool? IsActive, int Page, int PageSize);

public interface IUserRepository
{
    /// <summary>Incluye TOTP, codigos de recuperacion, roles y permisos.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Incluye TOTP, codigos de recuperacion, roles y permisos.</summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>True si ya existe un usuario con ese correo (ya normalizado). excludeUserId sirve al editar.</summary>
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId, CancellationToken ct = default);

    /// <summary>Pagina de usuarios con sus roles (sin permisos). Devuelve tambien el total sin paginar.</summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> SearchAsync(UserSearchCriteria criteria, CancellationToken ct = default);

    /// <summary>Cuantos usuarios activos tienen el rol indicado, sin contar a excludeUserId.</summary>
    Task<int> CountActiveInRoleAsync(string roleName, Guid? excludeUserId, CancellationToken ct = default);
}
