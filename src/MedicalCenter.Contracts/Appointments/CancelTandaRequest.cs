using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Appointments;

public sealed class CancelTandaRequest
{
    [JsonPropertyName("motivo")]
    public string? Motivo { get; init; }
}
