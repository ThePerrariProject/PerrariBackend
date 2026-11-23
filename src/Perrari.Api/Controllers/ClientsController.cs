using Microsoft.AspNetCore.Mvc;
using Perrari.Api.Extensions;
using Perrari.Application.Common;
using Perrari.Application.DTOs.Clientes;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Api.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController(IClientService clientService) : ControllerBase
{
    [HttpGet]
    [HasPermission("CLIENTS:READ")]
    public async Task<ActionResult<PagedResult<ClientListItemDto>>> List(
        [FromQuery] ClientListQueryDto query,
        CancellationToken ct)
    {
        return Ok(await clientService.ListAsync(query, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("CLIENTS:READ")]
    public async Task<ActionResult<ClientDetailDto>> Get(Guid id, CancellationToken ct)
    {
        return Ok(await clientService.GetAsync(id, ct));
    }

    [HttpPost]
    [HasPermission("CLIENTS:CREATE")]
    public async Task<ActionResult<ClientDetailDto>> Create(
        [FromBody] CreateClientDto dto,
        CancellationToken ct)
    {
        var created = await clientService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("CLIENTS:UPDATE")]
    public async Task<ActionResult<ClientDetailDto>> Update(
        Guid id,
        [FromBody] UpdateClientDto dto,
        CancellationToken ct)
    {
        return Ok(await clientService.UpdateAsync(id, dto, ct));
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission("CLIENTS:DELETE")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await clientService.DeactivateAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    [HasPermission("CLIENTS:UPDATE")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        await clientService.ReactivateAsync(id, ct);
        return NoContent();
    }
}

