using Perrari.Domain.Entities.Security;
using Perrari.Domain.Exceptions;
using Xunit;

namespace Perrari.Domain.Tests;

public class UserManagementTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = Now.AddHours(1);

    private static User CreateUser() => User.Create("ana", "ana@perrari.com", "hash", Now);

    [Fact]
    public void New_user_is_active()
        => Assert.True(CreateUser().IsActive);

    [Fact]
    public void Deactivate_and_reactivate_toggle_the_flag_and_touch_updated_at()
    {
        var user = CreateUser();

        user.Deactivate(Later);
        Assert.False(user.IsActive);
        Assert.Equal(Later, user.UpdatedAt);

        user.Reactivate(Later.AddHours(1));
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Unlock_clears_the_lock_and_the_counter()
    {
        var user = CreateUser();
        for (var i = 0; i < User.MaxFailedLogins; i++)
            user.RegisterFailedLogin(Now);
        Assert.True(user.IsLocked(Now));

        user.Unlock(Now);

        Assert.False(user.IsLocked(Now));
        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockedUntil);
    }

    [Fact]
    public void ChangePassword_replaces_the_hash()
    {
        var user = CreateUser();

        user.ChangePassword("new-hash", Later);

        Assert.Equal("new-hash", user.PasswordHash);
    }

    [Fact]
    public void ChangePassword_rejects_an_empty_hash()
        => Assert.Throws<DomainException>(() => CreateUser().ChangePassword(" ", Later));

    [Fact]
    public void UpdateProfile_normalizes_the_email()
    {
        var user = CreateUser();

        user.UpdateProfile("  Ana Perez ", "  ANA.PEREZ@Perrari.com ", Later);

        Assert.Equal("Ana Perez", user.Username);
        Assert.Equal("ana.perez@perrari.com", user.Email);
    }

    [Theory]
    [InlineData("", "ana@perrari.com")]
    [InlineData("ana", "no-es-un-correo")]
    public void UpdateProfile_rejects_invalid_data(string username, string email)
        => Assert.Throws<DomainException>(() => CreateUser().UpdateProfile(username, email, Later));

    [Fact]
    public void SetRoles_adds_new_roles_and_removes_the_ones_left_out()
    {
        var user = CreateUser();
        var seller = Guid.CreateVersion7();
        var inventory = Guid.CreateVersion7();
        var admin = Guid.CreateVersion7();
        user.AssignRole(seller);
        user.AssignRole(inventory);

        user.SetRoles([inventory, admin, admin], Later);

        var ids = user.UserRoles.Select(r => r.RoleId).ToHashSet();
        Assert.Equal(new HashSet<Guid> { inventory, admin }, ids);
    }

    [Fact]
    public void SetRoles_rejects_an_empty_list()
        => Assert.Throws<DomainException>(() => CreateUser().SetRoles([], Later));

    [Fact]
    public void Role_SetPermissions_replaces_the_permissions()
    {
        var role = Role.Create("seller", null, Now);
        var read = Guid.CreateVersion7();
        var create = Guid.CreateVersion7();
        var delete = Guid.CreateVersion7();
        role.GrantPermission(read);
        role.GrantPermission(create);

        role.SetPermissions([create, delete]);

        var ids = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        Assert.Equal(new HashSet<Guid> { create, delete }, ids);
    }
}
