using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Schedules;

public sealed class DeleteScheduleHourRequest
{
    [JsonPropertyName("resoluciones")]
    public IReadOnlyCollection<string> Resoluciones { get; init; } = [];
    [JsonPropertyName("motivo")]
    public string? Motivo { get; init; }
}
