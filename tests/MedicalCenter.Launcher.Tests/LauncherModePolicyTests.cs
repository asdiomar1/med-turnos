namespace MedicalCenter.Launcher.Tests.Shared;

using MedicalCenter.Launcher.Shared;
using Xunit;

public class LauncherModePolicyTests
{
    [Theory]
    [InlineData(LaunchMode.FullDocker)]
    [InlineData(LaunchMode.InfraOnly)]
    [InlineData(LaunchMode.LocalRun)]
    [InlineData(LaunchMode.Rebuild)]
    public void RequiresPrerequisiteChecks_WhenModeNeedsInfra_ReturnsTrue(LaunchMode mode)
    {
        Assert.True(LauncherModePolicy.RequiresPrerequisiteChecks(mode));
    }

    [Theory]
    [InlineData(LaunchMode.DumpData)]
    [InlineData(LaunchMode.RestoreData)]
    [InlineData(LaunchMode.Stop)]
    [InlineData(LaunchMode.Quit)]
    public void RequiresPrerequisiteChecks_WhenModeDoesNotNeedInfra_ReturnsFalse(LaunchMode mode)
    {
        Assert.False(LauncherModePolicy.RequiresPrerequisiteChecks(mode));
    }
}
