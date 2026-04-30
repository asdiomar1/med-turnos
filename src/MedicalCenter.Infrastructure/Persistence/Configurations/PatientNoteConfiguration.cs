using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class PatientNoteConfiguration : IEntityTypeConfiguration<PatientNote>
{
    public void Configure(EntityTypeBuilder<PatientNote> builder)
    {
        builder.ToTable("notas_paciente");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PatientId).HasColumnName("paciente_id");
        builder.Property(x => x.AuthorId).HasColumnName("autor_id");
        builder.Property(x => x.Message).HasColumnName("mensaje").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => x.PatientId).HasDatabaseName("notas_paciente_paciente_id_idx");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("notas_paciente_created_at_idx");
    }
}
