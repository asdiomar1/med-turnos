using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class ConsultationSlotConfiguration : IEntityTypeConfiguration<ConsultationSlot>
{
    public void Configure(EntityTypeBuilder<ConsultationSlot> builder)
    {
        builder.ToTable("consultas_slots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Hora).HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.MotivoCancelacion).HasMaxLength(500);
        builder.Property(x => x.ObservacionesAdmin).HasMaxLength(4000);
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => new { x.Fecha, x.Hora }).IsUnique();
        builder.Property(x => x.MedicoUserId).HasColumnName("medico_user_id");
        builder.HasIndex(x => x.PacienteId);
        builder.HasIndex(x => x.MedicoId);
        builder.HasIndex(x => x.Estado);
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");
    }
}
