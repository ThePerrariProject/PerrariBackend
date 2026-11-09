using Perrari.Domain.Common;

namespace Perrari.Domain.Entities.Security;

public class RecoveryCode : BaseEntity
{
    private RecoveryCode() { }

    internal RecoveryCode(Guid userId, string codeHash)
    {
        UserId = userId;
        CodeHash = codeHash;
    }

    public Guid UserId { get; private set; }
    public string CodeHash { get; private set; } = default!;
    public DateTime? UsedAt { get; private set; }

    public bool IsUsed => UsedAt.HasValue;

    internal void MarkUsed(DateTime now) => UsedAt = now;
}