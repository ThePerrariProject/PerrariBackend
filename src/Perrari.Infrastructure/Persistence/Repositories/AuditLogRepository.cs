using Microsoft.EntityFrameworkCore;
using Perrari.Application.DTOs.Auditorias;
using Perrari.Application.Interfaces.Repositories;

namespace Perrari.Infrastructure.Persistence.Repositories;

public class AuditLogRepository(PerrariDbContext db) : IAuditLogRepository
{
    public async Task<PagedResultDto<AuditLogDto>> GetPagedAsync(AuditLogFilterDto filter, CancellationToken ct = default)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 20 : filter.PageSize;

        var query = db.AuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.TableName))
        {
            var tableUpper = filter.TableName.Trim().ToUpperInvariant();
            query = query.Where(x => x.TableName == tableUpper);
        }

        if (filter.RecordId.HasValue)
        {
            query = query.Where(x => x.RecordId == filter.RecordId.Value);
        }

        if (filter.UserId.HasValue)
        {
            query = query.Where(x => x.UserId == filter.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var actionUpper = filter.Action.Trim().ToUpperInvariant();
            query = query.Where(x => x.Action == actionUpper);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto(
                x.Id,
                x.TableName,
                x.RecordId,
                x.Action,
                x.OldValues,
                x.NewValues,
                x.UserId,
                x.User != null ? x.User.Username : null,
                x.IpAddress,
                x.CreatedAt
            ))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResultDto<AuditLogDto>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<AuditLogDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.AuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.Id == id)
            .Select(x => new AuditLogDto(
                x.Id,
                x.TableName,
                x.RecordId,
                x.Action,
                x.OldValues,
                x.NewValues,
                x.UserId,
                x.User != null ? x.User.Username : null,
                x.IpAddress,
                x.CreatedAt
            ))
            .FirstOrDefaultAsync(ct);
    }
}

