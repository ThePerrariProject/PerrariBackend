using Microsoft.AspNetCore.Mvc;
using Perrari.Api.Extensions;
using Perrari.Application.DTOs.Inventarios;
using Perrari.Application.Interfaces.Services;

namespace Perrari.Api.Controllers;

[ApiController]
[Route("api/branches")]
public class BranchesController(IBranchService branchService) : ControllerBase
{
    [HttpGet]
    [HasPermission("VEHICLES:READ")]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> List(CancellationToken ct)
        => Ok(await branchService.ListAsync(ct));

    [HttpGet("{id:guid}")]
    [HasPermission("VEHICLES:READ")]
    public async Task<ActionResult<BranchDto>> Get(Guid id, CancellationToken ct)
        => Ok(await branchService.GetAsync(id, ct));

    [HttpPost]
    [HasPermission("VEHICLES:CREATE")]
    public async Task<ActionResult<BranchDto>> Create(BranchInputDto dto, CancellationToken ct)
    {
        var created = await branchService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission("VEHICLES:UPDATE")]
    public async Task<ActionResult<BranchDto>> Update(Guid id, BranchInputDto dto, CancellationToken ct)
        => Ok(await branchService.UpdateAsync(id, dto, ct));
}
