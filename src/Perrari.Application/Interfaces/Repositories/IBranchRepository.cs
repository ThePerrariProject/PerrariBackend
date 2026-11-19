using Perrari.Domain.Entities.Inventarios;

namespace Perrari.Application.Interfaces.Repositories;

public record BranchWithCount(Branch Branch, int VehicleCount);

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    Task<BranchWithCount?> GetWithCountAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<BranchWithCount>> ListAsync(CancellationToken ct = default);

    /// <summary>Compara sin distinguir mayusculas.</summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken ct = default);

    Task AddAsync(Branch branch, CancellationToken ct = default);
}
