using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Telefono).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DocumentoIdentidad).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DocumentoIdentidadNormalizado).HasMaxLength(50);
        builder.Property(x => x.Nacionalidad).HasMaxLength(100);
        builder.Property(x => x.NumeroCredencialObraSocial).HasMaxLength(100);
        builder.Property(x => x.LoginIdentifier).HasMaxLength(100);
        builder.Property(x => x.Notas).HasMaxLength(4000);
        builder.Property(x => x.OptInSource).HasMaxLength(100);
        builder.Property(x => x.DatosExtra).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.HasIndex(x => x.DocumentoIdentidadNormalizado);
        builder.HasIndex(x => x.LoginIdentifier).IsUnique().HasFilter("\"LoginIdentifier\" IS NOT NULL");
    }
}
