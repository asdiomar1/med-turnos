namespace MedicalCenter.Infrastructure.Security;

public sealed record SecurityEvent(
    string EventType,
    string Message,
    string? UserId = null,
    string? Path = null,
    string? IpAddress = null,
    DateTimeOffset Timestamp = default)
{
    public DateTimeOffset Timestamp { get; init; } = Timestamp == default ? DateTimeOffset.UtcNow : Timestamp;
}
