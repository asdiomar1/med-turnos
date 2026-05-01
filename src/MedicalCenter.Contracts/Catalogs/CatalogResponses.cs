using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Catalogs;

public sealed class CondicionIvaResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("activo")]
    public bool Activo { get; init; }

    [JsonPropertyName("orden")]
    public int Orden { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ObraSocialResponse
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("activa")]
    public bool Activa { get; init; }

    [JsonPropertyName("tiene_convenio")]
    public bool TieneConvenio { get; init; }

    [JsonPropertyName("orden")]
    public int Orden { get; init; }

    [JsonPropertyName("abreviatura")]
    public string? Abreviatura { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateObraSocialRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("tiene_convenio")]
    public bool TieneConvenio { get; init; }

    [JsonPropertyName("abreviatura")]
    public string? Abreviatura { get; init; }
}

public sealed class UpdateObraSocialRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("tiene_convenio")]
    public bool TieneConvenio { get; init; }

    [JsonPropertyName("abreviatura")]
    public string? Abreviatura { get; init; }
}

public sealed class ToggleObraSocialActiveRequest
{
    [JsonPropertyName("activa")]
    public bool Activa { get; init; }
}

public sealed class ToggleObraSocialConvenioRequest
{
    [JsonPropertyName("tiene_convenio")]
    public bool TieneConvenio { get; init; }
}

public sealed class CreateCondicionIvaRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;
}

public sealed class UpdateCondicionIvaRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("orden")]
    public int Orden { get; init; }
}

public sealed class ToggleCondicionIvaActiveRequest
{
    [JsonPropertyName("activo")]
    public bool Activo { get; init; }
}
