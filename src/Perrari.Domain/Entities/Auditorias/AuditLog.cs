using Perrari.Domain.Common;
using Perrari.Domain.Entities.Security;

namespace Perrari.Domain.Entities.Auditorias;

public class AuditLog : BaseEntity
{
    private AuditLog() { }

    public string TableName { get; private set; } = default!;
    public Guid RecordId { get; private set; }
    public string Action { get; private set; } = default!;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public Guid? UserId { get; private set; }
    public string IpAddress { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public User? User { get; private set; }

    public static AuditLog Create(
        string tableName,
        Guid recordId,
        string action,
        string? oldValues,
        string? newValues,
        Guid? userId,
        string ipAddress,
        DateTime createdAt)
    {
        return new AuditLog
        {
            TableName = tableName,
            RecordId = recordId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = userId,
            IpAddress = ipAddress,
            CreatedAt = createdAt
        };
    }
}

