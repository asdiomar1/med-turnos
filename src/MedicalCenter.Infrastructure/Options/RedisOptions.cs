namespace MedicalCenter.Infrastructure.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = "localhost:6379";
    public string InstanceName { get; init; } = "mc";
    public int DefaultTtlMinutes { get; init; } = 60;
}