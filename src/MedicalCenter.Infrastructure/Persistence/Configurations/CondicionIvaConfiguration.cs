using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class CondicionIvaConfiguration : IEntityTypeConfiguration<CondicionIva>
{
    public void Configure(EntityTypeBuilder<CondicionIva> builder)
    {
        builder.ToTable("condiciones_iva");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Activo).HasColumnName("activo");
        builder.Property(x => x.Orden).HasColumnName("orden");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => x.Nombre).IsUnique(false);
    }
}
