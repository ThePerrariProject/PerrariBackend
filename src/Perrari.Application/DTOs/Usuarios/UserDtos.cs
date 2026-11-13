namespace Perrari.Application.DTOs.Usuarios;

public record RoleSummaryDto(Guid Id, string Name);

public record UserListItemDto(
    Guid Id,
    string Username,
    string Email,
    bool IsActive,
    bool IsLocked,
    bool TwoFactorEnabled,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt);

public record UserDetailDto(
    Guid Id,
    string Username,
    string Email,
    bool IsActive,
    bool IsLocked,
    DateTime? LockedUntil,
    int FailedLoginCount,
    bool TwoFactorEnabled,
    IReadOnlyList<RoleSummaryDto> Roles,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateUserDto(string Username, string Email, string Password, IReadOnlyList<Guid> RoleIds);

public record UpdateUserDto(string Username, string Email);

public record SetUserRolesDto(IReadOnlyList<Guid> RoleIds);

public class UserListQueryDto
{
    public string? Search { get; set; }
    public Guid? RoleId { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}