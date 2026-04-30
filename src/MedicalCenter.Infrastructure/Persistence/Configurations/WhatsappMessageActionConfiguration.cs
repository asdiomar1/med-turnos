using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class WhatsappMessageActionConfiguration : IEntityTypeConfiguration<WhatsappMessageAction>
{
    public void Configure(EntityTypeBuilder<WhatsappMessageAction> builder)
    {
        builder.ToTable("whatsapp_message_actions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PatientId).HasColumnName("patient_id");
        builder.Property(x => x.SlotId).HasColumnName("slot_id");
        builder.Property(x => x.WhatsappMessageId).HasColumnName("whatsapp_message_id");
        builder.Property(x => x.ActionKind).HasColumnName("action_kind").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        builder.Property(x => x.PhoneE164).HasColumnName("phone_e164").HasMaxLength(30).IsRequired();
        builder.Property(x => x.IncomingMessageId).HasColumnName("incoming_message_id").HasMaxLength(200);
        builder.Property(x => x.ConfirmedIncomingMessageId).HasColumnName("confirmed_incoming_message_id").HasMaxLength(200);
        builder.Property(x => x.RawContext).HasColumnName("raw_context").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => new { x.WhatsappMessageId, x.CreatedAt });
        builder.HasIndex(x => new { x.PhoneE164, x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.SlotId, x.CreatedAt });
    }
}
