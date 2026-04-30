using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Professionals;

public sealed class ProfessionalResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; init; }
}
