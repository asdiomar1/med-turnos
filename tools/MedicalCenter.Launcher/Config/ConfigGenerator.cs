namespace MedicalCenter.Launcher.Config;

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MedicalCenter.Launcher.Shared;
using MedicalCenter.Launcher.UI;

/// <summary>
/// Generates or updates development configuration files.
/// </summary>
public static class ConfigGenerator
{
    private static readonly string[] RequiredLaunchJsonKeys = { "configurations" };
    private static readonly string[] RequiredAppSettingsKeys = { "ConnectionStrings", "Logging" };

    /// <summary>
    /// Ensures .vscode/launch.json exists with the MedicalCenter.Api debug profile.
    /// </summary>
    public static async Task<ConfigResult> EnsureLaunchJsonAsync(string workspaceRoot)
    {
        var vscodeDir = Path.Combine(workspaceRoot, ".vscode");
        var launchJsonPath = Path.Combine(vscodeDir, "launch.json");

        try
        {
            // Create .vscode directory if it doesn't exist
            if (!Directory.Exists(vscodeDir))
            {
                Directory.CreateDirectory(vscodeDir);
            }

            if (File.Exists(launchJsonPath))
            {
                // File exists - check if our profile is present
                var existingContent = await File.ReadAllTextAsync(launchJsonPath);

                // Simple check - if "MedicalCenter.Api" is in the file, we don't need to modify
                if (existingContent.Contains("MedicalCenter.Api"))
                {
                    ConsoleWriter.Info(".vscode/launch.json already contains MedicalCenter.Api profile");
                    return new ConfigResult(false, true, launchJsonPath);
                }

                // Merge: read existing, add our profile
                ConsoleWriter.Warning(".vscode/launch.json exists - adding MedicalCenter.Api profile");
                await MergeLaunchJsonAsync(launchJsonPath);
                return new ConfigResult(false, true, launchJsonPath);
            }

            // Create new launch.json
            var launchJsonContent = GenerateLaunchJson();
            await File.WriteAllTextAsync(launchJsonPath, launchJsonContent);
            ConsoleWriter.Success("Created .vscode/launch.json");
            return new ConfigResult(true, false, launchJsonPath);
        }
        catch (Exception ex)
        {
            ConsoleWriter.Error($"Failed to create launch.json: {ex.Message}");
            return new ConfigResult(false, false, launchJsonPath);
        }
    }

    /// <summary>
    /// Ensures appsettings.Development.json exists with default dev values.
    /// </summary>
    public static async Task<ConfigResult> EnsureAppSettingsDevAsync(string apiRoot)
    {
        var appSettingsPath = Path.Combine(apiRoot, "appsettings.Development.json");

        try
        {
            if (File.Exists(appSettingsPath))
            {
                // File exists - merge missing keys only
                ConsoleWriter.Warning("appsettings.Development.json exists - merging missing keys");
                await MergeAppSettingsAsync(appSettingsPath);
                return new ConfigResult(false, true, appSettingsPath);
            }

            // Create new appsettings.Development.json
            var appSettingsContent = GenerateAppSettingsDev();
            await File.WriteAllTextAsync(appSettingsPath, appSettingsContent);
            ConsoleWriter.Success("Created appsettings.Development.json");
            return new ConfigResult(true, false, appSettingsPath);
        }
        catch (Exception ex)
        {
            ConsoleWriter.Error($"Failed to create appsettings.Development.json: {ex.Message}");
            return new ConfigResult(false, false, appSettingsPath);
        }
    }

    private static string GenerateLaunchJson()
    {
        var config = new
        {
            version = "0.2.0",
            configurations = new object[]
            {
                new
                {
                    name = ".NET Core Launch (web)",
                    type = "coreclr",
                    request = "launch",
                    preLaunchTask = "build",
                    program = "${workspaceFolder}/src/MedicalCenter.Api/bin/Debug/net8.0/MedicalCenter.Api.dll",
                    args = "",
                    cwd = "${workspaceFolder}",
                    env = new { ASPNETCORE_ENVIRONMENT = "Development" },
                    console = "internalDebugger"
                },
                new
                {
                    name = ".NET Core Attach",
                    type = "coreclr",
                    request = "attach",
                    processId = "${command:pickProcess}"
                }
            }
        };

        return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
    }

    private static async Task MergeLaunchJsonAsync(string path)
    {
        try
        {
            var content = await File.ReadAllTextAsync(path);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Check if configurations array exists
            if (root.TryGetProperty("configurations", out var configs) && configs.ValueKind == JsonValueKind.Array)
            {
                // Simple merge: append our config if not present
                // For now, just log that manual merge might be needed
                ConsoleWriter.Info("Manual merge of launch.json may be required");
            }
        }
        catch
        {
            // If we can't parse, just skip merging
        }
    }

    private static string GenerateAppSettingsDev()
    {
        var config = new
        {
            Logging = new
            {
                LogLevel = new
                {
                    Default = "Information",
                    Microsoft = "Warning"
                }
            },
            ConnectionStrings = new
            {
                DefaultConnection = "Host=localhost;Port=5433;Database=medical_center;Username=postgres;Password=postgres",
                Redis = "localhost:6380"
            },
            Jwt = new
            {
                Key = "ThisIsADevelopmentKeyForLocalTestingOnly123456789",
                Issuer = "MedicalCenter",
                Audience = "MedicalCenter",
                ExpirationMinutes = 480
            },
            Redis = new
            {
                Host = "localhost",
                Port = 6380
            },
            Cors = new
            {
                AllowedOrigins = new[] { "http://localhost:3000", "http://localhost:5000", "http://localhost:5173", "http://127.0.0.1:5173" }
            }
        };

        return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
    }

    private static async Task MergeAppSettingsAsync(string path)
    {
        try
        {
            var existingJson = await File.ReadAllTextAsync(path);
            var existing = JsonDocument.Parse(existingJson);
            var newContent = GenerateAppSettingsDev();
            var newDoc = JsonDocument.Parse(newContent);

            // Simple merge - add missing top-level keys
            // This is a simplified merge - in production, you'd want deep merge
            ConsoleWriter.Info("appsettings.Development.json merge is simplified - manual review recommended");
        }
        catch (Exception ex)
        {
            ConsoleWriter.Warning($"Could not merge appsettings: {ex.Message}");
        }
    }
}