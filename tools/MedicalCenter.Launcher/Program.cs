namespace MedicalCenter.Launcher;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MedicalCenter.Launcher.Checks;
using MedicalCenter.Launcher.Config;
using MedicalCenter.Launcher.Services;
using MedicalCenter.Launcher.Shared;
using MedicalCenter.Launcher.UI;
using MedicalCenter.Launcher.Utils;

class Program
{
    private static readonly string ProjectRoot = FindProjectRoot();
    private static readonly string ApiRoot = System.IO.Path.Combine(ProjectRoot, "src", "MedicalCenter.Api");
    private static readonly string ComposeFile = System.IO.Path.Combine(ProjectRoot, "docker-compose.yml");

    static async Task<int> Main(string[] args)
    {
        // If args provided, run in one-shot mode (CLI usage)
        if (args.Length > 0)
        {
            var mode = ParseMode(args);
            if (mode == null)
            {
                return 0; // --help was shown
            }

            if (mode.Value == LaunchMode.Quit)
            {
                return 0;
            }

            return await ExecuteModeAsync(mode.Value, skipReturnPrompt: true);
        }

        // Interactive loop mode
        while (true)
        {
            Console.Clear();
            ConsoleWriter.SectionHeader("Medical Center Local Dev Launcher");

            var mode = ShowInteractiveMenu();

            if (mode == LaunchMode.Quit)
            {
                ConsoleWriter.Info("Goodbye.");
                return 0;
            }

            Console.Clear();
            ConsoleWriter.SectionHeader("Medical Center Local Dev Launcher");

            var exitCode = await ExecuteModeAsync(mode, skipReturnPrompt: false);

            if (exitCode != 0)
            {
                ConsoleWriter.Error("Operation finished with errors.");
            }

            ConsoleWriter.Info("");
            ConsoleWriter.Info("Press Enter to return to menu...");
            Console.ReadLine();
        }
    }

    private static async Task<int> ExecuteModeAsync(LaunchMode mode, bool skipReturnPrompt)
    {
        // Skip checks for modes that don't need them
        var needsChecks = mode is LaunchMode.FullDocker
            or LaunchMode.InfraOnly
            or LaunchMode.LocalRun
            or LaunchMode.Rebuild;

        if (needsChecks)
        {
            ConsoleWriter.SectionHeader("Prerequisite Checks");

            var dotnetCheck = await PrerequisiteChecker.CheckDotNetSdkAsync("8.0");
            ConsoleWriter.PrintCheckResult(".NET SDK 8.0", dotnetCheck.Passed, dotnetCheck.Passed ? "" : dotnetCheck.Message);

            var dockerCheck = await PrerequisiteChecker.CheckDockerEngineAsync();
            ConsoleWriter.PrintCheckResult("Docker Engine", dockerCheck.Passed, dockerCheck.Passed ? "" : dockerCheck.Message);

            var ports = new[] { 5433, 6380, 8090 };
            foreach (var port in ports)
            {
                var portCheck = await PrerequisiteChecker.CheckPortAvailableAsync(port);
                ConsoleWriter.PrintCheckResult($"Port {port}", portCheck.Passed, portCheck.Passed ? "" : portCheck.Message);
            }

            if (!dotnetCheck.Passed || !dockerCheck.Passed)
            {
                ConsoleWriter.Error("Prerequisites not met. Exiting.");
                return 1;
            }

            ConsoleWriter.SectionHeader("Configuration");
            await ConfigGenerator.EnsureLaunchJsonAsync(ProjectRoot);
            await ConfigGenerator.EnsureAppSettingsDevAsync(ApiRoot);

            ConsoleWriter.SectionHeader("Database Migrations");
            var migrationStatus = await MigrationStatusChecker.GetMigrationStatusAsync(ProjectRoot);
            if (migrationStatus.HasPending)
            {
                ConsoleWriter.Warning($"Found {migrationStatus.Pending} pending migration(s):");
                foreach (var name in migrationStatus.PendingNames)
                {
                    ConsoleWriter.Info($"  - {name}");
                }
                ConsoleWriter.Info("Migrations will be applied by API seed middleware on startup");
            }
            else
            {
                ConsoleWriter.Success("No pending migrations");
            }

            ConsoleWriter.SectionHeader("Launch Mode");
        }

        return mode switch
        {
            LaunchMode.FullDocker => await RunFullDockerMode(),
            LaunchMode.InfraOnly => await RunInfraOnlyMode(),
            LaunchMode.LocalRun => await RunLocalRunMode(),
            LaunchMode.DumpData => await RunDumpDataMode(),
            LaunchMode.Stop => await RunStopMode(),
            LaunchMode.Rebuild => await RunRebuildMode(),
            LaunchMode.Quit => 0,
            _ => 1
        };
    }

    private static LaunchMode? ParseMode(string[] args)
    {
        var arg = args[0].ToLowerInvariant();

        return arg switch
        {
            "--help" or "-h" => ShowHelp(),
            "1" or "full" => LaunchMode.FullDocker,
            "2" or "infra" => LaunchMode.InfraOnly,
            "3" or "local" => LaunchMode.LocalRun,
            "4" or "dump" => LaunchMode.DumpData,
            "5" or "rebuild" => LaunchMode.Rebuild,
            "s" or "stop" => LaunchMode.Stop,
            "q" or "quit" => LaunchMode.Quit,
            _ => ShowInvalidOption()
        };
    }

    private static LaunchMode? ShowHelp()
    {
        Console.WriteLine(@"
Usage: MedicalCenter.Launcher [mode]

Modes:
  1, full    - Full Docker Compose (api + postgres + redis)
  2, infra   - Infra Only (postgres + redis, then dotnet run)
  3, local   - Local Run (skip Docker, dotnet run)
  4, dump    - Dump dev data from running postgres to schema/
  5, rebuild - Rebuild Docker image for the API service
  s, stop    - Stop docker containers + kill local API process
  q, quit    - Quit the launcher

If no mode is provided, an interactive menu will be shown.
");
        return null;
    }

    private static LaunchMode? ShowInvalidOption()
    {
        ConsoleWriter.Error("Invalid mode. Use --help for usage information.");
        return null;
    }

    private static LaunchMode ShowInteractiveMenu()
    {
        Console.WriteLine(@"
Select launch mode:

  [1] Full Docker Compose    - Start all services (api, postgres, redis)
  [2] Infra Only             - Start postgres + redis, then dotnet run
  [3] Local Run              - Skip Docker, run with dotnet (assumes local infra)
  [4] Dump Dev Data          - Export dev data from running postgres to schema/
  [5] Rebuild Docker Image   - Rebuild the API Docker image

  [S] Stop all               - Stop docker containers + kill local API process
  [Q] Quit
");

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim().ToLowerInvariant();

            switch (input)
            {
                case "1": return LaunchMode.FullDocker;
                case "2": return LaunchMode.InfraOnly;
                case "3": return LaunchMode.LocalRun;
                case "4": return LaunchMode.DumpData;
                case "5": return LaunchMode.Rebuild;
                case "s": return LaunchMode.Stop;
                case "q": return LaunchMode.Quit;
                default:
                    ConsoleWriter.Warning("Invalid option. Enter 1, 2, 3, 4, 5, S, or Q.");
                    break;
            }
        }
    }

    private static async Task<int> RunFullDockerMode()
    {
        ConsoleWriter.Info("Mode: Full Docker Compose");

        ConsoleWriter.Info("Stopping existing containers and removing volumes (clean slate)...");
        await DockerComposeRunner.DownWithVolumesAsync(composeFile: ComposeFile, workingDirectory: ProjectRoot);

        ConsoleWriter.Info("Starting all services (api, postgres, redis)...");
        var upResult = await DockerComposeRunner.UpAsync(
            new[] { "api", "postgres", "redis" },
            composeFile: ComposeFile,
            workingDirectory: ProjectRoot);

        if (upResult.ExitCode != 0)
        {
            ConsoleWriter.Error($"Failed to start services: {upResult.Stderr}");
            return 1;
        }

        if (!await HealthWaiter.WaitForPostgresAsync()) return 1;
        if (!await HealthWaiter.WaitForRedisAsync()) return 1;

        ConsoleWriter.Info("Starting API service...");
        await ApiRunner.RunDockerAsync(composeFile: ComposeFile, workingDirectory: ProjectRoot);

        return 0;
    }

    private static async Task<int> RunInfraOnlyMode()
    {
        ConsoleWriter.Info("Mode: Infra Only");

        ConsoleWriter.Info("Stopping existing containers and removing volumes (clean slate)...");
        await DockerComposeRunner.DownWithVolumesAsync(composeFile: ComposeFile, workingDirectory: ProjectRoot);

        ConsoleWriter.Info("Starting postgres and redis...");
        var upResult = await DockerComposeRunner.UpAsync(
            new[] { "postgres", "redis" },
            composeFile: ComposeFile,
            workingDirectory: ProjectRoot);

        if (upResult.ExitCode != 0)
        {
            ConsoleWriter.Error($"Failed to start infra: {upResult.Stderr}");
            return 1;
        }

        if (!await HealthWaiter.WaitForPostgresAsync()) return 1;
        if (!await HealthWaiter.WaitForRedisAsync()) return 1;

        ConsoleWriter.SectionHeader("Infrastructure Ready");
        ConsoleWriter.Success("Postgres and Redis are running!");
        ConsoleWriter.Info("To debug with breakpoints:");
        ConsoleWriter.Info("  1. Open 'MedicalCenter.Api' project in Visual Studio");
        ConsoleWriter.Info("  2. Set 'MedicalCenter.Api' as startup project");
        ConsoleWriter.Info("  3. Press F5 to start debugging");
        ConsoleWriter.Info("");
        ConsoleWriter.Info("When running from Visual Studio (F5), API will be at:");
        ConsoleWriter.Info("  - HTTP:  http://localhost:8090");
        ConsoleWriter.Info("  - HTTPS: https://localhost:7012");
        ConsoleWriter.Info("");
        ConsoleWriter.Info("Press Enter to stop infrastructure and exit...");
        Console.ReadLine();

        await DockerComposeRunner.DownAsync(composeFile: ComposeFile, workingDirectory: ProjectRoot);
        return 0;
    }

    private static async Task<int> RunLocalRunMode()
    {
        ConsoleWriter.Info("Mode: Local Run (skipping Docker)");

        var appSettingsPath = System.IO.Path.Combine(ApiRoot, "appsettings.Development.json");
        if (!System.IO.File.Exists(appSettingsPath))
        {
            ConsoleWriter.Error("appsettings.Development.json not found.");
            ConsoleWriter.Info("Run the launcher in Full or Infra mode first to generate it, or create it manually.");
            return 1;
        }

        ConsoleWriter.Info("Starting API via dotnet run...");
        ConsoleWriter.Info("Press Ctrl+C to stop...");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            ConsoleWriter.Warning("Shutting down...");
            cts.Cancel();
        };

        await ApiRunner.RunDotNetAsync(ProjectRoot, cts.Token);
        return 0;
    }

    private static async Task<int> RunDumpDataMode()
    {
        return await DevDataDumper.DumpAsync(ProjectRoot, ComposeFile);
    }

    private static async Task<int> RunRebuildMode()
    {
        ConsoleWriter.Info("Mode: Rebuild Docker Image");
        ConsoleWriter.Info("Rebuilding API Docker image...");

        // Docker Compose sends progress messages (including "Built" success) to stderr.
        // We print both stdout and stderr as plain text to avoid showing ✗ on progress lines.
        var result = await ProcessRunner.RunAsync(
            "docker", "compose build api",
            workingDirectory: ProjectRoot,
            timeoutMs: 300000,
            onOutput: line => Console.WriteLine(line),
            onError: line => Console.WriteLine(line));

        if (result.ExitCode == 0)
        {
            ConsoleWriter.Success("Docker image rebuilt successfully.");
        }
        else
        {
            ConsoleWriter.Error($"Docker build failed with exit code {result.ExitCode}.");
        }

        return result.ExitCode;
    }

    private static readonly int[] AllKnownPorts = { 5433, 6380, 8090, 5245, 7012 };

    private static async Task<int> RunStopMode()
    {
        ConsoleWriter.SectionHeader("Stopping All Services");

        ConsoleWriter.Info("Stopping docker containers and removing volumes...");
        var downResult = await DockerComposeRunner.DownWithVolumesAsync(
            composeFile: ComposeFile,
            workingDirectory: ProjectRoot,
            timeoutMs: 60000);

        if (downResult.ExitCode == 0)
            ConsoleWriter.Success("Docker containers stopped.");
        else
            ConsoleWriter.Warning($"docker compose down exited with code {downResult.ExitCode} — containers may not have been running.");

        ConsoleWriter.Info("Checking local processes on known ports...");
        var killed = false;
        foreach (var port in AllKnownPorts)
        {
            var wasKilled = await ProcessKiller.KillProcessOnPortAsync(port);
            if (wasKilled)
            {
                ConsoleWriter.Success($"Killed process on port {port}.");
                killed = true;
            }
        }

        if (!killed)
            ConsoleWriter.Info("No local processes found on known ports.");

        ConsoleWriter.Success("Done.");
        return 0;
    }

    private static string FindProjectRoot()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        while (current != null && !System.IO.File.Exists(System.IO.Path.Combine(current, "MedicalCenter.sln")))
        {
            var parent = System.IO.Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return current ?? System.IO.Directory.GetCurrentDirectory();
    }
}
