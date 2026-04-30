using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Schedules;

public sealed class CameraMutationResponse
{
    [JsonPropertyName("camara")]
    public CameraResponse Camara { get; init; } = new();
    [JsonPropertyName("movidos")]
    public int Movidos { get; init; }
    [JsonPropertyName("cancelados")]
    public int Cancelados { get; init; }
    [JsonPropertyName("apartados_liberados")]
    public int ApartadosLiberados { get; init; }
    [JsonPropertyName("eliminados")]
    public int Eliminados { get; init; }
}
