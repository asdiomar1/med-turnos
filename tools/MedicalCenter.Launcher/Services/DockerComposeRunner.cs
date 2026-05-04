namespace MedicalCenter.Launcher.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MedicalCenter.Launcher.UI;
using MedicalCenter.Launcher.Utils;

/// <summary>
/// Runs docker-compose commands for starting/stopping services.
/// Provides enhanced error output for debugging.
/// </summary>
public static class DockerComposeRunner
{
    /// <summary>
    /// Starts docker-compose services (optionally filtered by service names).
    /// Shows full output on failure.
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

        var result = await ProcessRunner.RunAsync("docker", args, workingDirectory, timeoutMs);

        if (result.ExitCode != 0)
        {
            PrintFailureDetails(args, result);
        }

        return result;
    }

    /// <summary>
    /// Stops all docker-compose services.
    /// Shows full output on failure.
    /// </summary>
    public static async Task<ProcessResult> DownAsync(
        string? composeFile = null,
        string? workingDirectory = null,
        int timeoutMs = 60000)
    {
        var args = "compose down";
        var result = await ProcessRunner.RunAsync("docker", args, workingDirectory, timeoutMs);

        if (result.ExitCode != 0)
        {
            PrintFailureDetails(args, result);
        }

        return result;
    }

    /// <summary>
    /// Stops all docker-compose services AND removes named volumes (database data).
    /// Use this when you want a clean slate - destroys all data in volumes.
    /// Shows full output on failure.
    /// </summary>
    public static async Task<ProcessResult> DownWithVolumesAsync(
        string? composeFile = null,
        string? workingDirectory = null,
        int timeoutMs = 60000)
    {
        var args = "compose down -v";
        var result = await ProcessRunner.RunAsync("docker", args, workingDirectory, timeoutMs);

        if (result.ExitCode != 0)
        {
            PrintFailureDetails(args, result);
        }

        return result;
    }

    /// <summary>
    /// Builds docker-compose services with plain progress output (shows full build logs).
    /// This is critical for seeing actual build errors.
    /// </summary>
    public static async Task<ProcessResult> BuildAsync(
        string? service = null,
        string? workingDirectory = null,
        int timeoutMs = 300000)
    {
        var args = "compose build --progress=plain";

        if (!string.IsNullOrWhiteSpace(service))
        {
            args += $" {service}";
        }

        return await ProcessRunner.RunAsync(
            "docker", 
            args, 
            workingDirectory, 
            timeoutMs,
            onOutput: line => Console.WriteLine(line),
            onError: line => Console.WriteLine(line));
    }

    /// <summary>
    /// Runs a specific docker-compose service (for API in docker mode).
    /// Shows full output on failure.
    /// </summary>
    public static async Task<ProcessResult> RunServiceAsync(
        string serviceName,
        string? composeFile = null,
        string? workingDirectory = null,
        int timeoutMs = 0)
    {
        var args = $"compose up -d {serviceName}";
        var result = await ProcessRunner.RunAsync("docker", args, workingDirectory, timeoutMs);

        if (result.ExitCode != 0)
        {
            PrintFailureDetails(args, result);
        }

        return result;
    }

    /// <summary>
    /// Prints detailed failure information in a formatted box.
    /// </summary>
    private static void PrintFailureDetails(string args, ProcessResult result)
    {
        ConsoleWriter.Error($"✗ Docker command failed: docker {args}");
        ConsoleWriter.Warning($"Exit code: {result.ExitCode}");
        
        if (!string.IsNullOrWhiteSpace(result.Stderr))
        {
            ConsoleWriter.Warning("Captured stderr:");
            ConsoleWriter.PrintBox(result.Stderr.Trim());
        }
        
        if (!string.IsNullOrWhiteSpace(result.Stdout) && result.Stdout != result.Stderr)
        {
            ConsoleWriter.Warning("Captured stdout:");
            ConsoleWriter.PrintBox(result.Stdout.Trim());
        }
    }
}
