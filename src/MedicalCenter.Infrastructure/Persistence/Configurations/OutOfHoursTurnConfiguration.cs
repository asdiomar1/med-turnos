using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class OutOfHoursTurnConfiguration : IEntityTypeConfiguration<OutOfHoursTurn>
{
    public void Configure(EntityTypeBuilder<OutOfHoursTurn> builder)
    {
        builder.ToTable("turnos_fuera_horario");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Hora).HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.Notas).HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => new { x.Fecha, x.Hora, x.PacienteId }).IsUnique();
        builder.HasIndex(x => x.PacienteId);
    }
}
