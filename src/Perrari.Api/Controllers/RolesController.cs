using Microsoft.AspNetCore.Mvc;
using Perrari.Api.Extensions;
using Perrari.Application.DTOs.Usuarios;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Api.Controllers;

[ApiController]
[Route("api/roles")]
public class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [HasPermission("USERS:READ")]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List(CancellationToken ct)
        => Ok(await roleService.ListRolesAsync(ct));

    [HttpGet("{id:guid}")]
    [HasPermission("USERS:READ")]
    public async Task<ActionResult<RoleDto>> Get(Guid id, CancellationToken ct)
        => Ok(await roleService.GetRoleAsync(id, ct));

    [HttpPut("{id:guid}/permissions")]
    [HasPermission("USERS:UPDATE")]
    public async Task<ActionResult<RoleDto>> SetPermissions(Guid id, SetRolePermissionsDto dto, CancellationToken ct)
        => Ok(await roleService.SetPermissionsAsync(id, dto, ct));
}
