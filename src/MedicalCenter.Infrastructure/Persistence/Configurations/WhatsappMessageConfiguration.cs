using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class WhatsappMessageConfiguration : IEntityTypeConfiguration<WhatsappMessage>
{
    public void Configure(EntityTypeBuilder<WhatsappMessage> builder)
    {
        builder.ToTable("whatsapp_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PatientId).HasColumnName("patient_id");
        builder.Property(x => x.SlotId).HasColumnName("slot_id");
        builder.Property(x => x.TandaId).HasColumnName("tanda_id");
        builder.Property(x => x.TemplateId).HasColumnName("template_id");
        builder.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.MetaMessageId).HasColumnName("meta_message_id").HasMaxLength(200);
        builder.Property(x => x.PhoneE164).HasColumnName("phone_e164").HasMaxLength(30).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.TriggerSource).HasColumnName("trigger_source").HasMaxLength(100);
        builder.Property(x => x.ErrorCode).HasColumnName("error_code").HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(x => x.RequestPayload).HasColumnName("request_payload").HasColumnType("jsonb");
        builder.Property(x => x.ResponsePayload).HasColumnName("response_payload").HasColumnType("jsonb");
        builder.Property(x => x.DeliveredAt).HasColumnName("delivered_at");
        builder.Property(x => x.ReadAt).HasColumnName("read_at");
        builder.Property(x => x.FailedAt).HasColumnName("failed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => new { x.PatientId, x.Kind, x.CreatedAt });
        builder.HasIndex(x => new { x.SlotId, x.Kind, x.CreatedAt });
        builder.HasIndex(x => new { x.MetaMessageId, x.CreatedAt });
    }
}
