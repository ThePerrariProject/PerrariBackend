using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Users;
using Perrari.Application.Services;
using Perrari.Application.Validators.Users;
using Perrari.Domain.Common;
using Perrari.Domain.Entities.Security;
using Xunit;

namespace Perrari.Application.Tests;

public class RoleServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Admin_cannot_lose_the_USERS_permissions()
    {
        var usersRead = Permission.Create("USERS", "READ", null);
        var usersUpdate = Permission.Create("USERS", "UPDATE", null);
        var vehiclesRead = Permission.Create("VEHICLES", "READ", null);
        var admin = Role.Create(RoleNames.Admin, null, Now);
        foreach (var p in new[] { usersRead, usersUpdate, vehiclesRead })
            admin.GrantPermission(p.Id);

        var service = new RoleService(
            new FakeRoleRepository([admin], [usersRead, usersUpdate, vehiclesRead]),
            new FakeUnitOfWork(),
            new SetRolePermissionsDtoValidator());

        // Intenta dejar al ADMIN solo con VEHICLES:READ
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SetPermissionsAsync(admin.Id, new SetRolePermissionsDto([vehiclesRead.Id])));

        Assert.Equal(3, admin.RolePermissions.Count);
    }

    [Fact]
    public async Task Unknown_permission_ids_are_rejected()
    {
        var seller = Role.Create(RoleNames.Seller, null, Now);
        var service = new RoleService(
            new FakeRoleRepository([seller], []),
            new FakeUnitOfWork(),
            new SetRolePermissionsDtoValidator());

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.SetPermissionsAsync(seller.Id, new SetRolePermissionsDto([Guid.NewGuid()])));
    }
}
