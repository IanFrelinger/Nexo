namespace Ashlar.CLI.Output;

/// <summary>CLI console helper for formatted output, progress indicators, and user interaction.</summary>
public class CliConsole
{
    private readonly bool _verbose;
    private bool _spinnerActive;
    private CancellationTokenSource? _spinnerCts;
    private readonly object _lock = new();

    /// <summary>Creates a console helper; verbose mode enables detailed progress output.</summary>
    public CliConsole(bool verbose = false)
    {
        _verbose = verbose;
    }

    /// <summary>Writes a section header with underline styling.</summary>
    public void WriteHeader(string text)
    {
        Console.WriteLine();
        WriteColored(text, ConsoleColor.Cyan);
        var lineLength = Math.Min(text.Length + 4, 60);
        WriteColored(new string('─', lineLength), ConsoleColor.DarkGray);
        Console.WriteLine();
    }

    /// <summary>Writes a key-value pair with muted key styling.</summary>
    public void WritePair(string key, string value)
    {
        WriteColored($"  {key}: ", ConsoleColor.DarkGray);
        Console.WriteLine(value);
    }

    /// <summary>Writes text in the specified console color without a trailing newline.</summary>
    public void WriteColored(string text, ConsoleColor color)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = original;
    }

    /// <summary>Writes a line of text in the specified console color.</summary>
    public void WriteColoredLine(string text, ConsoleColor color)
    {
        WriteColored(text, color);
        Console.WriteLine();
    }

    /// <summary>Writes text without a trailing newline.</summary>
    public void Write(string text) => Console.Write(text);

    /// <summary>Writes a line of text.</summary>
    public void WriteLine(string text = "") => Console.WriteLine(text);

    /// <summary>Writes a success message prefixed with a check mark.</summary>
    public void WriteSuccess(string text) => WriteColoredLine($"✓ {text}", ConsoleColor.Green);

    /// <summary>Writes an error message prefixed with a cross mark.</summary>
    public void WriteError(string text) => WriteColoredLine($"✗ {text}", ConsoleColor.Red);

    /// <summary>Writes a warning message prefixed with a warning symbol.</summary>
    public void WriteWarning(string text) => WriteColoredLine($"⚠ {text}", ConsoleColor.Yellow);

    /// <summary>Starts an animated spinner with the given status message.</summary>
    public void StartSpinner(string message)
    {
        lock (_lock)
        {
            if (_spinnerActive) return;

            _spinnerActive = true;
            _spinnerCts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                var spinChars = new[] { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };
                var i = 0;

                while (!_spinnerCts.Token.IsCancellationRequested)
                {
                    lock (_lock)
                    {
                        if (!_spinnerActive) break;
                        Console.Write($"\r{spinChars[i++ % spinChars.Length]} {message}");
                    }
                    await Task.Delay(80, _spinnerCts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                }
            }, _spinnerCts.Token);
        }
    }

    /// <summary>Updates the active spinner message in place.</summary>
    public void UpdateSpinner(string message)
    {
        if (_spinnerActive)
        {
            Console.Write($"\r  {message}".PadRight(60));
        }
    }

    /// <summary>Stops the active spinner and clears the spinner line.</summary>
    public void StopSpinner()
    {
        lock (_lock)
        {
            _spinnerCts?.Cancel();
            _spinnerActive = false;
            Console.Write("\r".PadRight(60) + "\r"); // Clear line
        }
    }

    /// <summary>Writes brick execution progress when verbose mode is enabled.</summary>
    public void WriteProgress(BrickProgress progress)
    {
        if (!_verbose) return;

        var statusIcon = progress.Status switch
        {
            "running" => "⏳",
            "completed" => "✓",
            "failed" => "✗",
            "awaiting_approval" => "❓",
            _ => "•"
        };

        var color = progress.Status switch
        {
            "completed" => ConsoleColor.Green,
            "failed" => ConsoleColor.Red,
            "awaiting_approval" => ConsoleColor.Yellow,
            _ => ConsoleColor.Gray
        };

        WriteColored($"  {statusIcon} [{progress.BrickId}] {progress.Status}", color);
        Console.WriteLine();

        if (progress.Metrics != null)
        {
            foreach (var (key, value) in progress.Metrics)
            {
                WriteColored($"    {key}: {value}", ConsoleColor.DarkGray);
                Console.WriteLine();
            }
        }

        if (progress.Progress.HasValue)
        {
            WriteProgressBar(progress.Progress.Value);
        }
    }

    private void WriteProgressBar(int percent)
    {
        var width = 30;
        var filled = (int)(width * percent / 100.0);
        var empty = width - filled;

        Console.Write("    [");
        WriteColored(new string('█', filled), ConsoleColor.Green);
        Console.Write(new string('░', empty));
        Console.WriteLine($"] {percent}%");
    }

    /// <summary>Writes an aligned table from headers and row data.</summary>
    public void WriteTable(string[] headers, IEnumerable<string[]> rows)
    {
        // Calculate column widths
        var widths = headers.Select(h => h.Length).ToArray();
        var rowList = rows.ToList();

        foreach (var row in rowList)
        {
            for (int i = 0; i < row.Length && i < widths.Length; i++)
            {
                widths[i] = Math.Max(widths[i], row[i].Length);
            }
        }

        // Print header
        WriteColored("  " + string.Join(" │ ", headers.Select((h, i) => h.PadRight(widths[i]))), ConsoleColor.Cyan);
        Console.WriteLine();
        Console.WriteLine("  " + string.Join("─┼─", widths.Select(w => new string('─', w))));

        // Print rows
        foreach (var row in rowList)
        {
            Console.WriteLine("  " + string.Join(" │ ", row.Select((c, i) => c.PadRight(widths[i]))));
        }
    }

    /// <summary>Prompts for a single line of user input.</summary>
    public string? Prompt(string promptText)
    {
        Console.Write(promptText);
        return Console.ReadLine();
    }

    /// <summary>Prompts for a yes/no response with an optional default.</summary>
    public bool PromptYesNo(string promptText, bool defaultValue = false)
    {
        var defaultText = defaultValue ? "[Y/n]" : "[y/N]";
        Console.Write($"{promptText} {defaultText}: ");
        var response = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(response))
            return defaultValue;

        return response == "y" || response == "yes";
    }
}
