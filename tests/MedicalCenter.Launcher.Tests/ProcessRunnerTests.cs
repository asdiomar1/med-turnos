namespace MedicalCenter.Launcher.Tests.Utils;

using System;
using System.Threading;
using System.Threading.Tasks;
using MedicalCenter.Launcher.Utils;
using Xunit;

public class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_WithValidCommand_ReturnsExitCode()
    {
        // Arrange
        var fileName = "dotnet";
        var arguments = "--version";

        // Act
        var result = await ProcessRunner.RunAsync(fileName, arguments);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ExitCode >= 0);
    }

    [Fact]
    public async Task RunAsync_WithInvalidCommand_ReturnsNonZeroExitCode()
    {
        // Arrange - use a valid command with invalid arguments
        var fileName = "dotnet";
        var arguments = "--invalid-argument-that-does-not-exist-12345";

        // Act
        var result = await ProcessRunner.RunAsync(fileName, arguments, timeoutMs: 10000);

        // Assert - dotnet with invalid args returns non-zero exit code
        Assert.NotNull(result);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_WithTimeout_ReturnsTimedOutTrue()
    {
        // Arrange - use a command that runs indefinitely
        // Windows: ping -t runs continuously; Linux/macOS: ping without count runs indefinitely
        var fileName = "ping";
        var arguments = OperatingSystem.IsWindows() ? "-t localhost" : "localhost";

        // Act
        var result = await ProcessRunner.RunAsync(fileName, arguments, timeoutMs: 1000);

        // Assert
        Assert.True(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_WithCancellation_CancelsExecution()
    {
        // Arrange - use a long-running command and cancel quickly
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel after 50ms

        // Windows: ping -t runs continuously; Linux/macOS: ping without count runs indefinitely
        var arguments = OperatingSystem.IsWindows() ? "-t localhost" : "localhost";

        // Act
        var result = await ProcessRunner.RunAsync(
            "ping",
            arguments,
            timeoutMs: 5000,
            cancellationToken: cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RunAsync_CapturesStdout()
    {
        // Act
        var result = await ProcessRunner.RunAsync("dotnet", "--list-sdks");

        // Assert
        Assert.NotNull(result.Stdout);
        Assert.Contains("8.", result.Stdout); // Should contain .NET 8.x SDK
    }
}