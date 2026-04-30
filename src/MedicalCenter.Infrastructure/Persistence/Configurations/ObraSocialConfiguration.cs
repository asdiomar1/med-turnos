using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class ObraSocialConfiguration : IEntityTypeConfiguration<ObraSocial>
{
    public void Configure(EntityTypeBuilder<ObraSocial> builder)
    {
        builder.ToTable("obras_sociales");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Activa).HasColumnName("activa");
        builder.Property(x => x.TieneConvenio).HasColumnName("tiene_convenio");
        builder.Property(x => x.Orden).HasColumnName("orden");
        builder.Property(x => x.Abreviatura).HasColumnName("abreviatura").HasMaxLength(50);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => x.Nombre).IsUnique(false);
    }
}
