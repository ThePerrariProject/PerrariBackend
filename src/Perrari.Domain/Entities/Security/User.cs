using Perrari.Domain.Common;
using Perrari.Domain.Exceptions;

namespace Perrari.Domain.Entities.Security;

public class User : BaseEntity
{
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    private readonly List<UserRole> _userRoles = [];
    private readonly List<RecoveryCode> _recoveryCodes = [];

    private User() { }

    public string Username { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public DateTime? EmailVerifiedAt { get; private set; }
    public string PasswordHash { get; private set; } = default!; // columna "password"
    public bool TwoFactorEnabled { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public UserTotp? Totp { get; private set; }
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles;
    public IReadOnlyCollection<RecoveryCode> RecoveryCodes => _recoveryCodes;

    public static User Create(string username, string email, string passwordHash, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("El nombre de usuario es obligatorio.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("El correo no es válido.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("La contraseña es obligatoria.");

        return new User
        {
            Username = username.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    // Roles y permisos

    public void AssignRole(Guid roleId)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId))
            return;

        _userRoles.Add(new UserRole(Id, roleId));
    }

    public IReadOnlyList<string> GetRoleNames()
        => _userRoles.Select(ur => ur.Role.Name).Distinct().OrderBy(n => n).ToList();

    public IReadOnlyList<string> GetPermissionCodes()
        => _userRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

    // Bloqueo por intentos fallidos

    public bool IsLocked(DateTime now) => LockedUntil.HasValue && LockedUntil.Value > now;

    public void RegisterFailedLogin(DateTime now)
    {
        if (LockedUntil.HasValue && LockedUntil.Value <= now)
        {
            FailedLoginCount = 0;
            LockedUntil = null;
        }

        FailedLoginCount++;

        if (FailedLoginCount >= MaxFailedLogins)
            LockedUntil = now.Add(LockDuration);

        UpdatedAt = now;
    }

    public void RegisterSuccessfulLogin(DateTime now)
    {
        FailedLoginCount = 0;
        LockedUntil = null;
        UpdatedAt = now;
    }

    // Doble factor (TOTP)

    public void BeginTotpSetup(string secretEncrypted, DateTime now)
    {
        if (TwoFactorEnabled)
            throw new DomainException("La autenticación de dos factores ya está activa.");
        if (string.IsNullOrWhiteSpace(secretEncrypted))
            throw new DomainException("El secreto es obligatorio.");

        if (Totp is null)
            Totp = UserTotp.Create(Id, secretEncrypted, now);
        else
            Totp.ResetSecret(secretEncrypted);

        UpdatedAt = now;
    }

    public void ConfirmTotp(long step, IEnumerable<string> recoveryCodeHashes, DateTime now)
    {
        if (TwoFactorEnabled)
            throw new DomainException("La autenticación de dos factores ya está activa.");
        if (Totp is null)
            throw new DomainException("Primero debe iniciar la configuración de dos factores.");

        Totp.Confirm(step, now);
        TwoFactorEnabled = true;

        _recoveryCodes.Clear();
        foreach (var hash in recoveryCodeHashes)
            _recoveryCodes.Add(new RecoveryCode(Id, hash));

        UpdatedAt = now;
    }

    public void DisableTwoFactor(DateTime now)
    {
        TwoFactorEnabled = false;
        Totp = null;
        _recoveryCodes.Clear();
        UpdatedAt = now;
    }

    public bool IsTotpStepUsed(long step) => Totp?.LastUsedStep is { } last && step <= last;

    public void RegisterTotpStep(long step) => Totp?.RegisterStep(step);

    public bool ConsumeRecoveryCode(string codeHash, DateTime now)
    {
        var code = _recoveryCodes.FirstOrDefault(c => !c.IsUsed && c.CodeHash == codeHash);
        if (code is null)
            return false;

        code.MarkUsed(now);
        return true;
    }
}
