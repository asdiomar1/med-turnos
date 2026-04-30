using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Schedules;

public sealed class CameraResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("capacidad")]
    public int Capacidad { get; init; }

    [JsonPropertyName("activa")]
    public bool Activa { get; init; }
}
