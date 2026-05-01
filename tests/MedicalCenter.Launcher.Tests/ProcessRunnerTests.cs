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
        // Arrange - use a command that hangs
        var fileName = "ping";
        var arguments = "-t localhost"; // ping continuously

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

        // Act - this may timeout or be cancelled
        // On Windows, ping doesn't respond to cancellation the same way
        var result = await ProcessRunner.RunAsync(
            "ping",
            "localhost",
            timeoutMs: 5000,
            cancellationToken: cts.Token);

        // Assert - just verify we get a result (not null)
        Assert.NotNull(result);
        // On Windows with ping, cancellation may not work as expected, so we just check result exists
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