namespace MedicalCenter.Launcher.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using MedicalCenter.Launcher.UI;
using MedicalCenter.Launcher.Utils;

/// <summary>
/// Runs the API either via dotnet run or docker-compose.
/// </summary>
public static class ApiRunner
{
    /// <summary>
    /// Runs the API project using dotnet run.
    /// Shows detailed error output on build failure.
    /// </summary>
    public static async Task<ProcessResult> RunDotNetAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        var args = "run --project \"src\\MedicalCenter.Api\" --no-build";

        // First build the project
        ConsoleWriter.Info("Building API...");
        var buildResult = await ProcessRunner.RunAsync(
            "dotnet", "build \"src\\MedicalCenter.Api\"",
            workingDirectory: projectPath,
            timeoutMs: 120000,
            cancellationToken: cancellationToken,
            onOutput: line => Console.WriteLine(line),
            onError: line => Console.WriteLine(line));

        if (buildResult.ExitCode != 0)
        {
            ConsoleWriter.Error("✗ Build failed.");
            PrintBuildFailureDetails(buildResult);
            return buildResult;
        }

        ConsoleWriter.Success("Build succeeded. Starting API...");
        ConsoleWriter.Info("API will be available at:");
        ConsoleWriter.Info("  - HTTP:  http://localhost:8090");
        ConsoleWriter.Info("  - HTTPS: https://localhost:7012");
        ConsoleWriter.Info("Press Ctrl+C to stop...");
        Console.WriteLine();

        // Then run
        return await ProcessRunner.RunAsync(
            "dotnet", args,
            workingDirectory: projectPath,
            timeoutMs: 0, // No timeout - run until user Ctrl+C
            cancellationToken: cancellationToken,
            onOutput: line => Console.WriteLine(line),
            onError: line => Console.WriteLine(line));
    }

    /// <summary>
    /// Runs the API service via docker-compose.
    /// </summary>
    public static async Task<ProcessResult> RunDockerAsync(
        string? composeFile = null,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        return await DockerComposeRunner.RunServiceAsync(
            "api",
            composeFile,
            workingDirectory);
    }

    /// <summary>
    /// Prints detailed build failure information.
    /// </summary>
    private static void PrintBuildFailureDetails(ProcessResult result)
    {
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
