using Perrari.Domain.Common;
using Perrari.Domain.Exceptions;

namespace Perrari.Domain.Entities.Inventarios;

public class Vehicle : BaseEntity, IAuditable
{
    public const int VinLength = 17;
    public const int MinYear = 1950;

    private const string VinAlphabet = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";

    private Vehicle() { }

    public string Vin { get; private set; } = default!;
    public string Brand { get; private set; } = default!;
    public string Model { get; private set; } = default!;
    public int Year { get; private set; }
    public string? Color { get; private set; }
    public decimal PurchasePrice { get; private set; }
    public decimal SalePrice { get; private set; }
    public decimal? RentDailyRate { get; private set; }
    public string Status { get; private set; } = VehicleStatuses.Available;
    public Guid BranchId { get; private set; }
    public bool IsLocked { get; private set; }
    public DateTime? LockedAt { get; private set; }
    public Guid? LockedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Branch Branch { get; private set; } = default!;

    // --------Reglas---------

    public static string NormalizeVin(string? vin) => (vin ?? "").Trim().ToUpperInvariant();

    public static bool IsValidVin(string vin)
        => vin.Length == VinLength && vin.All(c => VinAlphabet.Contains(c));

    public bool IsLockActive(DateTime now, int lockMinutes)
        => IsLocked && LockedAt is { } at && at.AddMinutes(lockMinutes) > now;

    public bool IsLockedByOther(Guid userId, DateTime now, int lockMinutes)
        => IsLockActive(now, lockMinutes) && LockedByUserId != userId;

    // --------Comportamiento---------

    public static Vehicle Create(
        string vin, string brand, string model, int year, string? color,
        decimal purchasePrice, decimal salePrice, decimal? rentDailyRate, Guid branchId, DateTime now)
    {
        var normalized = NormalizeVin(vin);
        if (!IsValidVin(normalized))
            throw new DomainException("El VIN debe tener 17 caracteres (letras y números, sin I, O ni Q).");
        if (purchasePrice <= 0)
            throw new DomainException("El precio de compra debe ser mayor a cero.");
        if (branchId == Guid.Empty)
            throw new DomainException("La sucursal es obligatoria.");

        var vehicle = new Vehicle
        {
            Vin = normalized,
            PurchasePrice = purchasePrice,
            BranchId = branchId,
            Status = VehicleStatuses.Available,
            CreatedAt = now
        };
        vehicle.ApplyDetails(brand, model, year, color, salePrice, rentDailyRate, now);
        return vehicle;
    }

    public void UpdateDetails(string brand, string model, int year, string? color, decimal salePrice, decimal? rentDailyRate, DateTime now)
    {
        EnsureNotSold();
        ApplyDetails(brand, model, year, color, salePrice, rentDailyRate, now);
    }

    public void ChangeStatus(string newStatus, DateTime now)
    {
        EnsureNotSold();
        if (!VehicleStatuses.Manual.Contains(newStatus))
            throw new DomainException("El estado no es válido. Use AVAILABLE, IN_MAINTENANCE o DECOMMISSIONED.");

        Status = newStatus;
        // Un vehiculo que sale de AVAILABLE ya no se puede estar vendiendo
        if (newStatus != VehicleStatuses.Available)
            ReleaseLock();
        UpdatedAt = now;
    }

    /// <summary>Cambia la sucursal y devuelve el movimiento a guardar en la misma transaccion.</summary>
    public VehicleMovement TransferTo(Guid destinationBranchId, Guid userId, DateTime now)
    {
        if (Status is VehicleStatuses.Sold or VehicleStatuses.Decommissioned)
            throw new DomainException("No se puede trasladar un vehículo vendido o dado de baja.");

        var movement = VehicleMovement.Transfer(Id, BranchId, destinationBranchId, userId, now);
        BranchId = destinationBranchId;
        UpdatedAt = now;
        return movement;
    }

    /// <summary>Lo usara el modulo de Ventas al cerrar la venta. No hay endpoint para esto.</summary>
    public void MarkAsSold(DateTime now)
    {
        if (Status != VehicleStatuses.Available)
            throw new DomainException("Solo se puede vender un vehículo disponible.");

        Status = VehicleStatuses.Sold;
        ReleaseLock();
        UpdatedAt = now;
    }

    private void ApplyDetails(string brand, string model, int year, string? color, decimal salePrice, decimal? rentDailyRate, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(brand))
            throw new DomainException("La marca es obligatoria.");
        if (string.IsNullOrWhiteSpace(model))
            throw new DomainException("El modelo es obligatorio.");
        if (year < MinYear || year > now.Year + 1)
            throw new DomainException($"El año debe estar entre {MinYear} y {now.Year + 1}.");
        if (salePrice <= 0)
            throw new DomainException("El precio de venta debe ser mayor a cero.");
        if (rentDailyRate is <= 0)
            throw new DomainException("La tarifa de renta debe ser mayor a cero.");

        Brand = brand.Trim();
        Model = model.Trim();
        Year = year;
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        SalePrice = salePrice;
        RentDailyRate = rentDailyRate;
        UpdatedAt = now;
    }

    private void EnsureNotSold()
    {
        if (Status == VehicleStatuses.Sold)
            throw new DomainException("Un vehículo vendido no se puede modificar.");
    }

    private void ReleaseLock()
    {
        IsLocked = false;
        LockedAt = null;
        LockedByUserId = null;
    }
}
