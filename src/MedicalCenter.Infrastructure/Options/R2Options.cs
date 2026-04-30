namespace MedicalCenter.Infrastructure.Options;

public sealed class R2Options
{
    public const string SectionName = "R2";

    public string AccountId { get; init; } = string.Empty;
    public string AccessKeyId { get; init; } = string.Empty;
    public string SecretAccessKey { get; init; } = string.Empty;
    public string Bucket { get; init; } = "medicalcenter-imports-dev";
    public string Endpoint { get; init; } = string.Empty;
    public string Region { get; init; } = "auto";
    public int PresignTtlSeconds { get; init; } = 300;
}
