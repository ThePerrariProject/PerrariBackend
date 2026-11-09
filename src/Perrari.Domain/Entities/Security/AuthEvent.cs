using System.Net.NetworkInformation;
using Perrari.Domain.Common;

namespace Perrari.Domain.Entities.Security;

public class AuthEvent : BaseEntity
{
    private AuthEvent() { }

    public Guid? UserId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string IpAddress { get; private set; } = default!;
    public string? UserAgent { get; private set; }
    public string Status { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public static AuthEvent Create(Guid? userId, string eventType, string ipAddress, string? userAgent, string status, DateTime now)
        => new()
        {
            UserId = userId,
            EventType = eventType,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Status = status,
            CreatedAt = now
        };
}