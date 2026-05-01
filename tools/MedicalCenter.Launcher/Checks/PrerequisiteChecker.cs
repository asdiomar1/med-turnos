namespace MedicalCenter.Launcher.Checks;

using System;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MedicalCenter.Launcher.Shared;
using MedicalCenter.Launcher.Utils;

/// <summary>
/// Validates development prerequisites: .NET SDK, Docker engine, and port availability.
/// </summary>
public static class PrerequisiteChecker
{
    /// <summary>
    /// Checks if the required .NET SDK version is installed.
    /// </summary>
    public static async Task<CheckResult> CheckDotNetSdkAsync(string requiredVersion = "8.0")
    {
        try
        {
            var result = await ProcessRunner.RunAsync("dotnet", "--version");

            if (result.ExitCode != 0)
            {
                return new CheckResult(
                    false,
                    $".NET SDK not found",
                    "Install .NET 8.0 SDK from https://dotnet.microsoft.com/download");
            }

            var version = result.Stdout.Trim();
            if (IsVersionAtLeast(version, requiredVersion))
            {
                return new CheckResult(true, $".NET SDK {version} detected");
            }

            return new CheckResult(
                false,
                $".NET SDK {version} found, but {requiredVersion}+ required",
                $"Install .NET {requiredVersion} SDK from https://dotnet.microsoft.com/download");
        }
        catch (Exception ex)
        {
            return new CheckResult(
                false,
                $"Failed to check .NET SDK: {ex.Message}",
                "Install .NET 8.0 SDK from https://dotnet.microsoft.com/download");
        }
    }

    /// <summary>
    /// Checks if Docker engine is running (not just installed).
    /// </summary>
    public static async Task<CheckResult> CheckDockerEngineAsync()
    {
        try
        {
            // First check if docker command exists
            var versionResult = await ProcessRunner.RunAsync("docker", "--version");
            if (versionResult.ExitCode != 0)
            {
                return new CheckResult(
                    false,
                    "Docker CLI not found",
                    "Install Docker Desktop from https://www.docker.com/products/docker-desktop");
            }

            // Then check if engine is actually running via docker info
            var infoResult = await ProcessRunner.RunAsync("docker", "info", timeoutMs: 10000);
            if (infoResult.ExitCode != 0)
            {
                return new CheckResult(
                    false,
                    "Docker CLI installed but engine not running",
                    "Start Docker Desktop and wait for it to be ready");
            }

            return new CheckResult(true, "Docker engine is running");
        }
        catch (Exception ex)
        {
            return new CheckResult(
                false,
                $"Failed to check Docker: {ex.Message}",
                "Install Docker Desktop and ensure it's running");
        }
    }

    /// <summary>
    /// Checks if a port is available (not in use).
    /// </summary>
    public static async Task<CheckResult> CheckPortAvailableAsync(int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync("127.0.0.1", port);

            // Wait with timeout
            if (await Task.WhenAny(connectTask, Task.Delay(2000)) == connectTask)
            {
                // Port is open (in use)
                return new CheckResult(
                    false,
                    $"Port {port} is already in use",
                    $"Stop the process using port {port} or use a different port");
            }

            // Timeout means port is available
            return new CheckResult(true, $"Port {port} is available");
        }
        catch (SocketException)
        {
            // SocketException means port is available (connection refused)
            return new CheckResult(true, $"Port {port} is available");
        }
        catch (Exception ex)
        {
            return new CheckResult(
                false,
                $"Failed to check port {port}: {ex.Message}",
                null);
        }
    }

    private static bool IsVersionAtLeast(string installed, string required)
    {
        try
        {
            // Parse major.minor from installed version (e.g., "8.0.11" -> 8, 0)
            var parts = installed.Split('.');
            if (parts.Length < 2) return false;

            var major = int.Parse(parts[0]);
            var minor = int.Parse(parts[1]);

            var reqParts = required.Split('.');
            var reqMajor = int.Parse(reqParts[0]);
            var reqMinor = int.Parse(reqParts[1]);

            if (major > reqMajor) return true;
            if (major == reqMajor && minor >= reqMinor) return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}