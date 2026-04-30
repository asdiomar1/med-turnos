using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Schedules;

public sealed class UpdateCameraRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;
    [JsonPropertyName("capacidad")]
    public int Capacidad { get; init; }
}
