namespace Perrari.Application.DTOs.Inventarios;

public record BranchDto(Guid Id, string Name, string? Address, string? Phone, int VehicleCount, DateTime CreatedAt);

public record BranchInputDto(string Name, string? Address, string? Phone);