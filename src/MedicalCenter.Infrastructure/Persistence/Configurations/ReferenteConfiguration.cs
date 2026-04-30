using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class ReferenteConfiguration : IEntityTypeConfiguration<Referente>
{
    public void Configure(EntityTypeBuilder<Referente> builder)
    {
        builder.ToTable("referentes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Activo).HasColumnName("activo");
        builder.Property(x => x.Orden).HasColumnName("orden");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => new { x.Nombre, x.Tipo }).IsUnique();
    }
}
