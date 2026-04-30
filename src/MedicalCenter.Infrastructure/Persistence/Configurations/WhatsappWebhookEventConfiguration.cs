using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class WhatsappWebhookEventConfiguration : IEntityTypeConfiguration<WhatsappWebhookEvent>
{
    public void Configure(EntityTypeBuilder<WhatsappWebhookEvent> builder)
    {
        builder.ToTable("whatsapp_webhook_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.MetaObject).HasColumnName("meta_object").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntryId).HasColumnName("entry_id").HasMaxLength(200);
        builder.Property(x => x.MessageId).HasColumnName("message_id").HasMaxLength(200);
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Processed).HasColumnName("processed");
        builder.Property(x => x.ProcessingError).HasColumnName("processing_error").HasMaxLength(1000);
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at");
        builder.HasIndex(x => new { x.ReceivedAt, x.Id });
        builder.HasIndex(x => x.MessageId);
    }
}
