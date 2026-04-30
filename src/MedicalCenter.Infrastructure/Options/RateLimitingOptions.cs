namespace MedicalCenter.Infrastructure.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int AuthPermitLimit { get; init; } = 5;
    public int AuthWindowSeconds { get; init; } = 60;
    public int GeneralPermitLimit { get; init; } = 100;
    public int GeneralWindowSeconds { get; init; } = 60;
    public int WebhookPermitLimit { get; init; } = 1000;
    public int WebhookWindowSeconds { get; init; } = 60;
}
