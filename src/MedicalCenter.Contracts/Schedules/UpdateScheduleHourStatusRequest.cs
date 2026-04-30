using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Schedules;

public sealed class UpdateScheduleHourStatusRequest
{
    [JsonPropertyName("activo")]
    public bool Activo { get; init; }
}
