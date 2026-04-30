using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed class AdminEventFeedEntry : Entity<long>
{
    private AdminEventFeedEntry() { }

    public AdminEventFeedEntry(
        long id,
        DateTimeOffset occurredAt,
        Guid? actorUserId,
        string actorLabel,
        string actionCode,
        string actionFamily,
        string entityType,
        string entityId,
        string? agendaType,
        Guid? pacienteId,
        string? pacienteNombre,
        int? medicoId,
        string? medicoNombre,
        string title,
        string summary,
        string sourceSystem,
        string sourceRecordKey,
        string metadataJson)
    {
        Id = id;
        OccurredAt = occurredAt;
        ActorUserId = actorUserId;
        ActorLabel = actorLabel;
        ActionCode = actionCode;
        ActionFamily = actionFamily;
        EntityType = entityType;
        EntityId = entityId;
        AgendaType = agendaType;
        PacienteId = pacienteId;
        PacienteNombre = pacienteNombre;
        MedicoId = medicoId;
        MedicoNombre = medicoNombre;
        Title = title;
        Summary = summary;
        SourceSystem = sourceSystem;
        SourceRecordKey = sourceRecordKey;
        MetadataJson = metadataJson;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string ActorLabel { get; private set; } = "Sistema";
    public string ActionCode { get; private set; } = string.Empty;
    public string ActionFamily { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? AgendaType { get; private set; }
    public Guid? PacienteId { get; private set; }
    public string? PacienteNombre { get; private set; }
    public int? MedicoId { get; private set; }
    public string? MedicoNombre { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string SourceSystem { get; private set; } = string.Empty;
    public string SourceRecordKey { get; private set; } = string.Empty;
    public string MetadataJson { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }
}
