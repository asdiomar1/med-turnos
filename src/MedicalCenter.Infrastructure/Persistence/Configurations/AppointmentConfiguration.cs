using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.ModalidadCobro).HasMaxLength(20).IsRequired().HasDefaultValue("particular");
        builder.Property(x => x.NumeroAutorizacion).HasMaxLength(100);
        builder.Property(x => x.EsBloqueCompleto);
        builder.Property(x => x.EsTanda);
        builder.Property(x => x.ReferidoTercero);
        builder.Property(x => x.IniciarNuevoCicloObraSocial);
        builder.Property(x => x.ConvenioCorroborado);
        builder.Property(x => x.EsNuevoIngreso);
        builder.Property(x => x.EsMonoxido);
        builder.Property(x => x.MonoxidoOrdenMedica);
        builder.Property(x => x.MonoxidoResumenClinico);
        builder.Property(x => x.MedicoUserId).HasColumnName("medico_user_id");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => new { x.ScheduleId, x.Fecha, x.Hora, x.Lugar }).IsUnique();
        builder.HasIndex(x => new { x.PatientId, x.Fecha, x.Hora });
        builder.HasIndex(x => x.TandaId);
        builder.HasIndex(x => new { x.Fecha, x.Hora, x.CameraId });
        builder.Property<uint>("xmin").IsRowVersion().HasColumnName("xmin");
    }
}
