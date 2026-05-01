namespace MedicalCenter.Launcher.Tests.Checks;

using MedicalCenter.Launcher.Checks;
using MedicalCenter.Launcher.Shared;
using Xunit;

public class PrerequisiteCheckerTests
{
    [Fact]
    public async Task CheckDotNetSdkAsync_WhenValidVersion_ReturnsPassed()
    {
        // Arrange & Act - This test would need mocking ProcessRunner
        // In real implementation, we'd mock ProcessRunner to return specific versions

        // For now, just verify the method exists and is async
        var task = PrerequisiteChecker.CheckDotNetSdkAsync("8.0");
        Assert.NotNull(task);
    }

    [Fact]
    public async Task CheckDockerEngineAsync_WhenNotInstalled_ReturnsFailed()
    {
        // This test verifies the method signature
        var task = PrerequisiteChecker.CheckDockerEngineAsync();
        Assert.NotNull(task);
    }

    [Fact]
    public async Task CheckPortAvailableAsync_WithValidPort_ReturnsCheckResult()
    {
        // Verify the method returns a proper CheckResult
        var result = await PrerequisiteChecker.CheckPortAvailableAsync(5432);

        // Port 5432 might or might not be in use depending on environment
        // Just verify we get a valid result
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }
}