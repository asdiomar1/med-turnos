using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class WhatsappDispatchQueueItemConfiguration : IEntityTypeConfiguration<WhatsappDispatchQueueItem>
{
    public void Configure(EntityTypeBuilder<WhatsappDispatchQueueItem> builder)
    {
        builder.ToTable("whatsapp_dispatch_queue");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PatientId).HasColumnName("patient_id");
        builder.Property(x => x.SlotId).HasColumnName("slot_id");
        builder.Property(x => x.TandaId).HasColumnName("tanda_id");
        builder.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(50).IsRequired();
        builder.Property(x => x.TemplateKey).HasColumnName("template_key").HasMaxLength(100).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.TriggerSource).HasColumnName("trigger_source").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Attempts).HasColumnName("attempts");
        builder.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(2000);
        builder.Property(x => x.LockedAt).HasColumnName("locked_at");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => new { x.SlotId, x.Kind });
        builder.HasIndex(x => new { x.TandaId, x.Kind });
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
    }
}
