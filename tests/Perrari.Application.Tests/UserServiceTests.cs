using FluentValidation;
using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Users;
using Perrari.Application.Services;
using Perrari.Application.Validators.Users;
using Perrari.Domain.Common;
using Perrari.Domain.Entities.Security;
using Xunit;

namespace Perrari.Application.Tests;

public class UserServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private sealed class Harness
    {
        public Role AdminRole { get; } = Role.Create(RoleNames.Admin, null, Now);
        public Role SellerRole { get; } = Role.Create(RoleNames.Seller, null, Now);
        public FakeUserRepository Users { get; }
        public FakeRefreshTokenRepository Tokens { get; } = new();
        public FakeAuthEventRepository Events { get; } = new();
        public FakeCurrentUser Current { get; } = new();
        public UserService Service { get; }

        public Harness()
        {
            var roles = new List<Role> { AdminRole, SellerRole };
            Users = new FakeUserRepository(roles);

            Service = new UserService(
                Users,
                new FakeRoleRepository(roles, []),
                Tokens,
                Events,
                new FakeUnitOfWork(),
                new FakePasswordHasher(),
                Current,
                TimeProvider.System,
                new CreateUserDtoValidator(),
                new UpdateUserDtoValidator(),
                new SetUserRolesDtoValidator(),
                new UserListQueryDtoValidator());
        }

        public User AddUser(string name, Role role, bool active = true)
        {
            var user = User.Create(name, $"{name}@perrari.com", "hash:Secreto#Largo123", Now);
            user.AssignRole(role.Id);
            Reflect.LoadRoles(user, [AdminRole, SellerRole]);
            if (!active)
                user.Deactivate(Now);
            Users.Store.Add(user);
            return user;
        }
    }

    // ---------- Alta ----------

    [Fact]
    public async Task Create_hashes_the_password_and_assigns_the_roles()
    {
        var h = new Harness();

        var created = await h.Service.CreateAsync(
            new CreateUserDto("luis", "  LUIS@Perrari.com ", "Contraseña#Segura9", [h.SellerRole.Id]));

        Assert.Equal("luis@perrari.com", created.Email);
        Assert.Equal(new[] { RoleNames.Seller }, created.Roles.Select(r => r.Name));
        Assert.Equal("hash:Contraseña#Segura9", h.Users.Store.Single().PasswordHash);
    }

    [Fact]
    public async Task Create_with_a_repeated_email_is_a_conflict()
    {
        var h = new Harness();
        h.AddUser("ana", h.SellerRole);

        await Assert.ThrowsAsync<ConflictException>(() => h.Service.CreateAsync(
            new CreateUserDto("otra", "ANA@perrari.com", "Contraseña#Segura9", [h.SellerRole.Id])));
    }

    [Theory]
    [InlineData("corta1!A")]            // menos de 12 caracteres
    [InlineData("sinmayusculas#123")]
    [InlineData("SINMINUSCULAS#123")]
    [InlineData("SinNumeros#Aqui!")]
    [InlineData("SinSimbolos12345Ab")]
    public async Task Create_rejects_passwords_that_break_the_policy(string password)
    {
        var h = new Harness();

        await Assert.ThrowsAsync<ValidationException>(() => h.Service.CreateAsync(
            new CreateUserDto("luis", "luis@perrari.com", password, [h.SellerRole.Id])));
    }

    [Fact]
    public async Task Create_rejects_a_password_equal_to_the_email()
    {
        var h = new Harness();

        // Cumple todos los requisitos de complejidad, pero es el correo
        await Assert.ThrowsAsync<ValidationException>(() => h.Service.CreateAsync(
            new CreateUserDto("luis", "Luis@Perrari1.com", "Luis@Perrari1.com", [h.SellerRole.Id])));
    }

    [Fact]
    public async Task Create_with_an_unknown_role_is_rejected()
    {
        var h = new Harness();

        await Assert.ThrowsAsync<ValidationException>(() => h.Service.CreateAsync(
            new CreateUserDto("luis", "luis@perrari.com", "Contraseña#Segura9", [Guid.NewGuid()])));
    }

    // ---------- Baja logica ----------

    [Fact]
    public async Task Deactivate_yourself_is_a_conflict()
    {
        var h = new Harness();
        var admin = h.AddUser("admin", h.AdminRole);
        h.AddUser("admin2", h.AdminRole);
        h.Current.UserId = admin.Id;

        await Assert.ThrowsAsync<ConflictException>(() => h.Service.DeactivateAsync(admin.Id));
        Assert.True(admin.IsActive);
    }

    [Fact]
    public async Task Deactivate_the_last_active_admin_is_a_conflict()
    {
        var h = new Harness();
        var admin = h.AddUser("admin", h.AdminRole);
        var seller = h.AddUser("ventas", h.SellerRole);
        h.Current.UserId = seller.Id;

        await Assert.ThrowsAsync<ConflictException>(() => h.Service.DeactivateAsync(admin.Id));
        Assert.True(admin.IsActive);
    }

    [Fact]
    public async Task Deactivate_an_admin_is_allowed_when_another_active_admin_exists()
    {
        var h = new Harness();
        var first = h.AddUser("admin", h.AdminRole);
        var second = h.AddUser("admin2", h.AdminRole);
        h.Current.UserId = first.Id;

        await h.Service.DeactivateAsync(second.Id);

        Assert.False(second.IsActive);
        Assert.Contains(second.Id, h.Tokens.RevokedForUsers);
        Assert.Contains(h.Events.Events, e => e.UserId == second.Id && e.EventType == AuthEventTypes.UserDeactivated);
    }

    [Fact]
    public async Task An_inactive_admin_does_not_count_as_another_admin()
    {
        var h = new Harness();
        var admin = h.AddUser("admin", h.AdminRole);
        h.AddUser("viejo", h.AdminRole, active: false);
        var seller = h.AddUser("ventas", h.SellerRole);
        h.Current.UserId = seller.Id;

        await Assert.ThrowsAsync<ConflictException>(() => h.Service.DeactivateAsync(admin.Id));
    }

    [Fact]
    public async Task Deactivate_an_unknown_user_is_not_found()
    {
        var h = new Harness();

        await Assert.ThrowsAsync<NotFoundException>(() => h.Service.DeactivateAsync(Guid.NewGuid()));
    }

    // ---------- Roles ----------

    [Fact]
    public async Task SetRoles_cannot_remove_the_admin_role_from_yourself()
    {
        var h = new Harness();
        var admin = h.AddUser("admin", h.AdminRole);
        h.AddUser("admin2", h.AdminRole);
        h.Current.UserId = admin.Id;

        await Assert.ThrowsAsync<ConflictException>(() =>
            h.Service.SetRolesAsync(admin.Id, new SetUserRolesDto([h.SellerRole.Id])));
    }

    [Fact]
    public async Task SetRoles_cannot_remove_the_admin_role_from_the_last_admin()
    {
        var h = new Harness();
        var admin = h.AddUser("admin", h.AdminRole);
        var seller = h.AddUser("ventas", h.SellerRole);
        h.Current.UserId = seller.Id;

        await Assert.ThrowsAsync<ConflictException>(() =>
            h.Service.SetRolesAsync(admin.Id, new SetUserRolesDto([h.SellerRole.Id])));
    }

    [Fact]
    public async Task SetRoles_changes_the_roles_and_closes_the_sessions()
    {
        var h = new Harness();
        var admin = h.AddUser("admin", h.AdminRole);
        var seller = h.AddUser("ventas", h.SellerRole);
        h.Current.UserId = admin.Id;

        var result = await h.Service.SetRolesAsync(seller.Id, new SetUserRolesDto([h.AdminRole.Id]));

        Assert.Equal(new[] { RoleNames.Admin }, result.Roles.Select(r => r.Name));
        Assert.Contains(seller.Id, h.Tokens.RevokedForUsers);
    }

    [Fact]
    public async Task SetRoles_requires_at_least_one_role()
    {
        var h = new Harness();
        var seller = h.AddUser("ventas", h.SellerRole);

        await Assert.ThrowsAsync<ValidationException>(() => h.Service.SetRolesAsync(seller.Id, new SetUserRolesDto([])));
    }

    // ---------- Desbloqueo y 2FA ----------

    [Fact]
    public async Task Unlock_clears_the_lock_and_records_the_event()
    {
        var h = new Harness();
        var seller = h.AddUser("ventas", h.SellerRole);
        for (var i = 0; i < User.MaxFailedLogins; i++)
            seller.RegisterFailedLogin(DateTime.UtcNow);

        await h.Service.UnlockAsync(seller.Id);

        Assert.False(seller.IsLocked(DateTime.UtcNow));
        Assert.Contains(h.Events.Events, e => e.EventType == AuthEventTypes.AccountUnlocked);
    }

    [Fact]
    public async Task ResetTwoFactor_removes_the_second_factor_and_closes_the_sessions()
    {
        var h = new Harness();
        var admin = h.AddUser("admin", h.AdminRole);
        var seller = h.AddUser("ventas", h.SellerRole);
        seller.BeginTotpSetup("secret", Now);
        seller.ConfirmTotp(1, ["H1", "H2"], Now);
        h.Current.UserId = admin.Id;

        await h.Service.ResetTwoFactorAsync(seller.Id);

        Assert.False(seller.TwoFactorEnabled);
        Assert.Contains(seller.Id, h.Tokens.RevokedForUsers);
        Assert.Contains(h.Events.Events, e => e.UserId == seller.Id && e.EventType == AuthEventTypes.TwoFactorReset);
    }

    [Fact]
    public async Task ResetTwoFactor_for_yourself_is_a_conflict()
    {
        var h = new Harness();
        var admin = h.AddUser("admin", h.AdminRole);
        admin.BeginTotpSetup("secret", Now);
        admin.ConfirmTotp(1, ["H1"], Now);
        h.Current.UserId = admin.Id;

        await Assert.ThrowsAsync<ConflictException>(() => h.Service.ResetTwoFactorAsync(admin.Id));
    }

    [Fact]
    public async Task ResetTwoFactor_without_active_2fa_is_a_conflict()
    {
        var h = new Harness();
        var seller = h.AddUser("ventas", h.SellerRole);

        await Assert.ThrowsAsync<ConflictException>(() => h.Service.ResetTwoFactorAsync(seller.Id));
    }
}
