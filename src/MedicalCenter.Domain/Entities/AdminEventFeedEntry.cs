using MedicalCenter.Domain.Common;

namespace MedicalCenter.Domain.Entities;

public sealed record AdminEventFeedEntryCreateParams(
    long Id,
    DateTimeOffset OccurredAt,
    Guid? ActorUserId,
    string ActorLabel,
    string ActionCode,
    string ActionFamily,
    string EntityType,
    string EntityId,
    string? AgendaType,
    Guid? PacienteId,
    string? PacienteNombre,
    int? MedicoId,
    string? MedicoNombre,
    string Title,
    string Summary,
    string SourceSystem,
    string SourceRecordKey,
    string MetadataJson);

public sealed class AdminEventFeedEntry : Entity<long>
{
    private AdminEventFeedEntry() { }

    public AdminEventFeedEntry(AdminEventFeedEntryCreateParams p)
    {
        Id = p.Id;
        OccurredAt = p.OccurredAt;
        ActorUserId = p.ActorUserId;
        ActorLabel = p.ActorLabel;
        ActionCode = p.ActionCode;
        ActionFamily = p.ActionFamily;
        EntityType = p.EntityType;
        EntityId = p.EntityId;
        AgendaType = p.AgendaType;
        PacienteId = p.PacienteId;
        PacienteNombre = p.PacienteNombre;
        MedicoId = p.MedicoId;
        MedicoNombre = p.MedicoNombre;
        Title = p.Title;
        Summary = p.Summary;
        SourceSystem = p.SourceSystem;
        SourceRecordKey = p.SourceRecordKey;
        MetadataJson = p.MetadataJson;
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
