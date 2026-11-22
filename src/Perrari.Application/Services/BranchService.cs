using FluentValidation;
using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Inventarios;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Application.Interfaces.Services;
using Perrari.Domain.Entities.Inventarios;

namespace Perrari.Application.Services;

public class BranchService(
    IBranchRepository branches,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IValidator<BranchInputDto> validator) : IBranchService
{
    private const string MsgNotFound = "Sucursal no encontrada.";
    private const string MsgDuplicate = "Ya existe una sucursal con ese nombre.";

    public async Task<IReadOnlyList<BranchDto>> ListAsync(CancellationToken ct = default)
        => (await branches.ListAsync(ct)).Select(ToDto).ToList();

    public async Task<BranchDto> GetAsync(Guid id, CancellationToken ct = default)
        => ToDto(await branches.GetWithCountAsync(id, ct) ?? throw new NotFoundException(MsgNotFound));

    public async Task<BranchDto> CreateAsync(BranchInputDto dto, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(dto, ct);

        if (await branches.NameExistsAsync(dto.Name.Trim(), null, ct))
            throw new ConflictException(MsgDuplicate);

        var branch = Branch.Create(dto.Name, dto.Address, dto.Phone, timeProvider.GetUtcNow().UtcDateTime);
        await branches.AddAsync(branch, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDto(new BranchWithCount(branch, 0));
    }

    public async Task<BranchDto> UpdateAsync(Guid id, BranchInputDto dto, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(dto, ct);

        var branch = await branches.GetByIdAsync(id, ct) ?? throw new NotFoundException(MsgNotFound);

        if (await branches.NameExistsAsync(dto.Name.Trim(), id, ct))
            throw new ConflictException(MsgDuplicate);

        branch.Update(dto.Name, dto.Address, dto.Phone);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDto(await branches.GetWithCountAsync(id, ct) ?? throw new NotFoundException(MsgNotFound));
    }

    private static BranchDto ToDto(BranchWithCount row)
        => new(row.Branch.Id, row.Branch.Name, row.Branch.Address, row.Branch.Phone, row.VehicleCount, row.Branch.CreatedAt);
}
