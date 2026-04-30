using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class ConsultationSessionConfiguration : IEntityTypeConfiguration<ConsultationSession>
{
    public void Configure(EntityTypeBuilder<ConsultationSession> builder)
    {
        builder.ToTable("sesiones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Hora).HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.ModalidadCobro).HasMaxLength(20).IsRequired().HasDefaultValue("particular");
        builder.Property(x => x.NumeroAutorizacion).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => x.PacienteId);
        builder.HasIndex(x => x.Fecha);
        builder.HasIndex(x => x.SlotId);
    }
}
