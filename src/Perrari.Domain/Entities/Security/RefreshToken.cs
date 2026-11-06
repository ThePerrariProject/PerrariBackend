using Perrari.Domain.Common;

namespace Perrari.Domain.Entities.Security;

public class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt, DateTime now)
        => new() { UserId = userId, TokenHash = tokenHash, ExpiresAt = expiresAt, CreatedAt = now };

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTime now) => RevokedAt ??= now;
}