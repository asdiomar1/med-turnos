namespace MedicalCenter.Infrastructure.Options;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 6379;
    public string ConnectionString => $"{Host}:{Port}";
    public string InstanceName { get; init; } = "mc";
    public int DefaultTtlMinutes { get; init; } = 60;
}