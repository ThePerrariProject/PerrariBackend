using System.ComponentModel.Design;
using FluentValidation;
using FluentValidation.Results;
using Perrari.Application.Common;
using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Usuarios;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Application.Interfaces.Services;
using Perrari.Domain.Common;
using Perrari.Domain.Entities.Security;

namespace Perrari.Application.Services;

public class UserService(
    IUserRepository users,
    IRoleRepository roles,
    IRefreshTokenRepository refreshTokens,
    IAuthEventRepository authEvents,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    IValidator<CreateUserDto> createValidator,
    IValidator<UpdateUserDto> updateValidator,
    IValidator<SetUserRolesDto> rolesValidator,
    IValidator<UserListQueryDto> listValidator) : IUserService
{
    private const string MsgNotFound = "Usuario no encontrado";

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    #region Consultas

    public async Task<PagedResult<UserListItemDto>> ListAsync(UserListQueryDto query, CancellationToken ct = default)
    {
        await listValidator.ValidateAndThrowAsync(query, ct);

        var now = Now;
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var (items, total) = await users.SearchAsync(
            new UserSearchCriteria(search, query.RoleId, query.IsActive, query.Page, query.PageSize), ct);

        var rows = items
            .Select(u => new UserListItemDto(
                u.Id, u.Username, u.Email, u.IsActive, u.IsLocked(now), u.TwoFactorEnabled, u.GetRoleNames(), u.CreatedAt))
            .ToList();

        return new PagedResult<UserListItemDto>(rows, query.Page, query.PageSize, total);
    }

    public async Task<UserDetailDto> GetAsync(Guid id, CancellationToken ct = default)
        => ToDetail(await GetUserAsync(id, ct), Now);

    #endregion

    #region Crear y Editar

    public async Task<UserDetailDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await users.EmailExistsAsync(email, null, ct))
            throw new ConflictException("Ya existe un usuario con ese correo");

        var validRoles = await ValidateRolesAsync(dto.RoleIds, nameof(dto.RoleIds), ct);

        var user = User.Create(dto.Username, email, passwordHasher.Hash(dto.Password), now);
        foreach (var role in validRoles)
            user.AssignRole(role.Id);

        await users.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDetail(await GetUserAsync(user.Id, ct), now);
    }

    public async Task<UserDetailDto> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var user = await GetUserAsync(id, ct);
        var email = dto.Email.Trim().ToLowerInvariant();

        if (email != user.Email && await users.EmailExistsAsync(email, user.Id, ct))
            throw new ConflictException("Ya existe un usuario con ese correo.");

        user.UpdateProfile(dto.Username, email, now);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDetail(user, now);
    }

    #endregion

    #region  Roles

    public async Task<UserDetailDto> SetRolesAsync(Guid id, SetUserRolesDto dto, CancellationToken ct = default)
    {
        await rolesValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var user = await GetUserAsync(id, ct);
        var newRoles = await ValidateRolesAsync(dto.RoleIds, nameof(dto.RoleIds), ct);
        var keepsAdmin = newRoles.Any(r => r.Name == RoleNames.Admin);

        if (user.HasRole(RoleNames.Admin) && !keepsAdmin)
        {
            if (user.Id == currentUser.UserId)
                throw new ConflictException("No puede quitarse a sí mismo el rol de administrador.");

            await EnsureAnotherActiveAdminAsync(user, ct);
        }

        user.SetRoles(newRoles.Select(r => r.Id), now);
        await unitOfWork.SaveChangesAsync(ct);

        // El siguiente access token se emite al renovar la sesion: con los refresh tokens cerrados, trae los permisos nuevos
        await refreshTokens.RevokeAllActiveAsync(user.Id, now, ct);

        return ToDetail(await GetUserAsync(user.Id, ct), now);
    }

    #endregion

    #region Estado de la Cuenta

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var now = Now;
        var user = await GetUserAsync(id, ct);

        if (user.Id == currentUser.UserId)
            throw new ConflictException("No puede desactivarse a sí mismo.");

        if (!user.IsActive)
            return;

        if (user.HasRole(RoleNames.Admin))
            await EnsureAnotherActiveAdminAsync(user, ct);

        user.Deactivate(now);
        await RecordAsync(user.Id, AuthEventTypes.UserDeactivated, now, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await refreshTokens.RevokeAllActiveAsync(user.Id, now, ct);
    }

    public async Task ReactivateAsync(Guid id, CancellationToken ct = default)
    {
        var now = Now;
        var user = await GetUserAsync(id, ct);

        if (user.IsActive)
            return;

        user.Reactivate(now);
        await RecordAsync(user.Id, AuthEventTypes.UserReactivated, now, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task UnlockAsync(Guid id, CancellationToken ct = default)
    {
        var now = Now;
        var user = await GetUserAsync(id, ct);

        user.Unlock(now);
        await RecordAsync(user.Id, AuthEventTypes.AccountUnlocked, now, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ResetTwoFactorAsync(Guid id, CancellationToken ct = default)
    {
        var now = Now;
        var user = await GetUserAsync(id, ct);

        // Quitarse el 2FA a si mismo sin contrasena ni codigo permitiria que una sesion robada lo desactive
        if (user.Id == currentUser.UserId)
            throw new ConflictException("Para desactivar su propio 2FA use la opción de su perfil.");

        if (!user.TwoFactorEnabled)
            throw new ConflictException("El usuario no tiene la autenticación de dos factores activa.");

        user.DisableTwoFactor(now);
        await RecordAsync(user.Id, AuthEventTypes.TwoFactorReset, now, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await refreshTokens.RevokeAllActiveAsync(user.Id, now, ct);
    }

    #endregion

    #region Metodos Privados

    private async Task<User> GetUserAsync(Guid id, CancellationToken ct)
        => await users.GetByIdAsync(id, ct) ?? throw new NotFoundException(MsgNotFound);

    /// <summary>Quitarle el rol ADMIN (o desactivar) a este usuario no puede dejar al sistema sin un administrador activo.</summary>
    private async Task EnsureAnotherActiveAdminAsync(User user, CancellationToken ct)
    {
        // Si el usuario ya esta desactivado no cuenta como administrador activo, asi que quitarle el rol no cambia nada
        if (!user.IsActive)
            return;

        if (await users.CountActiveInRoleAsync(RoleNames.Admin, user.Id, ct) == 0)
            throw new ConflictException("Debe existir al menos un administrador activo.");
    }

    /// <summary>Quita repetidos y comprueba que todos los roles existan. Devuelve los roles encontrados.</summary>
    private async Task<IReadOnlyList<Role>> ValidateRolesAsync(IReadOnlyList<Guid> roleIds, string field, CancellationToken ct)
    {
        var ids = roleIds.Distinct().ToList();
        var found = await roles.GetByIdsAsync(ids, ct);

        if (found.Count != ids.Count)
            throw new ValidationException(new[] { new ValidationFailure(field, "Uno o más roles no existen.") });

        return found;
    }

    private Task RecordAsync(Guid userId, string eventType, DateTime now, CancellationToken ct)
        => authEvents.AddAsync(
            AuthEvent.Create(userId, eventType, currentUser.IpAddress ?? "unknown", currentUser.UserAgent, AuthEventStatuses.Success, now),
            ct);

    private static UserDetailDto ToDetail(User user, DateTime now)
        => new(
            user.Id,
            user.Username,
            user.Email,
            user.IsActive,
            user.IsLocked(now),
            user.LockedUntil,
            user.FailedLoginCount,
            user.TwoFactorEnabled,
            user.UserRoles.Select(ur => new RoleSummaryDto(ur.RoleId, ur.Role.Name)).OrderBy(r => r.Name).ToList(),
            user.CreatedAt,
            user.UpdatedAt);

    #endregion
}
