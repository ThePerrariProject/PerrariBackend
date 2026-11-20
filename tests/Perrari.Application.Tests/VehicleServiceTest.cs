using Perrari.Application.Common;
using Perrari.Application.Common.Exceptions;
using Perrari.Application.DTOs.Inventarios;
using Perrari.Application.Services;
using Perrari.Application.Validators.Inventarios;
using Perrari.Domain.Entities.Inventarios;
using Perrari.Domain.Exceptions;
using Xunit;

namespace Perrari.Application.Tests;

public class VehicleServiceTests
{
    private static readonly DateTime Start = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeBranchRepository _branches = new();
    private readonly FakeVehicleRepository _vehicles;
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeCurrentUser _user = new();
    private readonly MutableTimeProvider _time = new(Start);
    private readonly Branch _maranello = Branch.Create("Maranello", null, null, Start);
    private readonly Branch _modena = Branch.Create("Modena", null, null, Start);
    private readonly VehicleService _service;

    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _luis = Guid.NewGuid();

    public VehicleServiceTests()
    {
        _branches.Store.AddRange([_maranello, _modena]);
        _vehicles = new FakeVehicleRepository(_branches);
        _vehicles.UserNames[_ana] = "ana";
        _vehicles.UserNames[_luis] = "luis";
        _user.UserId = _ana;

        _service = new VehicleService(
            _vehicles, _branches, _uow, _user, _time, new InventorySettings { LockMinutes = 30 },
            new CreateVehicleDtoValidator(), new UpdateVehicleDtoValidator(), new ChangeVehicleStatusDtoValidator(),
            new TransferVehicleDtoValidator(), new VehicleListQueryDtoValidator(), new MovementListQueryDtoValidator());
    }

    private Task<VehicleDetailDto> CreateAsync(string vin = "ZFF296GTB0000001A")
        => _service.CreateAsync(new CreateVehicleDto(vin, "Ferrari", "296 GTB", 2025, "Rosso", 250_000m, 320_000m, null, _maranello.Id));

    [Fact]
    public async Task Create_saves_the_vehicle_and_its_entry_movement_in_one_save()
    {
        var created = await CreateAsync();

        Assert.Equal(VehicleStatuses.Available, created.Status);
        Assert.Equal("Maranello", created.BranchName);
        var movement = Assert.Single(_vehicles.Movements);
        Assert.Equal(MovementTypes.Entry, movement.MovementType);
        Assert.Null(movement.OriginBranchId);
        Assert.Equal(_ana, movement.UserId);
        Assert.Equal(1, _uow.Saves);
    }

    [Fact]
    public async Task Create_normalizes_vin_and_rejects_duplicates()
    {
        await _service.CreateAsync(new CreateVehicleDto("zff296gtb0000001a", "F", "M", 2025, null, 10m, 20m, null, _maranello.Id));

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync("ZFF296GTB0000001A"));
    }

    [Fact]
    public async Task Create_with_unknown_branch_is_404_and_bad_vin_is_validation()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateAsync(new CreateVehicleDto("ZFF296GTB0000001A", "F", "M", 2025, null, 10m, 20m, null, Guid.NewGuid())));

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.CreateAsync(new CreateVehicleDto("CORTO", "F", "M", 2025, null, 10m, 20m, null, _maranello.Id)));
    }

    [Fact]
    public async Task Lock_succeeds_and_returns_the_expiry()
    {
        var v = await CreateAsync();

        var locked = await _service.LockAsync(v.Id);

        Assert.True(locked.IsLocked);
        Assert.Equal(_ana, locked.LockedByUserId);
        Assert.Equal("ana", locked.LockedByName);
        Assert.Equal(Start.AddMinutes(30), locked.LockedUntil);
    }

    [Fact]
    public async Task Second_user_cannot_lock_while_the_first_lock_is_active()
    {
        var v = await CreateAsync();
        await _service.LockAsync(v.Id);

        _user.UserId = _luis;
        _time.UtcNow = Start.AddMinutes(10);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.LockAsync(v.Id));
        Assert.Contains("ana", ex.Message);
    }

    [Fact]
    public async Task Lock_expires_and_another_user_can_take_it()
    {
        var v = await CreateAsync();
        await _service.LockAsync(v.Id);

        _user.UserId = _luis;
        _time.UtcNow = Start.AddMinutes(31);

        var locked = await _service.LockAsync(v.Id);
        Assert.Equal(_luis, locked.LockedByUserId);
    }

    [Fact]
    public async Task Same_user_locking_again_renews_the_window()
    {
        var v = await CreateAsync();
        await _service.LockAsync(v.Id);

        _time.UtcNow = Start.AddMinutes(20);
        var renewed = await _service.LockAsync(v.Id);

        Assert.Equal(Start.AddMinutes(50), renewed.LockedUntil);
    }

    [Fact]
    public async Task Cannot_lock_a_vehicle_that_is_not_available()
    {
        var v = await CreateAsync();
        await _service.ChangeStatusAsync(v.Id, new ChangeVehicleStatusDto(VehicleStatuses.InMaintenance));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.LockAsync(v.Id));
        Assert.Contains("no está disponible", ex.Message);
    }

    [Fact]
    public async Task Lock_of_unknown_vehicle_is_404()
        => await Assert.ThrowsAsync<NotFoundException>(() => _service.LockAsync(Guid.NewGuid()));

    [Fact]
    public async Task Unlock_by_owner_releases_it_and_by_another_user_is_rejected()
    {
        var v = await CreateAsync();
        await _service.LockAsync(v.Id);

        _user.UserId = _luis;
        await Assert.ThrowsAsync<ConflictException>(() => _service.UnlockAsync(v.Id));

        _user.UserId = _ana;
        await _service.UnlockAsync(v.Id);
        Assert.False((await _service.GetAsync(v.Id)).IsLocked);
    }

    [Fact]
    public async Task Unlock_is_idempotent_when_nothing_is_locked_or_the_lock_expired()
    {
        var v = await CreateAsync();
        await _service.UnlockAsync(v.Id); // nunca se aparto

        await _service.LockAsync(v.Id);
        _user.UserId = _luis;
        _time.UtcNow = Start.AddMinutes(45); // vencio el apartado de ana
        await _service.UnlockAsync(v.Id);    // no lanza
    }

    [Fact]
    public async Task Force_unlock_frees_anyones_lock_and_404s_for_unknown_ids()
    {
        var v = await CreateAsync();
        await _service.LockAsync(v.Id);

        _user.UserId = _luis;
        await _service.ForceUnlockAsync(v.Id);

        Assert.False((await _service.GetAsync(v.Id)).IsLocked);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.ForceUnlockAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Transfer_moves_the_vehicle_and_records_the_movement()
    {
        var v = await CreateAsync();
        var savesBefore = _uow.Saves;

        var moved = await _service.TransferAsync(v.Id, new TransferVehicleDto(_modena.Id));

        Assert.Equal(_modena.Id, moved.BranchId);
        Assert.Equal(2, _vehicles.Movements.Count);
        var transfer = _vehicles.Movements.Last();
        Assert.Equal(MovementTypes.Transfer, transfer.MovementType);
        Assert.Equal(_maranello.Id, transfer.OriginBranchId);
        Assert.Equal(_modena.Id, transfer.DestinationBranchId);
        Assert.Equal(savesBefore + 1, _uow.Saves);   // vehiculo y movimiento, un solo SaveChanges
    }

    [Fact]
    public async Task Transfer_to_same_branch_or_unknown_branch_is_rejected()
    {
        var v = await CreateAsync();

        await Assert.ThrowsAsync<DomainException>(() => _service.TransferAsync(v.Id, new TransferVehicleDto(_maranello.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.TransferAsync(v.Id, new TransferVehicleDto(Guid.NewGuid())));
        Assert.Single(_vehicles.Movements); // solo el ENTRY
    }

    [Fact]
    public async Task Transfer_and_status_change_are_blocked_while_another_user_holds_the_lock()
    {
        var v = await CreateAsync();
        await _service.LockAsync(v.Id);

        _user.UserId = _luis;
        await Assert.ThrowsAsync<ConflictException>(() => _service.TransferAsync(v.Id, new TransferVehicleDto(_modena.Id)));
        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.ChangeStatusAsync(v.Id, new ChangeVehicleStatusDto(VehicleStatuses.Decommissioned)));

        // El dueño del apartado si puede
        _user.UserId = _ana;
        await _service.TransferAsync(v.Id, new TransferVehicleDto(_modena.Id));
    }

    [Fact]
    public async Task Status_sold_is_rejected_by_the_validator()
    {
        var v = await CreateAsync();
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.ChangeStatusAsync(v.Id, new ChangeVehicleStatusDto(VehicleStatuses.Sold)));
    }

    [Fact]
    public async Task List_filters_by_branch_and_status()
    {
        await CreateAsync("ZFF296GTB0000001A");
        var second = await CreateAsync("ZFF296GTB0000002B");
        await _service.TransferAsync(second.Id, new TransferVehicleDto(_modena.Id));

        var inModena = await _service.ListAsync(new VehicleListQueryDto { BranchId = _modena.Id });
        Assert.Equal(1, inModena.TotalCount);

        var sold = await _service.ListAsync(new VehicleListQueryDto { Status = VehicleStatuses.Sold });
        Assert.Equal(0, sold.TotalCount);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.ListAsync(new VehicleListQueryDto { PageSize = 500 }));
    }

    [Fact]
    public async Task Movements_are_listed_for_an_existing_vehicle_only()
    {
        var v = await CreateAsync();
        var page = await _service.GetMovementsAsync(v.Id, new MovementListQueryDto());
        Assert.Equal(1, page.TotalCount);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetMovementsAsync(Guid.NewGuid(), new MovementListQueryDto()));
    }
}
