using Perrari.Application.Common;
using Perrari.Application.DTOs.Inventarios;

namespace Perrari.Application.Interfaces.Services;

public interface IVehicleService
{
    Task<PagedResult<VehicleListItemDto>> ListAsync(VehicleListQueryDto query, CancellationToken ct = default);

    Task<VehicleDetailDto> GetAsync(Guid id, CancellationToken ct = default);

    Task<VehicleDetailDto> CreateAsync(CreateVehicleDto dto, CancellationToken ct = default);

    Task<VehicleDetailDto> UpdateAsync(Guid id, UpdateVehicleDto dto, CancellationToken ct = default);

    Task<VehicleDetailDto> ChangeStatusAsync(Guid id, ChangeVehicleStatusDto dto, CancellationToken ct = default);

    Task<VehicleDetailDto> TransferAsync(Guid id, TransferVehicleDto dto, CancellationToken ct = default);

    Task<PagedResult<VehicleMovementDto>> GetMovementsAsync(Guid id, MovementListQueryDto query, CancellationToken ct = default);

    /// <summary>Aparta el vehiculo para el usuario actual. 409 si otra persona lo tiene o no esta disponible.</summary>
    Task<VehicleDetailDto> LockAsync(Guid id, CancellationToken ct = default);

    /// <summary>Libera el apartado del usuario actual. Si ya no hay apartado vigente no hace nada.</summary>
    Task UnlockAsync(Guid id, CancellationToken ct = default);

    /// <summary>Libera el apartado de cualquier persona (supervision).</summary>
    Task ForceUnlockAsync(Guid id, CancellationToken ct = default);
}
