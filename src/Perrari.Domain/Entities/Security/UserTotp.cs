using Perrari.Domain.Common;

namespace Perrari.Domain.Entities.Security;

public class UserTotp : BaseEntity
{
    private UserTotp() { }

    public Guid UserId { get; private set; }
    public string? SecretEncrypted { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public long? LastUsedStep { get; private set; }
    public DateTime? CreatedAt { get; private set; }

    public bool IsConfirmed => ConfirmedAt.HasValue;

    internal static UserTotp Create(Guid userId, string secretEncrypted, DateTime now)
        => new() { UserId = userId, SecretEncrypted = secretEncrypted, CreatedAt = now };

    internal void ResetSecret(string secretEncrypted)
    {
        SecretEncrypted = secretEncrypted;
        ConfirmedAt = null;
        LastUsedStep = null;
    }

    internal void Confirm(long step, DateTime now)
    {
        ConfirmedAt = now;
        LastUsedStep = step;
    }

    internal void RegisterStep(long step) => LastUsedStep = step;
}