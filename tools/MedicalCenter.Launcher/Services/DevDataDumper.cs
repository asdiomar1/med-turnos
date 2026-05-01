namespace MedicalCenter.Launcher.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MedicalCenter.Launcher.UI;
using MedicalCenter.Launcher.Utils;

/// <summary>
/// Dumps development data from the running postgres container into a
/// dev-data-only SQL file that is 100% compatible with the .NET stack.
/// </summary>
public static class DevDataDumper
{
    private static readonly string[] ExcludedTables =
    {
        "rbac_permissions",
        "rbac_roles",
        "rbac_role_permissions",
        "rbac_user_roles",
        "rbac_effective_permissions",
        "perfiles",
        "users",
        "__EFMigrationsHistory"
    };

    /// <summary>
    /// Runs pg_dump inside the postgres container and writes the output
    /// to schema/dev-data-only-YYYYMMDD-HHMMSS.sql in the project root.
    /// </summary>
    public static async Task<int> DumpAsync(
        string projectRoot,
        string? composeFile = null,
        CancellationToken cancellationToken = default)
    {
        ConsoleWriter.SectionHeader("Development Data Dump");

        // Ensure postgres is running
        var checkResult = await ProcessRunner.RunAsync(
            "docker", "compose ps --services --filter status=running",
            workingDirectory: projectRoot,
            timeoutMs: 15000,
            cancellationToken: cancellationToken);

        if (!checkResult.Stdout.Contains("postgres"))
        {
            ConsoleWriter.Error("Postgres container is not running.");
            ConsoleWriter.Info("Start the stack first with mode 1 or 2, then run the dump.");
            return 1;
        }

        var schemaDir = Path.Combine(projectRoot, "schema");
        Directory.CreateDirectory(schemaDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var outputFile = Path.Combine(schemaDir, $"dev-data-only-{timestamp}.sql");

        // Build pg_dump arguments
        var pgDumpArgs = new System.Text.StringBuilder();
        pgDumpArgs.Append("compose exec -T postgres pg_dump ");
        pgDumpArgs.Append("--data-only ");
        pgDumpArgs.Append("--no-owner ");
        pgDumpArgs.Append("--no-privileges ");
        pgDumpArgs.Append("--schema=public ");
        pgDumpArgs.Append("--inserts ");
        pgDumpArgs.Append("--rows-per-insert=1000 ");

        foreach (var table in ExcludedTables)
        {
            pgDumpArgs.Append($"--exclude-table={table} ");
        }

        pgDumpArgs.Append("-U postgres -d medical_center");

        ConsoleWriter.Info("Running pg_dump inside postgres container...");
        ConsoleWriter.Info($"Excluding {ExcludedTables.Length} system tables.");
        ConsoleWriter.Info($"Output: {outputFile}");

        var result = await ProcessRunner.RunAsync(
            "docker", pgDumpArgs.ToString(),
            workingDirectory: projectRoot,
            timeoutMs: 120000,
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
        {
            ConsoleWriter.Error($"pg_dump failed: {result.Stderr}");
            return 1;
        }

        // Write stdout to file
        await File.WriteAllTextAsync(outputFile, result.Stdout, cancellationToken);

        // Clean up old dev-data-only files (keep last 5)
        CleanupOldDumps(schemaDir);

        var lines = result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        ConsoleWriter.Success($"Dump complete: {outputFile}");
        ConsoleWriter.Info($"Generated {lines} lines of SQL.");
        ConsoleWriter.Info("This file is safe to load in Development mode — it contains only data, no schema, and excludes all system tables.");

        return 0;
    }

    private static void CleanupOldDumps(string schemaDir)
    {
        var files = Directory
            .GetFiles(schemaDir, "dev-data-only-*.sql")
            .OrderByDescending(f => f)
            .ToArray();

        if (files.Length <= 5) return;

        foreach (var oldFile in files.Skip(5))
        {
            try
            {
                File.Delete(oldFile);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
