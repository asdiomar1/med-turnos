using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Appointments;

public sealed class AppointmentGroupResponse
{
    [JsonPropertyName("fecha")]
    public DateOnly Fecha { get; init; }
    [JsonPropertyName("slots")]
    public IReadOnlyCollection<AppointmentResponse> Slots { get; init; } = [];
}
