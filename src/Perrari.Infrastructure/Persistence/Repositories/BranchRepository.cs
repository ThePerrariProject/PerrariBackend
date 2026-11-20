using Microsoft.EntityFrameworkCore;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Domain.Entities.Inventarios;

namespace Perrari.Infrastructure.Persistence.Repositories;

public class BranchRepository(PerrariDbContext db) : IBranchRepository
{
    public Task<Branch?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => db.Branches.AnyAsync(b => b.Id == id, ct);

    public Task<BranchWithCount?> GetWithCountAsync(Guid id, CancellationToken ct = default)
        => WithCount(db.Branches.AsNoTracking().Where(b => b.Id == id)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BranchWithCount>> ListAsync(CancellationToken ct = default)
        => await WithCount(db.Branches.AsNoTracking().OrderBy(b => b.Name)).ToListAsync(ct);

    public Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default)
    {
        var lower = name.ToLower();
        return db.Branches.AnyAsync(b => b.Name.ToLower() == lower && (excludeId == null || b.Id != excludeId), ct);
    }

    public async Task AddAsync(Branch branch, CancellationToken ct = default)
        => await db.Branches.AddAsync(branch, ct);

    private IQueryable<BranchWithCount> WithCount(IQueryable<Branch> branches)
        => branches.Select(b => new BranchWithCount(b, db.Vehicles.Count(v => v.BranchId == b.Id)));
}
