namespace MedicalCenter.Application.DTOs;

public sealed record AdminEventFeedItemDto(
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
    string MetadataJson);

public sealed record AdminEventFeedActorOptionDto(
    Guid? Id,
    string Label);

public sealed record AdminEventFeedActionOptionDto(
    string Code,
    string Family,
    string Label);

public sealed record AdminEventFeedFilterOptionsDto(
    IReadOnlyCollection<AdminEventFeedActorOptionDto> Actors,
    IReadOnlyCollection<AdminEventFeedActionOptionDto> Actions);

public sealed record AdminEventFeedQuery(
    int Limit,
    DateTimeOffset? BeforeOccurredAt,
    long? BeforeId,
    Guid? ActorUserId,
    IReadOnlyCollection<string> ActionCodes,
    DateOnly? DateFrom,
    DateOnly? DateTo);
