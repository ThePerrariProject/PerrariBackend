namespace Perrari.Application.DTOs.Auditorias;

public record AuditLogFilterDto(
    string? TableName = null,
    Guid? RecordId = null,
    Guid? UserId = null,
    string? Action = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int Page = 1,
    int PageSize = 20
);

public record AuditLogDto(
    Guid Id,
    string TableName,
    Guid RecordId,
    string Action,
    string? OldValues,
    string? NewValues,
    Guid? UserId,
    string? Username,
    string IpAddress,
    DateTime CreatedAt
);

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

