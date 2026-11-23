namespace Perrari.Application.DTOs.Clientes;

public record CreateClientDto(string Name, string Rfc, string Phone, string Email);

public record UpdateClientDto(string Name, string Rfc, string Phone, string Email);

public record ClientListItemDto(
    Guid Id,
    string Name,
    string Rfc,
    string Phone,
    string Email,
    bool IsActive,
    DateTime CreatedAt);

public record ClientDetailDto(
    Guid Id,
    string Name,
    string Rfc,
    string Phone,
    string Email,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class ClientListQueryDto
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

