using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Appointments;

public sealed class ReleaseHeldAppointmentRequest
{
    [JsonPropertyName("motivo")]
    public string? Motivo { get; init; }
}
