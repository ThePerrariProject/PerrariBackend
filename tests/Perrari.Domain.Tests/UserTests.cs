using Perrari.Domain.Entities.Security;
using Perrari.Domain.Exceptions;
using Xunit;

namespace Perrari.Domain.Tests;

public class UserTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    private static User CreateUser() => User.Create("ana", "  ANA@Perrari.com ", "hash", Now);

    private static User CreateUserWithTwoFactor()
    {
        var user = CreateUser();
        user.BeginTotpSetup("secret-encrypted", Now);
        user.ConfirmTotp(100, ["HASH1", "HASH2"], Now);
        return user;
    }

    [Fact]
    public void Create_normalizes_email()
        => Assert.Equal("ana@perrari.com", CreateUser().Email);

    [Fact]
    public void Locks_after_max_failed_logins()
    {
        var user = CreateUser();

        for (var i = 0; i < User.MaxFailedLogins; i++)
            user.RegisterFailedLogin(Now);

        Assert.True(user.IsLocked(Now));
    }

    [Fact]
    public void Lock_expires_and_counter_restarts()
    {
        var user = CreateUser();
        for (var i = 0; i < User.MaxFailedLogins; i++)
            user.RegisterFailedLogin(Now);

        var later = Now.Add(User.LockDuration).AddMinutes(1);
        user.RegisterFailedLogin(later);

        Assert.False(user.IsLocked(later));
        Assert.Equal(1, user.FailedLoginCount);
    }

    [Fact]
    public void Successful_login_resets_counter()
    {
        var user = CreateUser();
        user.RegisterFailedLogin(Now);
        user.RegisterFailedLogin(Now);

        user.RegisterSuccessfulLogin(Now);

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockedUntil);
    }

    [Fact]
    public void ConfirmTotp_requires_setup_first()
    {
        var user = CreateUser();

        Assert.Throws<DomainException>(() => user.ConfirmTotp(1, ["HASH1"], Now));
    }

    [Fact]
    public void ConfirmTotp_enables_two_factor()
    {
        var user = CreateUserWithTwoFactor();

        Assert.True(user.TwoFactorEnabled);
        Assert.True(user.Totp!.IsConfirmed);
        Assert.Equal(2, user.RecoveryCodes.Count);
    }

    [Fact]
    public void Totp_step_cannot_be_reused()
    {
        var user = CreateUserWithTwoFactor();

        Assert.True(user.IsTotpStepUsed(100));
        Assert.True(user.IsTotpStepUsed(99));
        Assert.False(user.IsTotpStepUsed(101));

        user.RegisterTotpStep(101);
        Assert.True(user.IsTotpStepUsed(101));
    }

    [Fact]
    public void Recovery_code_works_only_once()
    {
        var user = CreateUserWithTwoFactor();

        Assert.True(user.ConsumeRecoveryCode("HASH1", Now));
        Assert.False(user.ConsumeRecoveryCode("HASH1", Now));
        Assert.False(user.ConsumeRecoveryCode("DOES-NOT-EXIST", Now));
    }

    [Fact]
    public void DisableTwoFactor_clears_secret_and_codes()
    {
        var user = CreateUserWithTwoFactor();

        user.DisableTwoFactor(Now);

        Assert.False(user.TwoFactorEnabled);
        Assert.Null(user.Totp);
        Assert.Empty(user.RecoveryCodes);
    }

    [Fact]
    public void BeginTotpSetup_fails_when_already_enabled()
    {
        var user = CreateUserWithTwoFactor();

        Assert.Throws<DomainException>(() => user.BeginTotpSetup("other-secret", Now));
    }

    [Fact]
    public void RefreshToken_is_active_until_expired_or_revoked()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddHours(8), Now);

        Assert.True(token.IsActive(Now));
        Assert.False(token.IsActive(Now.AddHours(9)));

        token.Revoke(Now);
        Assert.False(token.IsActive(Now));
    }
}
