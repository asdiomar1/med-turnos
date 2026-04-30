using System.Text.Json.Serialization;

namespace MedicalCenter.Contracts.Common;

public sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "ok";

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
