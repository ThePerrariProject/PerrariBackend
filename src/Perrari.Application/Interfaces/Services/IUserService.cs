using Perrari.Application.Common;
using Perrari.Application.DTOs.Usuarios;

namespace Perrari.Application.Interfaces.Services;

public interface IUserService
{
    Task<PagedResult<UserListItemDto>> ListAsync(UserListQueryDto query, CancellationToken ct = default);
    Task<UserDetailDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<UserDetailDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<UserDetailDto> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken ct = default);
    Task<UserDetailDto> SetRolesAsync(Guid id, SetUserRolesDto dto, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
    Task ReactivateAsync(Guid id, CancellationToken ct = default);
    Task UnlockAsync(Guid id, CancellationToken ct = default);
    Task ResetTwoFactorAsync(Guid id, CancellationToken ct = default);
}