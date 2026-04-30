using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class WhatsappTemplateConfiguration : IEntityTypeConfiguration<WhatsappTemplate>
{
    public void Configure(EntityTypeBuilder<WhatsappTemplate> builder)
    {
        builder.ToTable("whatsapp_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(50).IsRequired();
        builder.Property(x => x.MetaTemplateName).HasColumnName("meta_template_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.LanguageCode).HasColumnName("language_code").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Active).HasColumnName("active");
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.MetaTemplateName).IsUnique();
        builder.HasIndex(x => new { x.Kind, x.Active });
    }
}
