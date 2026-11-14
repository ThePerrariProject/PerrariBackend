using Perrari.Application.DTOs.Auditorias;

namespace Perrari.Application.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task<PagedResultDto<AuditLogDto>> GetPagedAsync(AuditLogFilterDto filter, CancellationToken ct = default);
    Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

