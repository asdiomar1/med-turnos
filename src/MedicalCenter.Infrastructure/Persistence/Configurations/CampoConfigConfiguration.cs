using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class CampoConfigConfiguration : IEntityTypeConfiguration<CampoConfig>
{
    public void Configure(EntityTypeBuilder<CampoConfig> builder)
    {
        builder.ToTable("campos_config");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Orden).HasColumnName("orden");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => x.Orden);
    }
}
