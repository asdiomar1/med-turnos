using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Schedules;

public sealed class UpdateScheduleHourRequest
{
    [JsonPropertyName("hora")]
    public string Hora { get; init; } = string.Empty;
    [JsonPropertyName("orden")]
    public int Orden { get; init; }
}
