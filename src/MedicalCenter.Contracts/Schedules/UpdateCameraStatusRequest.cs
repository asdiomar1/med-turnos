using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Schedules;

public sealed class UpdateCameraStatusRequest
{
    [JsonPropertyName("activa")]
    public bool Activa { get; init; }
}
