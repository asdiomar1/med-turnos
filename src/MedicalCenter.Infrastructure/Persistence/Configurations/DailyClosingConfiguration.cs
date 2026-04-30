using MedicalCenter.Domain.Entities;
using MedicalCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class DailyClosingConfiguration : IEntityTypeConfiguration<DailyClosing>
{
    public void Configure(EntityTypeBuilder<DailyClosing> builder)
    {
        builder.ToTable("daily_closings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Fecha).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.DetallesJson).HasColumnType("jsonb");
        builder.Property(x => x.MotivoReapertura).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.ConfirmedAt);
        builder.Property(x => x.ReopenedAt);
        builder.HasIndex(x => x.Fecha).IsUnique();
    }
}
