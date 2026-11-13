using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Perrari.Application.Interfaces.Services;
using Perrari.Domain.Common;
using Perrari.Domain.Entities.Auditorias;
using Perrari.Domain.Entities.Security;

namespace Perrari.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor(ICurrentUserService currentUserService, TimeProvider timeProvider) : SaveChangesInterceptor
{
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordhash",
        "secret_encrypted",
        "secretencrypted",
        "code_hash",
        "codehash",
        "token_hash",
        "tokenhash"
    };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            OnBeforeSaveChanges(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            OnBeforeSaveChanges(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void OnBeforeSaveChanges(DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable &&
                        e.Entity is not AuditLog &&
                        e.Entity is not AuthEvent &&
                        e.Entity is not RefreshToken &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        if (entries.Count == 0)
            return;

        var userId = currentUserService.UserId;
        var ipAddress = !string.IsNullOrWhiteSpace(currentUserService.IpAddress)
            ? currentUserService.IpAddress
            : "system";
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var auditLogs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            var tableName = (entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name).ToUpperInvariant();

            Guid recordId = Guid.Empty;
            if (entry.Entity is BaseEntity baseEntity)
            {
                recordId = baseEntity.Id;
            }
            else
            {
                var idProp = entry.Property("Id");
                if (idProp?.CurrentValue is Guid g && g != Guid.Empty)
                    recordId = g;
                else if (idProp?.OriginalValue is Guid og)
                    recordId = og;
            }

            string action = entry.State switch
            {
                EntityState.Added => "INSERT",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(action))
                continue;

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                var propName = property.Metadata.Name;
                var colName = property.Metadata.GetColumnName();

                if (ExcludedProperties.Contains(propName) || (colName != null && ExcludedProperties.Contains(colName)))
                    continue;

                var key = colName ?? propName;

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[key] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        oldValues[key] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                        {
                            oldValues[key] = property.OriginalValue;
                            newValues[key] = property.CurrentValue;
                        }
                        break;
                }
            }

            // Si es un UPDATE pero no hubo cambios en propiedades auditables (ej. solo cambio de password), omitir
            if (entry.State == EntityState.Modified && oldValues.Count == 0 && newValues.Count == 0)
                continue;

            string? oldJson = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null;
            string? newJson = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null;

            var auditLog = AuditLog.Create(
                tableName: tableName,
                recordId: recordId,
                action: action,
                oldValues: oldJson,
                newValues: newJson,
                userId: userId,
                ipAddress: ipAddress,
                createdAt: now
            );

            auditLogs.Add(auditLog);
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }
}

