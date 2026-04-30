using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Patients;

public sealed class PatientResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("telefono")]
    public string Telefono { get; init; } = string.Empty;

    [JsonPropertyName("documento_identidad")]
    public string DocumentoIdentidad { get; init; } = string.Empty;

    [JsonPropertyName("documento_identidad_normalizado")]
    public string? DocumentoIdentidadNormalizado { get; init; }

    [JsonPropertyName("nacionalidad")]
    public string? Nacionalidad { get; init; }

    [JsonPropertyName("condicion_iva_id")]
    public int CondicionIvaId { get; init; }

    [JsonPropertyName("obra_social_id")]
    public int? ObraSocialId { get; init; }

    [JsonPropertyName("numero_credencial_obra_social")]
    public string? NumeroCredencialObraSocial { get; init; }

    [JsonPropertyName("portal_habilitado")]
    public bool PortalHabilitado { get; init; }

    [JsonPropertyName("requiere_reset_portal")]
    public bool RequiereResetPortal { get; init; }

    [JsonPropertyName("login_identifier")]
    public string? LoginIdentifier { get; init; }

    [JsonPropertyName("claustrofobico")]
    public bool Claustrofobico { get; init; }

    [JsonPropertyName("notas")]
    public string? Notas { get; init; }

    [JsonPropertyName("datos_extra")]
    public object DatosExtra { get; init; } = new { };

    [JsonPropertyName("opt_in_whatsapp")]
    public bool OptInWhatsapp { get; init; }

    [JsonPropertyName("opt_in_source")]
    public string? OptInSource { get; init; }
}
