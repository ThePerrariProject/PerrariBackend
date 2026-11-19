namespace Perrari.Application.DTOs.Inventarios;

public record VehicleListItemDto(
    Guid Id,
    string Vin,
    string Brand,
    string Model,
    int Year,
    string? Color,
    decimal SalePrice,
    string Status,
    Guid BranchId,
    string BranchName,
    bool IsLocked,
    DateTime CreatedAt);

public record VehicleDetailDto(
    Guid Id,
    string Vin,
    string Brand,
    string Model,
    int Year,
    string? Color,
    decimal PurchasePrice,
    decimal SalePrice,
    decimal? RentDailyRate,
    string Status,
    Guid BranchId,
    string BranchName,
    bool IsLocked,
    DateTime? LockedAt,
    DateTime? LockedUntil,
    Guid? LockedByUserId,
    string? LockedByName,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateVehicleDto(
    string Vin, string Brand, string Model, int Year, string? Color,
    decimal PurchasePrice, decimal SalePrice, decimal? RentDailyRate, Guid BranchId);

public record UpdateVehicleDto(
    string Brand, string Model, int Year, string? Color, decimal SalePrice, decimal? RentDailyRate);

public record ChangeVehicleStatusDto(string Status);

public record TransferVehicleDto(Guid DestinationBranchId);

public record VehicleMovementDto(
    Guid Id,
    string MovementType,
    Guid? OriginBranchId,
    string? OriginBranchName,
    Guid DestinationBranchId,
    string DestinationBranchName,
    Guid UserId,
    string? UserName,
    DateTime MovedAt);

/// <summary>Parametros del listado (query string). Todos son opcionales.</summary>
public class VehicleListQueryDto
{
    /// <summary>Texto a buscar en VIN, marca o modelo.</summary>
    public string? Search { get; set; }
    public Guid? BranchId { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class MovementListQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}