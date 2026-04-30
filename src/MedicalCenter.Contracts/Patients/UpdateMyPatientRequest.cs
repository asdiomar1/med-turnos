using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Patients;

public sealed class UpdateMyPatientRequest
{
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;
    [JsonPropertyName("email")]
    public string? Email { get; init; }
    [JsonPropertyName("telefono")]
    public string Telefono { get; init; } = string.Empty;
}
