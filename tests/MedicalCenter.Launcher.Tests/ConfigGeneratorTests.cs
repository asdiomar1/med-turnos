namespace MedicalCenter.Launcher.Tests.Config;

using System;
using System.IO;
using System.Threading.Tasks;
using MedicalCenter.Launcher.Config;
using MedicalCenter.Launcher.Shared;
using Xunit;

public class ConfigGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LauncherTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task EnsureLaunchJsonAsync_WhenDirectoryMissing_CreatesFile()
    {
        // Arrange
        var vscodeDir = Path.Combine(_tempDir, ".vscode");

        // Act
        var result = await ConfigGenerator.EnsureLaunchJsonAsync(_tempDir);

        // Assert
        Assert.True(File.Exists(Path.Combine(vscodeDir, "launch.json")));
        Assert.True(result.Created);
    }

    [Fact]
    public async Task EnsureLaunchJsonAsync_WhenFileExists_ReturnsAlreadyExists()
    {
        // Arrange
        var launchJsonPath = Path.Combine(_tempDir, ".vscode", "launch.json");
        Directory.CreateDirectory(Path.GetDirectoryName(launchJsonPath)!);
        await File.WriteAllTextAsync(launchJsonPath, "{ \"version\": \"0.2.0\" }");

        // Act
        var result = await ConfigGenerator.EnsureLaunchJsonAsync(_tempDir);

        // Assert
        Assert.True(result.AlreadyExists);
        Assert.False(result.Created);
    }

    [Fact]
    public async Task EnsureAppSettingsDevAsync_WhenFileMissing_CreatesFile()
    {
        // Arrange - API root must exist for the method to work
        var apiRoot = Path.Combine(_tempDir, "api");
        Directory.CreateDirectory(apiRoot);

        // Act
        var result = await ConfigGenerator.EnsureAppSettingsDevAsync(apiRoot);

        // Assert
        var expectedPath = Path.Combine(apiRoot, "appsettings.Development.json");
        Assert.True(result.Created);
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public async Task EnsureAppSettingsDevAsync_WhenFileExists_ReturnsAlreadyExists()
    {
        // Arrange
        var apiRoot = Path.Combine(_tempDir, "api");
        Directory.CreateDirectory(apiRoot);
        var appSettingsPath = Path.Combine(apiRoot, "appsettings.Development.json");
        await File.WriteAllTextAsync(appSettingsPath, "{ \"Logging\": { } }");

        // Act
        var result = await ConfigGenerator.EnsureAppSettingsDevAsync(apiRoot);

        // Assert
        Assert.True(result.AlreadyExists);
    }
}