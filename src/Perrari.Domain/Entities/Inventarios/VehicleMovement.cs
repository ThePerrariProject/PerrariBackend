using Perrari.Domain.Common;
using Perrari.Domain.Exceptions;

namespace Perrari.Domain.Entities.Inventarios;

/// <summary>Historial de ubicacion de un vehiculo. Solo se inserta, nunca se modifica.</summary>
public class VehicleMovement : BaseEntity
{
    private VehicleMovement() { }

    public Guid VehicleId { get; private set; }
    public Guid? OriginBranchId { get; private set; }
    public Guid DestinationBranchId { get; private set; }
    public string MovementType { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public DateTime MovedAt { get; private set; }

    public Branch? OriginBranch { get; private set; }
    public Branch DestinationBranch { get; private set; } = default!;

    /// <summary>Alta del vehiculo en una sucursal (sin origen).</summary>
    public static VehicleMovement Entry(Guid vehicleId, Guid branchId, Guid userId, DateTime now)
        => Build(vehicleId, null, branchId, MovementTypes.Entry, userId, now);

    public static VehicleMovement Transfer(Guid vehicleId, Guid originBranchId, Guid destinationBranchId, Guid userId, DateTime now)
    {
        if (originBranchId == destinationBranchId)
            throw new DomainException("La sucursal de destino debe ser distinta a la actual.");

        return Build(vehicleId, originBranchId, destinationBranchId, MovementTypes.Transfer, userId, now);
    }

    private static VehicleMovement Build(Guid vehicleId, Guid? origin, Guid destination, string type, Guid userId, DateTime now)
        => new()
        {
            VehicleId = vehicleId,
            OriginBranchId = origin,
            DestinationBranchId = destination,
            MovementType = type,
            UserId = userId,
            MovedAt = now
        };
}
