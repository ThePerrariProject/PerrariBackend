using Microsoft.AspNetCore.Mvc;
using Perrari.Api.Extensions;
using Perrari.Application.DTOs.Users;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Api.Controllers;

/// <summary>Catalogo de permisos (solo lectura).</summary>
[ApiController]
[Route("api/permissions")]
public class PermissionsController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [HasPermission("USERS:READ")]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> List(CancellationToken ct)
        => Ok(await roleService.ListPermissionsAsync(ct));
}
