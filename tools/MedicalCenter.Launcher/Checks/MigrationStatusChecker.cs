namespace MedicalCenter.Launcher.Checks;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MedicalCenter.Launcher.Shared;
using MedicalCenter.Launcher.Utils;

/// <summary>
/// Checks for pending Entity Framework migrations without executing them.
/// </summary>
public static class MigrationStatusChecker
{
    /// <summary>
    /// Gets the migration status for the Infrastructure project.
    /// This method ONLY reports pending migrations - it NEVER executes them.
    /// </summary>
    public static async Task<MigrationStatus> GetMigrationStatusAsync(string projectRoot)
    {
        var infraProjectPath = Path.Combine(projectRoot, "src", "MedicalCenter.Infrastructure");

        if (!Directory.Exists(infraProjectPath))
        {
            return new MigrationStatus(0, Array.Empty<string>(), false);
        }

        try
        {
            // Try JSON output first (more reliable to parse)
            var jsonResult = await ProcessRunner.RunAsync(
                "dotnet", "ef migrations list --project \"..\\src\\MedicalCenter.Infrastructure\" --output json",
                workingDirectory: projectRoot,
                timeoutMs: 30000);

            // If that didn't work, try text output
            if (jsonResult.ExitCode != 0 || string.IsNullOrWhiteSpace(jsonResult.Stdout))
            {
                var textResult = await ProcessRunner.RunAsync(
                    "dotnet", "ef migrations list --project \"..\\src\\MedicalCenter.Infrastructure\"",
                    workingDirectory: projectRoot,
                    timeoutMs: 30000);

                return ParseTextOutput(textResult.Stdout);
            }

            return ParseJsonOutput(jsonResult.Stdout);
        }
        catch (Exception)
        {
            // If EF is not available or project is not set up, assume no migrations needed
            return new MigrationStatus(0, Array.Empty<string>(), false);
        }
    }

    private static MigrationStatus ParseJsonOutput(string jsonOutput)
    {
        try
        {
            // Simple JSON parsing - look for "pendingMigrations" array
            var pendingMatch = Regex.Match(jsonOutput, @"""pendingMigrations"":\s*\[(.*?)\]", RegexOptions.Singleline);
            if (!pendingMatch.Success)
            {
                return new MigrationStatus(0, Array.Empty<string>(), false);
            }

            var pendingContent = pendingMatch.Groups[1].Value;

            // Extract migration names
            var names = Regex.Matches(pendingContent, @"""name"":\s*""([^""]+)""")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToArray();

            return new MigrationStatus(names.Length, names, names.Length > 0);
        }
        catch
        {
            return new MigrationStatus(0, Array.Empty<string>(), false);
        }
    }

    private static MigrationStatus ParseTextOutput(string textOutput)
    {
        try
        {
            var lines = textOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var pendingMigrations = new System.Collections.Generic.List<string>();

            bool inPendingSection = false;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Look for the "Pending migrations:" section
                if (trimmed.Contains("Pending migrations:"))
                {
                    inPendingSection = true;
                    continue;
                }

                if (inPendingSection && !string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("("))
                {
                    // This is a migration name
                    pendingMigrations.Add(trimmed);
                }
            }

            return new MigrationStatus(pendingMigrations.Count, pendingMigrations.ToArray(), pendingMigrations.Count > 0);
        }
        catch
        {
            return new MigrationStatus(0, Array.Empty<string>(), false);
        }
    }
}