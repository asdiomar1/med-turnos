using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Appointments;

public sealed class RescheduleAppointmentRequest
{
    [JsonPropertyName("target_slot_id")]
    public Guid TargetSlotId { get; init; }

    [JsonPropertyName("scope")]
    public string Scope { get; init; } = "normal";
}
