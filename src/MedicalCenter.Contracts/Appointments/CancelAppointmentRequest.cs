using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Appointments;

public sealed class CancelAppointmentRequest
{
    [JsonPropertyName("motivo")]
    public string? Motivo { get; init; }
}
