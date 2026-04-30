using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class BlockHistoryConfiguration : IEntityTypeConfiguration<BlockHistory>
{
    public void Configure(EntityTypeBuilder<BlockHistory> builder)
    {
        builder.ToTable("historial_bloques");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Hora).HasColumnType("time without time zone").IsRequired();
        builder.Property(x => x.Accion).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ModalidadCobro).HasMaxLength(20).IsRequired().HasDefaultValue("particular");
        builder.Property(x => x.NumeroAutorizacion).HasMaxLength(100);
        builder.Property(x => x.Motivo).HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(x => new { x.Fecha, x.Hora, x.CamaraId });
        builder.HasIndex(x => x.SlotId);
        builder.HasIndex(x => x.TandaId);
    }
}
