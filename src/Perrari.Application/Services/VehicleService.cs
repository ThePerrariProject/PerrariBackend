using FluentValidation;
using Perrari.Application.Common;
using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Inventarios;
using Perrari.Application.Interfaces.Repositories;
using Perrari.Application.Interfaces.Services;
using Perrari.Domain.Entities.Inventarios;

namespace Perrari.Application.Services;

public class VehicleService(
    IVehicleRepository vehicles,
    IBranchRepository branches,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    TimeProvider timeProvider,
    InventorySettings settings,
    IValidator<CreateVehicleDto> createValidator,
    IValidator<UpdateVehicleDto> updateValidator,
    IValidator<ChangeVehicleStatusDto> statusValidator,
    IValidator<TransferVehicleDto> transferValidator,
    IValidator<VehicleListQueryDto> listValidator,
    IValidator<MovementListQueryDto> movementsValidator) : IVehicleService
{
    private const string MsgNotFound = "Vehículo no encontrado.";
    private const string MsgBranchNotFound = "Sucursal no encontrada.";

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    private int LockMinutes => settings.LockMinutes;

    private Guid CurrentUserId => currentUser.UserId ?? throw new UnauthorizedException("No se pudo identificar al usuario.");

    // ---------- Consultas ----------

    public async Task<PagedResult<VehicleListItemDto>> ListAsync(VehicleListQueryDto query, CancellationToken ct = default)
    {
        await listValidator.ValidateAndThrowAsync(query, ct);

        var now = Now;
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var (items, total) = await vehicles.SearchAsync(
            new VehicleSearchCriteria(search, query.BranchId, query.Status, query.Page, query.PageSize), ct);

        var rows = items.Select(r => ToListItem(r, now)).ToList();
        return new PagedResult<VehicleListItemDto>(rows, query.Page, query.PageSize, total);
    }

    public async Task<VehicleDetailDto> GetAsync(Guid id, CancellationToken ct = default)
        => ToDetail(await GetRowAsync(id, ct), Now);

    public async Task<PagedResult<VehicleMovementDto>> GetMovementsAsync(Guid id, MovementListQueryDto query, CancellationToken ct = default)
    {
        await movementsValidator.ValidateAndThrowAsync(query, ct);
        _ = await GetRowAsync(id, ct); // 404 si el vehiculo no existe

        var (items, total) = await vehicles.GetMovementsAsync(id, query.Page, query.PageSize, ct);
        var rows = items
            .Select(m => new VehicleMovementDto(
                m.Movement.Id, m.Movement.MovementType,
                m.Movement.OriginBranchId, m.OriginBranchName,
                m.Movement.DestinationBranchId, m.DestinationBranchName,
                m.Movement.UserId, m.UserName, m.Movement.MovedAt))
            .ToList();

        return new PagedResult<VehicleMovementDto>(rows, query.Page, query.PageSize, total);
    }

    // ---------- Alta y edicion ----------

    public async Task<VehicleDetailDto> CreateAsync(CreateVehicleDto dto, CancellationToken ct = default)
    {
        await createValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var userId = CurrentUserId;
        var vin = Vehicle.NormalizeVin(dto.Vin);

        if (await vehicles.VinExistsAsync(vin, ct))
            throw new ConflictException("Ya existe un vehículo con ese VIN.");

        if (!await branches.ExistsAsync(dto.BranchId, ct))
            throw new NotFoundException(MsgBranchNotFound);

        var vehicle = Vehicle.Create(
            vin, dto.Brand, dto.Model, dto.Year, dto.Color,
            dto.PurchasePrice, dto.SalePrice, dto.RentDailyRate, dto.BranchId, now);

        // Vehiculo y movimiento ENTRY se guardan en un solo SaveChanges (una transaccion)
        await vehicles.AddAsync(vehicle, ct);
        await vehicles.AddMovementAsync(VehicleMovement.Entry(vehicle.Id, dto.BranchId, userId, now), ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDetail(await GetRowAsync(vehicle.Id, ct), now);
    }

    public async Task<VehicleDetailDto> UpdateAsync(Guid id, UpdateVehicleDto dto, CancellationToken ct = default)
    {
        await updateValidator.ValidateAndThrowAsync(dto, ct);

        var vehicle = await GetEntityAsync(id, ct);
        vehicle.UpdateDetails(dto.Brand, dto.Model, dto.Year, dto.Color, dto.SalePrice, dto.RentDailyRate, Now);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDetail(await GetRowAsync(id, ct), Now);
    }

    public async Task<VehicleDetailDto> ChangeStatusAsync(Guid id, ChangeVehicleStatusDto dto, CancellationToken ct = default)
    {
        await statusValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var vehicle = await GetEntityAsync(id, ct);
        EnsureNotLockedByOther(vehicle, now);

        vehicle.ChangeStatus(dto.Status, now);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDetail(await GetRowAsync(id, ct), now);
    }

    public async Task<VehicleDetailDto> TransferAsync(Guid id, TransferVehicleDto dto, CancellationToken ct = default)
    {
        await transferValidator.ValidateAndThrowAsync(dto, ct);

        var now = Now;
        var userId = CurrentUserId;
        var vehicle = await GetEntityAsync(id, ct);
        EnsureNotLockedByOther(vehicle, now);

        if (!await branches.ExistsAsync(dto.DestinationBranchId, ct))
            throw new NotFoundException(MsgBranchNotFound);

        // El cambio de sucursal y el movimiento se guardan juntos: o quedan los dos o ninguno
        var movement = vehicle.TransferTo(dto.DestinationBranchId, userId, now);
        await vehicles.AddMovementAsync(movement, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ToDetail(await GetRowAsync(id, ct), now);
    }

    // ---------- Apartado ----------

    public async Task<VehicleDetailDto> LockAsync(Guid id, CancellationToken ct = default)
    {
        var now = Now;
        var userId = CurrentUserId;

        // Un solo UPDATE condicional decide quien gana: si dos usuarios llegan a la vez, la base solo deja pasar a uno
        var locked = await vehicles.TryLockAsync(id, userId, now, now.AddMinutes(-LockMinutes), ct);
        var row = await GetRowAsync(id, ct);

        if (!locked)
        {
            if (row.Vehicle.Status != VehicleStatuses.Available)
                throw new ConflictException("El vehículo no está disponible para la venta.");

            var who = row.LockedByName ?? "otro usuario";
            throw new ConflictException($"El vehículo está apartado por {who}. Intente de nuevo más tarde.");
        }

        return ToDetail(row, now);
    }

    public async Task UnlockAsync(Guid id, CancellationToken ct = default)
    {
        var now = Now;

        if (await vehicles.TryUnlockAsync(id, CurrentUserId, now, ct))
            return;

        var row = await GetRowAsync(id, ct); // 404 si no existe

        // Sin apartado vigente no hay nada que liberar; con apartado ajeno, no se permite
        if (row.Vehicle.IsLockedByOther(CurrentUserId, now, LockMinutes))
            throw new ConflictException("Solo quien apartó el vehículo puede liberarlo.");
    }

    public async Task ForceUnlockAsync(Guid id, CancellationToken ct = default)
    {
        if (!await vehicles.ForceUnlockAsync(id, Now, ct))
            throw new NotFoundException(MsgNotFound);
    }

    // ---------- Privados ----------

    private async Task<Vehicle> GetEntityAsync(Guid id, CancellationToken ct)
        => await vehicles.GetByIdAsync(id, ct) ?? throw new NotFoundException(MsgNotFound);

    private async Task<VehicleRow> GetRowAsync(Guid id, CancellationToken ct)
        => await vehicles.GetRowAsync(id, ct) ?? throw new NotFoundException(MsgNotFound);

    private void EnsureNotLockedByOther(Vehicle vehicle, DateTime now)
    {
        if (vehicle.IsLockedByOther(CurrentUserId, now, LockMinutes))
            throw new ConflictException("El vehículo está apartado por otro usuario. No se puede modificar ahora.");
    }

    private VehicleListItemDto ToListItem(VehicleRow r, DateTime now)
    {
        var v = r.Vehicle;
        return new VehicleListItemDto(
            v.Id, v.Vin, v.Brand, v.Model, v.Year, v.Color, v.SalePrice, v.Status,
            v.BranchId, r.BranchName, v.IsLockActive(now, LockMinutes), v.CreatedAt);
    }

    private VehicleDetailDto ToDetail(VehicleRow r, DateTime now)
    {
        var v = r.Vehicle;
        var active = v.IsLockActive(now, LockMinutes);

        return new VehicleDetailDto(
            v.Id, v.Vin, v.Brand, v.Model, v.Year, v.Color, v.PurchasePrice, v.SalePrice, v.RentDailyRate,
            v.Status, v.BranchId, r.BranchName,
            active,
            active ? v.LockedAt : null,
            active ? v.LockedAt!.Value.AddMinutes(LockMinutes) : null,
            active ? v.LockedByUserId : null,
            active ? r.LockedByName : null,
            v.CreatedAt, v.UpdatedAt);
    }
}
