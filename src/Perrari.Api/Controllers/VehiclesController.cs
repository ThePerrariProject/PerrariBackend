using Microsoft.AspNetCore.Mvc;
using Perrari.Api.Extensions;
using Perrari.Application.Common;
using Perrari.Application.DTOs.Inventarios;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController(IVehicleService vehicleService) : ControllerBase
{
    [HttpGet]
    [HasPermission("VEHICLES:READ")]
    public async Task<ActionResult<PagedResult<VehicleListItemDto>>> List([FromQuery] VehicleListQueryDto query, CancellationToken ct)
        => Ok(await vehicleService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission("VEHICLES:READ")]
    public async Task<ActionResult<VehicleDetailDto>> Get(Guid id, CancellationToken ct)
        => Ok(await vehicleService.GetAsync(id, ct));

    [HttpGet("{id:guid}/movements")]
    [HasPermission("VEHICLES:READ")]
    public async Task<ActionResult<PagedResult<VehicleMovementDto>>> Movements(Guid id, [FromQuery] MovementListQueryDto query, CancellationToken ct)
        => Ok(await vehicleService.GetMovementsAsync(id, query, ct));

    [HttpPost]
    [HasPermission("VEHICLES:CREATE")]
    public async Task<ActionResult<VehicleDetailDto>> Create(CreateVehicleDto dto, CancellationToken ct)
    {
        var created = await vehicleService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("VEHICLES:UPDATE")]
    public async Task<ActionResult<VehicleDetailDto>> Update(Guid id, UpdateVehicleDto dto, CancellationToken ct)
        => Ok(await vehicleService.UpdateAsync(id, dto, ct));

    [HttpPut("{id:guid}/status")]
    [HasPermission("VEHICLES:UPDATE")]
    public async Task<ActionResult<VehicleDetailDto>> ChangeStatus(Guid id, ChangeVehicleStatusDto dto, CancellationToken ct)
        => Ok(await vehicleService.ChangeStatusAsync(id, dto, ct));

    [HttpPost("{id:guid}/transfer")]
    [HasPermission("VEHICLES:UPDATE")]
    public async Task<ActionResult<VehicleDetailDto>> Transfer(Guid id, TransferVehicleDto dto, CancellationToken ct)
        => Ok(await vehicleService.TransferAsync(id, dto, ct));

    [HttpPost("{id:guid}/lock")]
    [HasPermission("SALES:CREATE")]
    public async Task<ActionResult<VehicleDetailDto>> Lock(Guid id, CancellationToken ct)
        => Ok(await vehicleService.LockAsync(id, ct));

    [HttpPost("{id:guid}/unlock")]
    [HasPermission("SALES:CREATE")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct)
    {
        await vehicleService.UnlockAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/force-unlock")]
    [HasPermission("VEHICLES:UPDATE")]
    public async Task<IActionResult> ForceUnlock(Guid id, CancellationToken ct)
    {
        await vehicleService.ForceUnlockAsync(id, ct);
        return NoContent();
    }
}
