using Microsoft.AspNetCore.Mvc;
using Perrari.Api.Extensions;
using Perrari.Application.Common;
using Perrari.Application.DTOs.Users;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [HasPermission("USERS:READ")]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> List([FromQuery] UserListQueryDto query, CancellationToken ct)
        => Ok(await userService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission("USERS:READ")]
    public async Task<ActionResult<UserDetailDto>> Get(Guid id, CancellationToken ct)
        => Ok(await userService.GetAsync(id, ct));

    [HttpPost]
    [HasPermission("USERS:CREATE")]
    public async Task<ActionResult<UserDetailDto>> Create(CreateUserDto dto, CancellationToken ct)
    {
        var created = await userService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("USERS:UPDATE")]
    public async Task<ActionResult<UserDetailDto>> Update(Guid id, UpdateUserDto dto, CancellationToken ct)
        => Ok(await userService.UpdateAsync(id, dto, ct));

    [HttpPut("{id:guid}/roles")]
    [HasPermission("USERS:UPDATE")]
    public async Task<ActionResult<UserDetailDto>> SetRoles(Guid id, SetUserRolesDto dto, CancellationToken ct)
        => Ok(await userService.SetRolesAsync(id, dto, ct));

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission("USERS:DELETE")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await userService.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    [HasPermission("USERS:UPDATE")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        await userService.ReactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/unlock")]
    [HasPermission("USERS:UPDATE")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct)
    {
        await userService.UnlockAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/2fa/reset")]
    [HasPermission("USERS:UPDATE")]
    public async Task<IActionResult> ResetTwoFactor(Guid id, CancellationToken ct)
    {
        await userService.ResetTwoFactorAsync(id, ct);
        return NoContent();
    }
}
