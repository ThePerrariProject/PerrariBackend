using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Perrari.Domain.Entities.Clientes;

namespace Perrari.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.ToTable("clients");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Name).HasMaxLength(255).IsRequired();
        b.Property(x => x.Rfc).HasMaxLength(13).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        b.Property(x => x.Email).HasMaxLength(255).IsRequired();
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => x.Rfc).IsUnique();
        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => x.Name);
    }
}

