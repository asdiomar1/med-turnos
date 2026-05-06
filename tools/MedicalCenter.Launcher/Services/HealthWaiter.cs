namespace MedicalCenter.Launcher.Services;

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MedicalCenter.Launcher.UI;
using MedicalCenter.Launcher.Utils;

/// <summary>
/// Waits for Postgres and Redis to become healthy before launching the API.
/// </summary>
public static class HealthWaiter
{
    private const int DefaultTimeoutSeconds = 60;
    private const int PollIntervalMs = 2000;
    private const string DefaultHealthUrl = "http://localhost:8090/health/ready"; // NOSONAR S1075 — default value, overridable via parameter

    /// <summary>
    /// Waits for Postgres to become available.
    /// </summary>
    public static async Task<bool> WaitForPostgresAsync(
        string? connectionString = null,
        int timeoutSeconds = DefaultTimeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        ConsoleWriter.Info("Waiting for Postgres to be ready...");

        var host = "localhost";
        var port = 5433; // docker-compose maps 5433:5432

        // Extract from connection string if provided
        if (!string.IsNullOrEmpty(connectionString))
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
                    host = part.Trim()[5..];
                else if (part.Trim().StartsWith("Port=", StringComparison.OrdinalIgnoreCase) && int.TryParse(part.Trim()[5..], out var parsedPort))
                    port = parsedPort;
            }
        }

        return await WaitForPortAsync(host, port, timeoutSeconds, "Postgres", cancellationToken);
    }

    /// <summary>
    /// Waits for Redis to become available.
    /// </summary>
    public static async Task<bool> WaitForRedisAsync(
        string host = "localhost",
        int port = 6380, // docker-compose maps 6380:6379
        int timeoutSeconds = DefaultTimeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        ConsoleWriter.Info("Waiting for Redis to be ready...");

        return await WaitForPortAsync(host, port, timeoutSeconds, "Redis", cancellationToken);
    }

    /// <summary>
    /// Waits for a specific port to become available.
    /// </summary>
    private static async Task<bool> WaitForPortAsync(
        string host,
        int port,
        int timeoutSeconds,
        string serviceName,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var endTime = DateTime.UtcNow.Add(timeout);
        var spinnerIndex = 0;
        var spinner = new[] { '|', '/', '-', '\\' };

        while (DateTime.UtcNow < endTime)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                ConsoleWriter.Warning($"{serviceName} health check cancelled");
                return false;
            }

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);

                if (await Task.WhenAny(connectTask, Task.Delay(1000, cancellationToken)) == connectTask)
                {
                    ConsoleWriter.ClearSpinner();
                    ConsoleWriter.Success($"{serviceName} is ready on {host}:{port}");
                    return true;
                }
            }
            catch
            {
                // Connection failed, keep waiting
            }

            // Show spinner while waiting
            ConsoleWriter.WriteSpinner(spinner, spinnerIndex++, $"Waiting for {serviceName}...");
            await Task.Delay(PollIntervalMs, cancellationToken);
        }

        ConsoleWriter.ClearSpinner();
        ConsoleWriter.Error($"{serviceName} did not become ready within {timeoutSeconds} seconds");
        return false;
    }

    /// <summary>
    /// Waits for API health endpoint (optional additional check).
    /// </summary>
    public static async Task<bool> WaitForApiHealthAsync(
        string url = DefaultHealthUrl,
        int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        ConsoleWriter.Info("Checking API health endpoint...");

        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var endTime = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < endTime)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await httpClient.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    ConsoleWriter.Success("API is healthy and ready");
                    return true;
                }
            }
            catch
            {
                // Not ready yet
            }

            await Task.Delay(2000, cancellationToken);
        }

        ConsoleWriter.Warning($"API health check timed out after {timeoutSeconds}s (this may be normal if API uses lazy initialization)");
        return true; // Don't fail the launch - API might initialize lazily
    }
}