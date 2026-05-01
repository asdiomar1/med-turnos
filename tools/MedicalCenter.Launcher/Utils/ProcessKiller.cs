namespace MedicalCenter.Launcher.Utils;

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

/// <summary>
/// Finds and terminates processes listening on specific ports.
/// </summary>
public static class ProcessKiller
{
    /// <summary>
    /// Kills the process listening on the given port, if any.
    /// Returns true if a process was found and killed.
    /// </summary>
    public static async Task<bool> KillProcessOnPortAsync(int port)
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? await KillOnPortWindowsAsync(port)
            : await KillOnPortUnixAsync(port);
    }

    private static async Task<bool> KillOnPortWindowsAsync(int port)
    {
        // netstat -ano lists: Proto  Local Address  Foreign Address  State  PID
        var netstat = await ProcessRunner.RunAsync(
            "netstat", $"-ano",
            timeoutMs: 5000);

        if (netstat.ExitCode != 0) return false;

        var pid = ParsePidFromNetstat(netstat.Stdout, port);
        if (pid == null) return false;

        var kill = await ProcessRunner.RunAsync(
            "taskkill", $"/PID {pid} /F /T",
            timeoutMs: 5000);

        return kill.ExitCode == 0;
    }

    private static async Task<bool> KillOnPortUnixAsync(int port)
    {
        // lsof -ti :PORT prints the PID of the process using that port
        var lsof = await ProcessRunner.RunAsync(
            "lsof", $"-ti :{port}",
            timeoutMs: 5000);

        var pidStr = lsof.Stdout.Trim();
        if (lsof.ExitCode != 0 || string.IsNullOrEmpty(pidStr)) return false;

        var kill = await ProcessRunner.RunAsync(
            "kill", $"-9 {pidStr}",
            timeoutMs: 5000);

        return kill.ExitCode == 0;
    }

    private static string? ParsePidFromNetstat(string output, int port)
    {
        // Look for lines containing ":PORT " in LISTENING state
        var target = $":{port} ";
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.Contains(target)) continue;
            if (!trimmed.Contains("LISTENING")) continue;

            // Format: TCP  0.0.0.0:5245  0.0.0.0:0  LISTENING  12345
            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 5)
            {
                return parts[^1]; // Last token is the PID
            }
        }
        return null;
    }
}
