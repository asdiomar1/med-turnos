using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class WhatsappMessageSettingConfiguration : IEntityTypeConfiguration<WhatsappMessageSetting>
{
    public void Configure(EntityTypeBuilder<WhatsappMessageSetting> builder)
    {
        builder.ToTable("whatsapp_message_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("key").HasMaxLength(100);
        builder.Property(x => x.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.MessageText).HasColumnName("message_text").HasColumnType("text").IsRequired();
        builder.Property(x => x.Active).HasColumnName("active");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
