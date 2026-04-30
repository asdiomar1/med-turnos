using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Configuration;

public sealed class CampoConfigResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;

    [JsonPropertyName("orden")]
    public int Orden { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateCampoConfigRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;
}

public sealed class UpdateCampoConfigRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;
}
