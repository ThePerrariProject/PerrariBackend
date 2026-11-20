using Perrari.Domain.Entities.Inventarios;

namespace Perrari.Application.Interfaces.Repositories;

/// <summary>Vehiculo ya con el nombre de su sucursal y de quien lo aparto, listo para armar un DTO.</summary>
public record VehicleRow(Vehicle Vehicle, string BranchName, string? LockedByName);

public record MovementRow(VehicleMovement Movement, string? OriginBranchName, string DestinationBranchName, string? UserName);

public record VehicleSearchCriteria(string? Search, Guid? BranchId, string? Status, int Page, int PageSize);

public interface IVehicleRepository
{
    /// <summary>Entidad con seguimiento, para modificarla.</summary>
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lectura sin seguimiento, siempre fresca desde la base.</summary>
    Task<VehicleRow?> GetRowAsync(Guid id, CancellationToken ct = default);

    Task<bool> VinExistsAsync(string vin, CancellationToken ct = default);

    Task<(IReadOnlyList<VehicleRow> Items, int TotalCount)> SearchAsync(VehicleSearchCriteria criteria, CancellationToken ct = default);

    Task AddAsync(Vehicle vehicle, CancellationToken ct = default);

    Task AddMovementAsync(VehicleMovement movement, CancellationToken ct = default);

    Task<(IReadOnlyList<MovementRow> Items, int TotalCount)> GetMovementsAsync(Guid vehicleId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Aparta el vehiculo con un unico UPDATE condicional. Devuelve true solo si ESTA llamada lo logro:
    /// debe estar AVAILABLE y sin apartado vigente (o apartado por el mismo usuario, lo que renueva el plazo).
    /// </summary>
    Task<bool> TryLockAsync(Guid id, Guid userId, DateTime now, DateTime lockExpiredBefore, CancellationToken ct = default);

    /// <summary>Libera solo si el apartado es del usuario indicado.</summary>
    Task<bool> TryUnlockAsync(Guid id, Guid userId, DateTime now, CancellationToken ct = default);

    /// <summary>Libera el apartado sin importar de quien sea. Devuelve false si el vehiculo no existe.</summary>
    Task<bool> ForceUnlockAsync(Guid id, DateTime now, CancellationToken ct = default);
}
