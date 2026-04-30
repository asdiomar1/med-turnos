using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class ClinicalHistoryConfiguration : IEntityTypeConfiguration<ClinicalHistory>
{
    public void Configure(EntityTypeBuilder<ClinicalHistory> builder)
    {
        builder.ToTable("historias_clinicas");
        builder.HasKey(x => x.PatientId);
        builder.Ignore(x => x.Id);
        builder.Property(x => x.PatientId).HasColumnName("paciente_id");
        builder.Property(x => x.Numero).HasColumnName("numero");
        builder.Property(x => x.Antecedentes).HasColumnName("antecedentes");
        builder.Property(x => x.Alergias).HasColumnName("alergias");
        builder.Property(x => x.MedicacionActual).HasColumnName("medicacion_actual");
        builder.Property(x => x.ObservacionesRelevantes).HasColumnName("observaciones_relevantes");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
