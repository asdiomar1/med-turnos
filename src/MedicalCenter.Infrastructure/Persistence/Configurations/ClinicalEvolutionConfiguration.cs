using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class ClinicalEvolutionConfiguration : IEntityTypeConfiguration<ClinicalEvolution>
{
    public void Configure(EntityTypeBuilder<ClinicalEvolution> builder)
    {
        builder.ToTable("historia_clinica_evoluciones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PatientId).HasColumnName("paciente_id");
        builder.Property(x => x.ConsultaSlotId).HasColumnName("consulta_slot_id");
        builder.Property(x => x.MedicoId).HasColumnName("medico_id");
        builder.Property(x => x.MedicoUserId).HasColumnName("medico_user_id");
        builder.Property(x => x.AuthorProfileId).HasColumnName("autor_perfil_id");
        builder.Property(x => x.FechaClinica).HasColumnName("fecha_clinica");
        builder.Property(x => x.Titulo).HasColumnName("titulo");
        builder.Property(x => x.Nota).HasColumnName("nota").IsRequired();
        builder.Property(x => x.DiagnosticoImpresion).HasColumnName("diagnostico_impresion");
        builder.Property(x => x.Indicaciones).HasColumnName("indicaciones");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => new { x.PatientId, x.FechaClinica, x.CreatedAt }).HasDatabaseName("idx_historia_clinica_evoluciones_paciente_fecha");
    }
}
