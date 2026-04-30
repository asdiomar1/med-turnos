using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Common;

public sealed class GuidLookupResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("documento_identidad")]
    public string? DocumentoIdentidad { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("activo")]
    public bool? Activo { get; init; }
}

public sealed class IntLookupResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("extra")]
    public string? Extra { get; init; }

    [JsonPropertyName("activo")]
    public bool? Activo { get; init; }
}
