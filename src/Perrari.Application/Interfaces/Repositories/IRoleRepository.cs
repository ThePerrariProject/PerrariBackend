using Perrari.Domain.Entities.Security;

namespace Perrari.Application.Interfaces.Repositories;

public interface IRoleRepository
{
    /// <summary>Todos los roles con sus permisos, ordenados por nombre.</summary>
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Un rol con sus permisos (rastreado, listo para modificar).</summary>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Los roles que existen entre los ids indicados (para validar una lista).</summary>
    Task<IReadOnlyList<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken ct = default);

    /// <summary>Los permisos que existen entre los ids indicados (para validar una lista).</summary>
    Task<IReadOnlyList<Permission>> GetPermissionsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}
