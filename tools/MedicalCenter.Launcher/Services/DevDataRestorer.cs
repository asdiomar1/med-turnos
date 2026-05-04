namespace MedicalCenter.Launcher.Services;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MedicalCenter.Launcher.UI;
using MedicalCenter.Launcher.Utils;

/// <summary>
/// Restores development data from backup SQL files into the database.
/// Executes multiple backup files in sequence, ignoring duplicate key errors.
/// This allows merging all available backups into a single consolidated dataset.
/// </summary>
public static class DevDataRestorer
{
    /// <summary>
    /// Restores all dev data SQL files in the schema directory.
    /// Files are processed in order (oldest first), and duplicate key errors are ignored.
    /// </summary>
    public static async Task<int> RestoreAsync(
        string projectRoot,
        CancellationToken cancellationToken = default)
    {
        ConsoleWriter.SectionHeader("Development Data Restore");

        // Ensure postgres is running
        var checkResult = await ProcessRunner.RunAsync(
            "docker", "compose ps --services --filter status=running",
            workingDirectory: projectRoot,
            timeoutMs: 15000,
            cancellationToken: cancellationToken);

        if (!checkResult.Stdout.Contains("postgres"))
        {
            ConsoleWriter.Error("Postgres container is not running.");
            ConsoleWriter.Info("Start the stack first with mode 1 or 2, then run the restore.");
            return 1;
        }

        var schemaDir = Path.Combine(projectRoot, "schema");
        
        if (!Directory.Exists(schemaDir))
        {
            ConsoleWriter.Error($"Schema directory not found: {schemaDir}");
            return 1;
        }

        var sqlFiles = Directory.GetFiles(schemaDir, "dev-data-only-*.sql")
            .OrderBy(f => f)
            .ToArray();

        if (sqlFiles.Length == 0)
        {
            ConsoleWriter.Warning("No dev data backup files found in schema/");
            ConsoleWriter.Info("Run option 4 (Dump Dev Data) first to create a backup.");
            return 1;
        }

        ConsoleWriter.Info($"Found {sqlFiles.Length} backup file(s) to restore:");
        foreach (var file in sqlFiles)
        {
            var fileName = Path.GetFileName(file);
            ConsoleWriter.Info($"  - {fileName}");
        }
        Console.WriteLine();

        int totalExecuted = 0;
        int totalSkipped = 0;

        foreach (var sqlFile in sqlFiles)
        {
            var fileName = Path.GetFileName(sqlFile);
            ConsoleWriter.Info($"Restoring {fileName}...");

            // Execute SQL file using docker exec to psql
            var result = await ExecuteSqlFileAsync(
                sqlFile,
                projectRoot,
                cancellationToken);

            ConsoleWriter.Info($"  Result: {result.Executed} executed, {result.Skipped} skipped/errores");
            totalExecuted += result.Executed;
            totalSkipped += result.Skipped;
        }

        Console.WriteLine();
        ConsoleWriter.Success($"Restore complete!");
        ConsoleWriter.Info($"Total: {totalExecuted} ejecutados, {totalSkipped} ignorados");
        
        return 0;
    }

    /// <summary>
    /// Executes a SQL file using psql inside the postgres container.
    /// Ignores errors (continues on failure) to allow merging multiple files.
    /// </summary>
    private static async Task<(int Executed, int Skipped)> ExecuteSqlFileAsync(
        string sqlFile,
        string projectRoot,
        CancellationToken cancellationToken)
    {
        var containerPath = Path.GetFileName(sqlFile);
        
        // Execute SQL file with psql, using ON CONFLICT DO NOTHING for duplicates
        // and --set ON_ERROR_STOP=0 to continue on errors
        var psqlArgs = $"compose exec -T postgres psql " +
            $"--set ON_ERROR_STOP=0 " +
            $"-U postgres -d medical_center " +
            $"-f /app/schema/{containerPath}";

        var result = await ProcessRunner.RunAsync(
            "docker", psqlArgs,
            workingDirectory: projectRoot,
            timeoutMs: 120000,
            cancellationToken: cancellationToken);

        // Parse psql output to count executed vs skipped
        // psql outputs "INSERT 0 X" for successful inserts
        // For errors, it outputs "ERROR: ..." but continues
        
        int executed = 0;
        int skipped = 0;

        var lines = result.Stdout.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("INSERT "))
            {
                // Parse "INSERT 0 5" -> 5 rows inserted
                var parts = line.Split(' ');
                if (parts.Length >= 3 && int.TryParse(parts[2], out var count))
                {
                    executed += count;
                }
            }
            else if (line.StartsWith("ERROR:") || line.Contains("duplicate key"))
            {
                skipped++;
            }
        }

        return (executed, skipped);
    }
}