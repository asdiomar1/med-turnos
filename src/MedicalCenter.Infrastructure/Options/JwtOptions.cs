namespace MedicalCenter.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = "MedicalCenter";
    public string Audience { get; init; } = "MedicalCenter.Client";
    public string SecretKey { get; init; } = "change-this-secret-in-production-with-at-least-32-chars";
    public int AccessTokenExpirationMinutes { get; init; } = 30;
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
