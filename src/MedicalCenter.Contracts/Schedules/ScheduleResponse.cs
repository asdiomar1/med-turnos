using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Schedules;

public sealed class ScheduleResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }

    [JsonPropertyName("hora")]
    public TimeOnly Hora { get; init; }

    [JsonPropertyName("lugar")]
    public int Lugar { get; init; }

    [JsonPropertyName("agenda_key")]
    public string AgendaKey { get; init; } = string.Empty;
}
