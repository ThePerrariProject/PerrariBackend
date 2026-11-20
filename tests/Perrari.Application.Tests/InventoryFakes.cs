using Perrari.Application.Interfaces.Repositories;
using Perrari.Domain.Entities.Inventarios;

namespace Perrari.Application.Tests;

// Requiere Fakes.cs del modulo de Personal (FakeUnitOfWork y FakeCurrentUser).

internal sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider
{
    public DateTime UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(UtcNow, DateTimeKind.Utc));
}

internal sealed class FakeBranchRepository : IBranchRepository
{
    public List<Branch> Store { get; } = [];

    public Task<Branch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(b => b.Id == id));

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Store.Any(b => b.Id == id));

    public Task<BranchWithCount?> GetWithCountAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Store.Where(b => b.Id == id).Select(b => new BranchWithCount(b, 0)).FirstOrDefault());

    public Task<IReadOnlyList<BranchWithCount>> ListAsync(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<BranchWithCount>)Store.OrderBy(b => b.Name).Select(b => new BranchWithCount(b, 0)).ToList());

    public Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default)
        => Task.FromResult(Store.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase) && b.Id != excludeId));

    public Task AddAsync(Branch branch, CancellationToken ct = default)
    {
        Store.Add(branch);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Reproduce en memoria las mismas condiciones del UPDATE de apartado. No prueba la concurrencia real
/// (eso lo hace PostgreSQL); prueba que el servicio interprete bien el resultado true/false.
/// </summary>
internal sealed class FakeVehicleRepository(FakeBranchRepository branches) : IVehicleRepository
{
    public List<Vehicle> Store { get; } = [];
    public List<VehicleMovement> Movements { get; } = [];
    public Dictionary<Guid, string> UserNames { get; } = [];

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(v => v.Id == id));

    public Task<VehicleRow?> GetRowAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Store.Where(v => v.Id == id).Select(ToRow).FirstOrDefault());

    public Task<bool> VinExistsAsync(string vin, CancellationToken ct = default)
        => Task.FromResult(Store.Any(v => v.Vin == vin));

    public Task<(IReadOnlyList<VehicleRow> Items, int TotalCount)> SearchAsync(VehicleSearchCriteria c, CancellationToken ct = default)
    {
        var q = Store.AsEnumerable();
        if (c.BranchId is { } b) q = q.Where(v => v.BranchId == b);
        if (c.Status is { } s) q = q.Where(v => v.Status == s);
        if (c.Search is { } t) q = q.Where(v => v.Vin.Contains(t, StringComparison.OrdinalIgnoreCase)
                                                || v.Brand.Contains(t, StringComparison.OrdinalIgnoreCase)
                                                || v.Model.Contains(t, StringComparison.OrdinalIgnoreCase));
        var list = q.ToList();
        var page = list.Skip((c.Page - 1) * c.PageSize).Take(c.PageSize).Select(ToRow).ToList();
        return Task.FromResult(((IReadOnlyList<VehicleRow>)page, list.Count));
    }

    public Task AddAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        Store.Add(vehicle);
        return Task.CompletedTask;
    }

    public Task AddMovementAsync(VehicleMovement movement, CancellationToken ct = default)
    {
        Movements.Add(movement);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<MovementRow> Items, int TotalCount)> GetMovementsAsync(Guid vehicleId, int page, int pageSize, CancellationToken ct = default)
    {
        var rows = Movements.Where(m => m.VehicleId == vehicleId)
            .Select(m => new MovementRow(m, null, "destino", UserNames.GetValueOrDefault(m.UserId)))
            .ToList();
        return Task.FromResult(((IReadOnlyList<MovementRow>)rows, rows.Count));
    }

    public Task<bool> TryLockAsync(Guid id, Guid userId, DateTime now, DateTime lockExpiredBefore, CancellationToken ct = default)
    {
        var v = Store.FirstOrDefault(x => x.Id == id);
        var ok = v is not null
                 && v.Status == VehicleStatuses.Available
                 && (!v.IsLocked || v.LockedAt is null || v.LockedAt <= lockExpiredBefore || v.LockedByUserId == userId);
        if (ok)
            Set(v!, true, now, userId);
        return Task.FromResult(ok);
    }

    public Task<bool> TryUnlockAsync(Guid id, Guid userId, DateTime now, CancellationToken ct = default)
    {
        var v = Store.FirstOrDefault(x => x.Id == id);
        var ok = v is { IsLocked: true } && v.LockedByUserId == userId;
        if (ok)
            Set(v!, false, null, null);
        return Task.FromResult(ok);
    }

    public Task<bool> ForceUnlockAsync(Guid id, DateTime now, CancellationToken ct = default)
    {
        var v = Store.FirstOrDefault(x => x.Id == id);
        if (v is not null)
            Set(v, false, null, null);
        return Task.FromResult(v is not null);
    }

    private VehicleRow ToRow(Vehicle v)
        => new(v,
            branches.Store.FirstOrDefault(b => b.Id == v.BranchId)?.Name ?? "?",
            v.LockedByUserId is { } u ? UserNames.GetValueOrDefault(u) : null);

    private static void Set(Vehicle v, bool locked, DateTime? at, Guid? by)
    {
        typeof(Vehicle).GetProperty(nameof(Vehicle.IsLocked))!.SetValue(v, locked);
        typeof(Vehicle).GetProperty(nameof(Vehicle.LockedAt))!.SetValue(v, at);
        typeof(Vehicle).GetProperty(nameof(Vehicle.LockedByUserId))!.SetValue(v, by);
    }
}
