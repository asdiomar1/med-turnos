namespace MedicalCenter.Launcher.Shared;

/// <summary>
/// Result of a prerequisite check.
/// </summary>
public record CheckResult(bool Passed, string Message, string? Remediation = null);

/// <summary>
/// Result of a configuration generation operation.
/// </summary>
public record ConfigResult(bool Created, bool AlreadyExists, string Path);

/// <summary>
/// Available launch modes for the local development environment.
/// </summary>
public enum LaunchMode
{
    /// <summary>Start all docker-compose services (api, postgres, redis) and wait for health.</summary>
    FullDocker,
    /// <summary>Start only postgres and redis, then run API via dotnet run.</summary>
    InfraOnly,
    /// <summary>Skip Docker entirely, assume local infrastructure, run API via dotnet run.</summary>
    LocalRun,
    /// <summary>Stop all running services: docker containers + local API process.</summary>
    Stop,
    /// <summary>Dump development data from running postgres into a compatible SQL file.</summary>
    DumpData,
    /// <summary>Restore all development data backups into the database (merge).</summary>
    RestoreData,
    /// <summary>Rebuild the Docker image for the API service.</summary>
    Rebuild,
    /// <summary>Quit the launcher.</summary>
    Quit
}

/// <summary>
/// Migration status information returned by the checker.
/// </summary>
public record MigrationStatus(int Pending, string[] PendingNames, bool HasPending);