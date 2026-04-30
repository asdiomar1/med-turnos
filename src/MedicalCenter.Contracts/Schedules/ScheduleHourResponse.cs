using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Schedules;

public sealed class ScheduleHourResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("hora")]
    public string Hora { get; init; } = string.Empty;

    [JsonPropertyName("orden")]
    public int Orden { get; init; }

    [JsonPropertyName("activo")]
    public bool Activo { get; init; }
}
