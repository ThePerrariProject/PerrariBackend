using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perrari.Api.Extensions;
using Perrari.Application.DTOs.Auditorias;
using Perrari.Application.Interfaces.Repositories;

namespace Perrari.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController(IAuditLogRepository auditLogRepo) : ControllerBase
{
    [HttpGet]
    [HasPermission("AUDIT:READ")]
    public async Task<ActionResult<PagedResultDto<AuditLogDto>>> GetAuditLogs(
        [FromQuery] AuditLogFilterDto filter,
        CancellationToken ct)
    {
        return Ok(await auditLogRepo.GetPagedAsync(filter, ct));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("AUDIT:READ")]
    public async Task<ActionResult<AuditLogDto>> GetById(Guid id, CancellationToken ct)
    {
        var log = await auditLogRepo.GetByIdAsync(id, ct);
        if (log is null)
            return NotFound(new { message = $"No se encontró el registro de auditoría con ID '{id}'." });

        return Ok(log);
    }
}

