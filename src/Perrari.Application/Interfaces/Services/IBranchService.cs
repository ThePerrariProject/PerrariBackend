using Perrari.Application.DTOs.Inventarios;

namespace Perrari.Application.Interfaces.Services;

public interface IBranchService
{
    Task<IReadOnlyList<BranchDto>> ListAsync(CancellationToken ct = default);

    Task<BranchDto> GetAsync(Guid id, CancellationToken ct = default);

    Task<BranchDto> CreateAsync(BranchInputDto dto, CancellationToken ct = default);

    Task<BranchDto> UpdateAsync(Guid id, BranchInputDto dto, CancellationToken ct = default);
}
