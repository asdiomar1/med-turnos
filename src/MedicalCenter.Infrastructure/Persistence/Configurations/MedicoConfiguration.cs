using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class MedicoConfiguration : IEntityTypeConfiguration<Medico>
{
    public void Configure(EntityTypeBuilder<Medico> builder)
    {
        builder.ToTable("medicos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Activo).HasColumnName("activo");
        builder.Property(x => x.Orden).HasColumnName("orden");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.PerfilId).HasColumnName("perfil_id");
        builder.HasIndex(x => x.Nombre).IsUnique();
    }
}
