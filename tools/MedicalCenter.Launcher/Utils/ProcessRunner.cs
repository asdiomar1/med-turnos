namespace MedicalCenter.Launcher.Utils;

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Shared process runner with async stdout/stderr capture, exit code return, and timeout support.
/// </summary>
public static class ProcessRunner
{
    /// <summary>
    /// Runs a process and captures its output.
    /// </summary>
    /// <param name="onOutput">Optional callback invoked for each stdout line (in addition to capture).</param>
    /// <param name="onError">Optional callback invoked for each stderr line (in addition to capture).</param>
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        int timeoutMs = 0,
        Action<string>? onOutput = null,
        Action<string>? onError = null,
        CancellationToken cancellationToken = default)
    {
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            stdout.AppendLine(e.Data);
            onOutput?.Invoke(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            stderr.AppendLine(e.Data);
            onError?.Invoke(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var completed = await WaitForExitAsync(process, timeoutMs, cancellationToken);

        if (!completed)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            return new ProcessResult(-1, stdout.ToString(), stderr.ToString(), true);
        }

        return new ProcessResult(
            process.ExitCode,
            stdout.ToString(),
            stderr.ToString(),
            false);
    }

    private static async Task<bool> WaitForExitAsync(Process process, int timeoutMs, CancellationToken cancellationToken)
    {
        if (timeoutMs <= 0)
        {
            try
            {
                await process.WaitForExitAsync(cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false; // Cancellation requested
            }
        }

        using var timeoutCts = new CancellationTokenSource(timeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
            return true;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return false; // Timeout
        }
        catch (OperationCanceledException)
        {
            return false; // External cancellation requested
        }
    }
}

/// <summary>
/// Result of a process execution.
/// </summary>
public record ProcessResult(int ExitCode, string Stdout, string Stderr, bool TimedOut);