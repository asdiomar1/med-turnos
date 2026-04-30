using MedicalCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalCenter.Infrastructure.Persistence.Configurations;

public sealed class AdminEventFeedEntryConfiguration : IEntityTypeConfiguration<AdminEventFeedEntry>
{
    public void Configure(EntityTypeBuilder<AdminEventFeedEntry> builder)
    {
        builder.ToTable("admin_event_feed");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd().HasColumnName("id");
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.ActorLabel).HasColumnName("actor_label").HasMaxLength(200);
        builder.Property(x => x.ActionCode).HasColumnName("action_code").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ActionFamily).HasColumnName("action_family").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(100).IsRequired();
        builder.Property(x => x.AgendaType).HasColumnName("agenda_type").HasMaxLength(50);
        builder.Property(x => x.PacienteId).HasColumnName("paciente_id");
        builder.Property(x => x.PacienteNombre).HasColumnName("paciente_nombre").HasMaxLength(200);
        builder.Property(x => x.MedicoId).HasColumnName("medico_id");
        builder.Property(x => x.MedicoNombre).HasColumnName("medico_nombre").HasMaxLength(200);
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(250).IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(500).IsRequired();
        builder.Property(x => x.SourceSystem).HasColumnName("source_system").HasMaxLength(50).IsRequired();
        builder.Property(x => x.SourceRecordKey).HasColumnName("source_record_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => new { x.OccurredAt, x.Id });
        builder.HasIndex(x => new { x.SourceSystem, x.SourceRecordKey }).IsUnique();
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.ActionCode);
    }
}
