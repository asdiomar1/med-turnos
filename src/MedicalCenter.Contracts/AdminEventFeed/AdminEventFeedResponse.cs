using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.AdminEventFeed;

public sealed class AdminEventFeedItemResponse
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("occurred_at")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("actor_user_id")]
    public Guid? ActorUserId { get; init; }

    [JsonPropertyName("actor_label")]
    public string ActorLabel { get; init; } = string.Empty;

    [JsonPropertyName("action_code")]
    public string ActionCode { get; init; } = string.Empty;

    [JsonPropertyName("action_family")]
    public string ActionFamily { get; init; } = string.Empty;

    [JsonPropertyName("entity_type")]
    public string EntityType { get; init; } = string.Empty;

    [JsonPropertyName("entity_id")]
    public string EntityId { get; init; } = string.Empty;

    [JsonPropertyName("agenda_type")]
    public string? AgendaType { get; init; }

    [JsonPropertyName("paciente_id")]
    public Guid? PacienteId { get; init; }

    [JsonPropertyName("paciente_nombre")]
    public string? PacienteNombre { get; init; }

    [JsonPropertyName("medico_id")]
    public int? MedicoId { get; init; }

    [JsonPropertyName("medico_nombre")]
    public string? MedicoNombre { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("metadata")]
    public JsonElement Metadata { get; init; }
}

public sealed class AdminEventFeedFilterOptionsResponse
{
    [JsonPropertyName("actors")]
    public IReadOnlyCollection<AdminEventFeedActorOptionResponse> Actors { get; init; } = [];

    [JsonPropertyName("actions")]
    public IReadOnlyCollection<AdminEventFeedActionOptionResponse> Actions { get; init; } = [];
}

public sealed class AdminEventFeedActorOptionResponse
{
    [JsonPropertyName("id")]
    public Guid? Id { get; init; }

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;
}

public sealed class AdminEventFeedActionOptionResponse
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("family")]
    public string Family { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;
}

public sealed class AdminEventActionCodeResponse
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("family")]
    public string Family { get; init; } = string.Empty;

    [JsonPropertyName("entity_type")]
    public string EntityType { get; init; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; init; } = string.Empty;
}
