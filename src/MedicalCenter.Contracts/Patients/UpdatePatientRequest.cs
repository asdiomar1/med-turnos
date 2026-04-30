using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Patients;

public sealed class UpdatePatientRequest
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }
    [JsonPropertyName("telefono")]
    public string Telefono { get; init; } = string.Empty;
    [JsonPropertyName("documento_identidad")]
    public string DocumentoIdentidad { get; init; } = string.Empty;
    [JsonPropertyName("nacionalidad")]
    public string? Nacionalidad { get; init; }
    [JsonPropertyName("condicion_iva_id")]
    public int CondicionIvaId { get; init; }
    [JsonPropertyName("obra_social_id")]
    public int? ObraSocialId { get; init; }
    [JsonPropertyName("numero_credencial_obra_social")]
    public string? NumeroCredencialObraSocial { get; init; }
    [JsonPropertyName("claustrofobico")]
    public bool Claustrofobico { get; init; }
    [JsonPropertyName("notas")]
    public string? Notas { get; init; }
    [JsonPropertyName("datos_extra")]
    public object? DatosExtra { get; init; }
    [JsonPropertyName("actualizar_notas")]
    public bool ActualizarNotas { get; init; } = true;
    [JsonPropertyName("opt_in_whatsapp")]
    public bool OptInWhatsapp { get; init; }
    [JsonPropertyName("opt_in_source")]
    public string? OptInSource { get; init; }
}
