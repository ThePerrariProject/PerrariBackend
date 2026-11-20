using Perrari.Domain.Entities.Inventarios;
using Perrari.Domain.Exceptions;
using Xunit;

namespace Perrari.Domain.Tests;

public class VehicleTests
{
    private static readonly DateTime Now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid BranchA = Guid.NewGuid();
    private static readonly Guid BranchB = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static Vehicle NewVehicle(string vin = "zff296gtb0000001a")
        => Vehicle.Create(vin, "Ferrari", "296 GTB", 2025, "Rosso", 250_000m, 320_000m, 1_500m, BranchA, Now);

    [Fact]
    public void Create_normalizes_the_vin_and_starts_available()
    {
        var v = NewVehicle();
        Assert.Equal("ZFF296GTB0000001A", v.Vin);
        Assert.Equal(VehicleStatuses.Available, v.Status);
        Assert.False(v.IsLocked);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ZFF296GTB0000001")]    // 16
    [InlineData("ZFF296GTB0000001AB")]  // 18
    [InlineData("ZFF296GTB0000I01A")]   // contiene I
    [InlineData("ZFF296GTB0000O01A")]   // contiene O
    [InlineData("ZFF296GTB0000Q01A")]   // contiene Q
    [InlineData("ZFF296GTB000-001A")]   // simbolo
    public void Create_rejects_invalid_vins(string vin)
        => Assert.Throws<DomainException>(() => NewVehicle(vin));

    [Fact]
    public void Create_rejects_bad_prices_and_year()
    {
        Assert.Throws<DomainException>(() => Vehicle.Create("ZFF296GTB0000001A", "F", "M", 2025, null, 0m, 10m, null, BranchA, Now));
        Assert.Throws<DomainException>(() => Vehicle.Create("ZFF296GTB0000001A", "F", "M", 2025, null, 10m, 0m, null, BranchA, Now));
        Assert.Throws<DomainException>(() => Vehicle.Create("ZFF296GTB0000001A", "F", "M", 1900, null, 10m, 10m, null, BranchA, Now));
        Assert.Throws<DomainException>(() => Vehicle.Create("ZFF296GTB0000001A", "F", "M", 2040, null, 10m, 10m, null, BranchA, Now));
        Assert.Throws<DomainException>(() => Vehicle.Create("ZFF296GTB0000001A", "F", "M", 2025, null, 10m, 10m, 0m, BranchA, Now));
    }

    [Fact]
    public void Lock_is_active_only_inside_the_window()
    {
        var v = NewVehicle();
        Assert.False(v.IsLockActive(Now, 30));

        // El apartado lo hace el repositorio con un UPDATE; aqui se simula con reflexion
        typeof(Vehicle).GetProperty(nameof(Vehicle.IsLocked))!.SetValue(v, true);
        typeof(Vehicle).GetProperty(nameof(Vehicle.LockedAt))!.SetValue(v, Now);
        typeof(Vehicle).GetProperty(nameof(Vehicle.LockedByUserId))!.SetValue(v, UserId);

        Assert.True(v.IsLockActive(Now.AddMinutes(29), 30));
        Assert.False(v.IsLockActive(Now.AddMinutes(30), 30));   // vence justo a los 30
        Assert.False(v.IsLockedByOther(UserId, Now.AddMinutes(1), 30));
        Assert.True(v.IsLockedByOther(Guid.NewGuid(), Now.AddMinutes(1), 30));
        Assert.False(v.IsLockedByOther(Guid.NewGuid(), Now.AddMinutes(31), 30)); // ya expiro
    }

    [Fact]
    public void Transfer_changes_branch_and_returns_a_movement()
    {
        var v = NewVehicle();

        var movement = v.TransferTo(BranchB, UserId, Now.AddHours(1));

        Assert.Equal(BranchB, v.BranchId);
        Assert.Equal(Now.AddHours(1), v.UpdatedAt);
        Assert.Equal(MovementTypes.Transfer, movement.MovementType);
        Assert.Equal(BranchA, movement.OriginBranchId);
        Assert.Equal(BranchB, movement.DestinationBranchId);
        Assert.Equal(v.Id, movement.VehicleId);
    }

    [Fact]
    public void Transfer_to_the_same_branch_is_rejected()
        => Assert.Throws<DomainException>(() => NewVehicle().TransferTo(BranchA, UserId, Now));

    [Fact]
    public void Entry_movement_has_no_origin()
    {
        var m = VehicleMovement.Entry(Guid.NewGuid(), BranchA, UserId, Now);
        Assert.Null(m.OriginBranchId);
        Assert.Equal(MovementTypes.Entry, m.MovementType);
    }

    [Fact]
    public void Decommissioned_vehicle_cannot_be_transferred()
    {
        var v = NewVehicle();
        v.ChangeStatus(VehicleStatuses.Decommissioned, Now);
        Assert.Throws<DomainException>(() => v.TransferTo(BranchB, UserId, Now));
    }

    [Fact]
    public void Sold_status_cannot_be_set_by_hand()
        => Assert.Throws<DomainException>(() => NewVehicle().ChangeStatus(VehicleStatuses.Sold, Now));

    [Fact]
    public void Mark_as_sold_requires_available_and_freezes_the_vehicle()
    {
        var maintenance = NewVehicle();
        maintenance.ChangeStatus(VehicleStatuses.InMaintenance, Now);
        Assert.Throws<DomainException>(() => maintenance.MarkAsSold(Now));

        var v = NewVehicle();
        v.MarkAsSold(Now);
        Assert.Equal(VehicleStatuses.Sold, v.Status);
        Assert.Throws<DomainException>(() => v.ChangeStatus(VehicleStatuses.Available, Now));
        Assert.Throws<DomainException>(() => v.UpdateDetails("F", "M", 2025, null, 10m, null, Now));
    }

    [Fact]
    public void Leaving_available_releases_the_lock()
    {
        var v = NewVehicle();
        typeof(Vehicle).GetProperty(nameof(Vehicle.IsLocked))!.SetValue(v, true);
        typeof(Vehicle).GetProperty(nameof(Vehicle.LockedAt))!.SetValue(v, Now);
        typeof(Vehicle).GetProperty(nameof(Vehicle.LockedByUserId))!.SetValue(v, UserId);

        v.ChangeStatus(VehicleStatuses.InMaintenance, Now.AddMinutes(1));

        Assert.False(v.IsLocked);
        Assert.Null(v.LockedAt);
        Assert.Null(v.LockedByUserId);
    }

    [Fact]
    public void Update_details_changes_sale_price_but_not_purchase_price()
    {
        var v = NewVehicle();
        v.UpdateDetails("Ferrari", "Roma", 2026, "  Nero ", 300_000m, null, Now.AddDays(1));

        Assert.Equal("Roma", v.Model);
        Assert.Equal("Nero", v.Color);
        Assert.Equal(300_000m, v.SalePrice);
        Assert.Equal(250_000m, v.PurchasePrice);
        Assert.Null(v.RentDailyRate);
    }

    [Fact]
    public void Branch_requires_a_name_and_trims_optional_fields()
    {
        Assert.Throws<DomainException>(() => Branch.Create("  ", null, null, Now));

        var b = Branch.Create(" Maranello ", "  ", " 555 ", Now);
        Assert.Equal("Maranello", b.Name);
        Assert.Null(b.Address);
        Assert.Equal("555", b.Phone);
    }
}
