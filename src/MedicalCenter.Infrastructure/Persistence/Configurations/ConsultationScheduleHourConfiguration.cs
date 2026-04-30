using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class ConsultationScheduleHourConfiguration : IEntityTypeConfiguration<ConsultationScheduleHour>
{
    public void Configure(EntityTypeBuilder<ConsultationScheduleHour> builder)
    {
        builder.ToTable("consultas_horarios_config");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Hora).HasMaxLength(5).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => x.Hora).IsUnique();
        builder.HasIndex(x => x.Orden).IsUnique();
    }
}
