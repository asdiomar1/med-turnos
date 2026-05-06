using Microsoft.Extensions.Configuration;

namespace MedicalCenter.Infrastructure.Options;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";
    public string AdminIdentifier { get; init; } = "admin";
    public string AdminEmail { get; init; } = "admin@medicalcenter.local";

    [ConfigurationKeyName("AdminSecret")]
    public string AdminPassword { get; init; } = "set-via-env";
}
