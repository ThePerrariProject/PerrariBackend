using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Perrari.Domain.Entities.Auditorias;
using Perrari.Domain.Entities.Security;

namespace Perrari.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.TableName).HasMaxLength(100).IsRequired();
        b.Property(x => x.RecordId).IsRequired();
        b.Property(x => x.Action).HasMaxLength(20).IsRequired();

        b.Property(x => x.OldValues).HasColumnType("jsonb");
        b.Property(x => x.NewValues).HasColumnType("jsonb");

        b.Property(x => x.IpAddress).HasMaxLength(45).IsRequired();
        b.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Índices forenses requeridos
        b.HasIndex(x => new { x.TableName, x.RecordId });
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.CreatedAt);
    }
}

