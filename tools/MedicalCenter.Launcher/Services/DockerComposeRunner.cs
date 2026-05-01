namespace MedicalCenter.Launcher.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MedicalCenter.Launcher.Utils;

/// <summary>
/// Runs docker-compose commands for starting/stopping services.
/// </summary>
public static class DockerComposeRunner
{
    /// <summary>
    /// Starts docker-compose services (optionally filtered by service names).
    /// </summary>
    public static async Task<ProcessResult> UpAsync(
        IEnumerable<string>? services = null,
        string? composeFile = null,
        string? workingDirectory = null,
        int timeoutMs = 120000)
    {
        var args = "compose up -d";

        if (services != null)
        {
            foreach (var service in services)
            {
                args += $" {service}";
            }
        }

        // Use -f only if explicitly provided and different from default
        // Otherwise rely on workingDirectory being set to the project root
        return await ProcessRunner.RunAsync("docker", args, workingDirectory, timeoutMs);
    }

    /// <summary>
    /// Stops all docker-compose services.
    /// </summary>
    public static async Task<ProcessResult> DownAsync(
        string? composeFile = null,
        string? workingDirectory = null,
        int timeoutMs = 60000)
    {
        var args = "compose down";

        return await ProcessRunner.RunAsync("docker", args, workingDirectory, timeoutMs);
    }

    /// <summary>
    /// Stops all docker-compose services AND removes named volumes (database data).
    /// Use this when you want a clean slate - destroys all data in volumes.
    /// </summary>
    public static async Task<ProcessResult> DownWithVolumesAsync(
        string? composeFile = null,
        string? workingDirectory = null,
        int timeoutMs = 60000)
    {
        var args = "compose down -v";

        return await ProcessRunner.RunAsync("docker", args, workingDirectory, timeoutMs);
    }

    /// <summary>
    /// Runs a specific docker-compose service (for API in docker mode).
    /// </summary>
    public static async Task<ProcessResult> RunServiceAsync(
        string serviceName,
        string? composeFile = null,
        string? workingDirectory = null,
        int timeoutMs = 0)
    {
        var args = $"compose up -d {serviceName}";

        return await ProcessRunner.RunAsync("docker", args, workingDirectory, timeoutMs);
    }
}