using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.WhatsApp;

public sealed class WhatsappDispatchRequest
{
    [JsonPropertyName("slot_ids")]
    public IReadOnlyCollection<Guid> SlotIds { get; init; } = [];

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }
}

public sealed class WhatsappDispatchResponse
{
    [JsonPropertyName("requested")]
    public int Requested { get; init; }

    [JsonPropertyName("found")]
    public int Found { get; init; }
}

public sealed class WhatsappReminderRequest
{
    [JsonPropertyName("fecha_objetivo")]
    public DateOnly? FechaObjetivo { get; init; }
}

public sealed class WhatsappReminderResponse
{
    [JsonPropertyName("fecha_objetivo")]
    public DateOnly FechaObjetivo { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }
}
