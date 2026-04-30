using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class DiasLaborablesConfigConfiguration : IEntityTypeConfiguration<DiasLaborablesConfig>
{
    public void Configure(EntityTypeBuilder<DiasLaborablesConfig> builder)
    {
        builder.ToTable("dias_laborables_config");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("key").HasMaxLength(20);
        builder.Property(x => x.DiasSemana).HasColumnName("dias_semana").HasColumnType("smallint[]").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
