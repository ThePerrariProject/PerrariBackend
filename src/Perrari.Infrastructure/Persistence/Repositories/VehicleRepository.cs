using Microsoft.EntityFrameworkCore;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Domain.Entities.Inventarios;

namespace Perrari.Infrastructure.Persistence.Repositories;

public class VehicleRepository(PerrariDbContext db) : IVehicleRepository
{
    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<VehicleRow?> GetRowAsync(Guid id, CancellationToken ct = default)
        => ToRows(db.Vehicles.AsNoTracking().Where(v => v.Id == id)).FirstOrDefaultAsync(ct);

    public Task<bool> VinExistsAsync(string vin, CancellationToken ct = default)
        => db.Vehicles.AnyAsync(v => v.Vin == vin, ct);

    public async Task<(IReadOnlyList<VehicleRow> Items, int TotalCount)> SearchAsync(VehicleSearchCriteria c, CancellationToken ct = default)
    {
        var query = db.Vehicles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(c.Search))
        {
            // Se escapan % _ y \ para que el texto se busque tal cual y no como comodin
            var escaped = c.Search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            var pattern = $"%{escaped}%";
            query = query.Where(v =>
                EF.Functions.ILike(v.Vin, pattern) ||
                EF.Functions.ILike(v.Brand, pattern) ||
                EF.Functions.ILike(v.Model, pattern));
        }

        if (c.BranchId is { } branchId)
            query = query.Where(v => v.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(c.Status))
            query = query.Where(v => v.Status == c.Status);

        var total = await query.CountAsync(ct);

        var items = await ToRows(query, newestFirst: true)
            .Skip((c.Page - 1) * c.PageSize)
            .Take(c.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken ct = default)
        => await db.Vehicles.AddAsync(vehicle, ct);

    public async Task AddMovementAsync(VehicleMovement movement, CancellationToken ct = default)
        => await db.VehicleMovements.AddAsync(movement, ct);

    public async Task<(IReadOnlyList<MovementRow> Items, int TotalCount)> GetMovementsAsync(
        Guid vehicleId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.VehicleMovements.AsNoTracking().Where(m => m.VehicleId == vehicleId);
        var total = await query.CountAsync(ct);

        var items = await (
                from m in query
                join u in db.Users on m.UserId equals u.Id into users
                from u in users.DefaultIfEmpty()
                orderby m.MovedAt descending, m.Id descending
                select new MovementRow(
                    m,
                    m.OriginBranch != null ? m.OriginBranch.Name : null,
                    m.DestinationBranch.Name,
                    u != null ? u.Username : null))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    // ---------- Apartado atomico ----------
    // ExecuteUpdateAsync emite UN solo UPDATE ... WHERE ... y devuelve las filas afectadas.
    // Si dos usuarios apartan a la vez, PostgreSQL serializa los UPDATE sobre la fila: el segundo vuelve
    // a evaluar la condicion, ya ve el apartado del primero y afecta 0 filas. No hay ventana entre leer y escribir.
    // Nota: ExecuteUpdate no pasa por SaveChanges, asi que estos cambios no generan fila en audit_logs.

    public async Task<bool> TryLockAsync(Guid id, Guid userId, DateTime now, DateTime lockExpiredBefore, CancellationToken ct = default)
    {
        var rows = await db.Vehicles
            .Where(v => v.Id == id
                        && v.Status == VehicleStatuses.Available
                        && (!v.IsLocked
                            || v.LockedAt == null
                            || v.LockedAt <= lockExpiredBefore
                            || v.LockedByUserId == userId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(v => v.IsLocked, true)
                .SetProperty(v => v.LockedAt, (DateTime?)now)
                .SetProperty(v => v.LockedByUserId, (Guid?)userId)
                .SetProperty(v => v.UpdatedAt, now), ct);

        return rows == 1;
    }

    public async Task<bool> TryUnlockAsync(Guid id, Guid userId, DateTime now, CancellationToken ct = default)
    {
        var rows = await db.Vehicles
            .Where(v => v.Id == id && v.IsLocked && v.LockedByUserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(v => v.IsLocked, false)
                .SetProperty(v => v.LockedAt, (DateTime?)null)
                .SetProperty(v => v.LockedByUserId, (Guid?)null)
                .SetProperty(v => v.UpdatedAt, now), ct);

        return rows == 1;
    }

    public async Task<bool> ForceUnlockAsync(Guid id, DateTime now, CancellationToken ct = default)
    {
        var rows = await db.Vehicles
            .Where(v => v.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(v => v.IsLocked, false)
                .SetProperty(v => v.LockedAt, (DateTime?)null)
                .SetProperty(v => v.LockedByUserId, (Guid?)null)
                .SetProperty(v => v.UpdatedAt, now), ct);

        return rows == 1; // 1 aunque no estuviera apartado: el WHERE solo filtra por id
    }

    /// <summary>Une cada vehiculo con el nombre de su sucursal y de quien lo aparto (LEFT JOIN).</summary>
    private IQueryable<VehicleRow> ToRows(IQueryable<Vehicle> vehicles, bool newestFirst = false)
    {
        var rows =
            from v in vehicles
            join b in db.Branches on v.BranchId equals b.Id
            join u in db.Users on v.LockedByUserId equals u.Id into owners
            from u in owners.DefaultIfEmpty()
            select new { v, BranchName = b.Name, OwnerName = u != null ? u.Username : null };

        // El orden se aplica antes de proyectar al record para que EF lo traduzca sin problemas
        var ordered = newestFirst
            ? rows.OrderByDescending(x => x.v.CreatedAt).ThenBy(x => x.v.Id)
            : rows.OrderBy(x => x.v.Id);

        return ordered.Select(x => new VehicleRow(x.v, x.BranchName, x.OwnerName));
    }
}
