using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class ScheduleHourConfiguration : IEntityTypeConfiguration<ScheduleHour>
{
    public void Configure(EntityTypeBuilder<ScheduleHour> builder)
    {
        builder.ToTable("horarios_config");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Hora).HasMaxLength(5).IsRequired();
        builder.HasIndex(x => x.Hora).IsUnique();
        builder.HasIndex(x => x.Orden).IsUnique();
    }
}
