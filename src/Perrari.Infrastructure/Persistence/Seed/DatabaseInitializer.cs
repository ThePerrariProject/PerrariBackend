using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Perrari.Application.Interfaces.Services;
using Perrari.Domain.Common;
using Perrari.Domain.Entities.Security;

namespace Perrari.Infrastructure.Persistence.Seed;

public static class DatabaseInitializer
{
    private static readonly string[] Modules = ["VEHICLES", "CLIENTS", "SALES", "USERS", "AUDIT"];
    private static readonly string[] Actions = ["READ", "CREATE", "UPDATE", "DELETE"];

    /// <summary>
    /// Aplica migraciones (opcional), crea roles y permisos base si la tabla roles esta vacia y,
    /// si no existe ningun usuario, crea el primer ADMIN con Seed:AdminEmail y Seed:AdminPassword
    /// (usar User Secrets / variables de entorno). Sin esos valores no se crea el usuario.
    /// </summary>
    public static async Task InitializeAsync(
        IServiceProvider services, IConfiguration configuration, bool applyMigrations, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PerrariDbContext>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        if (applyMigrations)
            await db.Database.MigrateAsync(ct);

        if (!await db.Roles.AnyAsync(ct))
        {
            var permissions = Modules
                .SelectMany(m => Actions.Select(a => Permission.Create(m, a, $"{a} sobre {m}")))
                .ToList();
            db.Permissions.AddRange(permissions);

            Role BuildRole(string name, string description, Func<Permission, bool> allow)
            {
                var role = Role.Create(name, description, now);
                foreach (var p in permissions.Where(allow))
                    role.GrantPermission(p.Id);
                return role;
            }

            db.Roles.AddRange(
                BuildRole(RoleNames.Admin, "Administrador del sistema", _ => true),
                BuildRole(RoleNames.Seller, "Asesor de ventas",
                    p => p.Module is "CLIENTS" or "SALES" || p is { Module: "VEHICLES", Action: "READ" }),
                BuildRole(RoleNames.InventoryManager, "Encargado de inventario", p => p.Module == "VEHICLES"));

            await db.SaveChangesAsync(ct);
        }

        if (await db.Users.AnyAsync(ct))
            return;

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        if (password.Length < 12)
            throw new InvalidOperationException("Seed:AdminPassword debe tener al menos 12 caracteres.");

        var adminRole = await db.Roles.SingleAsync(r => r.Name == RoleNames.Admin, ct);
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var admin = User.Create(configuration["Seed:AdminUsername"] ?? "admin", email, hasher.Hash(password), now);
        admin.AssignRole(adminRole.Id);

        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);
    }
}
