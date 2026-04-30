using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Schedules;

public sealed class DeleteScheduleHourPreviewResponse
{
    [JsonPropertyName("slots_futuros")]
    public int SlotsFuturos { get; init; }
}
