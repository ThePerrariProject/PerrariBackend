using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Perrari.Domain.Entities.Inventarios;
using Perrari.Domain.Entities.Security;

namespace Perrari.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> b)
    {
        b.ToTable("branches");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Address).HasColumnType("text");
        b.Property(x => x.Phone).HasMaxLength(50);
    }
}

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.ToTable("vehicles", t =>
            t.HasCheckConstraint("ck_vehicles_status",
                "status IN ('AVAILABLE','IN_MAINTENANCE','DECOMMISSIONED','SOLD')"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Vin).HasMaxLength(17).IsRequired();
        b.HasIndex(x => x.Vin).IsUnique();
        b.Property(x => x.Brand).HasMaxLength(100).IsRequired();
        b.Property(x => x.Model).HasMaxLength(100).IsRequired();
        b.Property(x => x.Color).HasMaxLength(50);
        b.Property(x => x.PurchasePrice).HasPrecision(12, 2);
        b.Property(x => x.SalePrice).HasPrecision(12, 2);
        // Si en tu diccionario de datos la columna de renta se llama distinto, ajusta aqui con .HasColumnName("...")
        b.Property(x => x.RentDailyRate).HasPrecision(10, 2);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired().HasDefaultValue(VehicleStatuses.Available);
        b.Property(x => x.IsLocked).HasDefaultValue(false);

        // Concurrencia optimista: el UPDATE incluye "WHERE updated_at = <valor leido>"; si alguien cambio la fila, falla y el UnitOfWork lo vuelve 409
        b.Property(x => x.UpdatedAt).IsConcurrencyToken();

        b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.LockedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.Status, x.BranchId });
        b.HasIndex(x => x.BranchId);
    }
}

public class VehicleMovementConfiguration : IEntityTypeConfiguration<VehicleMovement>
{
    public void Configure(EntityTypeBuilder<VehicleMovement> b)
    {
        b.ToTable("vehicle_movements", t =>
            t.HasCheckConstraint("ck_vehicle_movements_type", "movement_type IN ('ENTRY','TRANSFER')"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.MovementType).HasMaxLength(50).IsRequired();

        b.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OriginBranch).WithMany().HasForeignKey(x => x.OriginBranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DestinationBranch).WithMany().HasForeignKey(x => x.DestinationBranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.VehicleId, x.MovedAt });
    }
}
