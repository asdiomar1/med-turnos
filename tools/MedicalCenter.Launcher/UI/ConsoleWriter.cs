namespace MedicalCenter.Launcher.UI;

using System;

/// <summary>
/// Provides colored console output helpers.
/// </summary>
public static class ConsoleWriter
{
    private static readonly object _lock = new();

    public static void SectionHeader(string title)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine($"═══ {title} ═══");
            Console.ResetColor();
        }
    }

    public static void Success(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {message}");
            Console.ResetColor();
        }
    }

    public static void Error(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {message}");
            Console.ResetColor();
        }
    }

    public static void Warning(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠ {message}");
            Console.ResetColor();
        }
    }

    public static void Info(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"  {message}");
            Console.ResetColor();
        }
    }

    public static void Debug(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"  [DEBUG] {message}");
            Console.ResetColor();
        }
    }

    public static void WriteSpinner(char[] frames, int frameIndex, string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"\r{frames[frameIndex % frames.Length]} {message}");
            Console.ResetColor();
        }
    }

    public static void ClearSpinner()
    {
        lock (_lock)
        {
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
        }
    }

    public static void PrintCheckResult(string checkName, bool passed, string details = "")
    {
        lock (_lock)
        {
            if (passed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"  [PASS] ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"  [FAIL] ");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(checkName);

            if (!string.IsNullOrEmpty(details))
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($" - {details}");
            }

            Console.ResetColor();
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Prints a message inside a bordered box for emphasis (useful for error details).
    /// </summary>
    public static void PrintBox(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        lock (_lock)
        {
            var lines = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var maxLength = Math.Min(lines.Max(l => l.Length) + 4, Console.WindowWidth - 2);
            
            // Top border
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("┌" + new string('─', maxLength) + "┐");
            
            // Content
            foreach (var line in lines)
            {
                var trimmed = line.Length > maxLength - 4 ? line.Substring(0, maxLength - 4) : line;
                Console.WriteLine($"│ {trimmed.PadRight(maxLength - 3)} │");
            }
            
            // Bottom border
            Console.WriteLine("└" + new string('─', maxLength) + "┘");
            Console.ResetColor();
        }
    }
}