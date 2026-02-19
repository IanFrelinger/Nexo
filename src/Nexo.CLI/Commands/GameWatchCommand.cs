using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester;
using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.CLI.Output;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Commands;

/// <summary>
/// Live gameplay watch mode: captures screenshots at high frequency and periodically
/// summarizes what's happening using vision AI. No actions executed.
/// </summary>
public sealed class GameWatchCommand : Command
{
    public GameWatchCommand() : base("game-watch", "Watch and live-summarize video game gameplay (vision AI, no actions)")
    {
        var processOpt = new Option<string>("--process", "Process name to watch (e.g. GameName, chrome). Bring its window to the foreground.")
        {
            IsRequired = true
        };
        var durationOpt = new Option<string>("--duration", () => "10m", "How long to watch (e.g. 5m, 30m, 1h)");
        var summaryIntervalOpt = new Option<int>("--summary-interval", () => 15, "Seconds between live summaries");
        var captureIntervalOpt = new Option<int>("--capture-interval", () => 200, "Milliseconds between frame captures (lower = smoother, higher CPU; 100=10fps)");
        var verboseOpt = new Option<bool>("--verbose", () => false, "Verbose output");

        AddOption(processOpt);
        AddOption(durationOpt);
        AddOption(summaryIntervalOpt);
        AddOption(captureIntervalOpt);
        AddOption(verboseOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var process = ctx.ParseResult.GetValueForOption(processOpt)!;
            var durationStr = ctx.ParseResult.GetValueForOption(durationOpt)!;
            var summaryInterval = ctx.ParseResult.GetValueForOption(summaryIntervalOpt);
            var captureInterval = ctx.ParseResult.GetValueForOption(captureIntervalOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);
            var maxDuration = ParseDuration(durationStr);
            await ExecuteAsync(process, maxDuration, summaryInterval, captureInterval, verbose);
        });
    }

    /// <summary>Parses duration string (e.g. "5m", "1h", "60s") to TimeSpan.</summary>
    private static TimeSpan ParseDuration(string s)
    {
        s = s.Trim().ToLowerInvariant();
        if (s.EndsWith("m"))
        {
            var n = int.TryParse(s[..^1], out var v) ? v : 10;
            return TimeSpan.FromMinutes(n);
        }
        if (s.EndsWith("h"))
        {
            var n = int.TryParse(s[..^1], out var v) ? v : 1;
            return TimeSpan.FromHours(n);
        }
        if (s.EndsWith("s"))
        {
            var n = int.TryParse(s[..^1], out var v) ? v : 60;
            return TimeSpan.FromSeconds(n);
        }
        return int.TryParse(s, out var mins) ? TimeSpan.FromMinutes(mins) : TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// Configures UniversalTesterAgent for watch-only mode, runs it, and displays report summary.
    /// Uses DesktopAdapter to capture screenshots of the target process window.
    /// </summary>
    private static async Task ExecuteAsync(
        string processName,
        TimeSpan maxDuration,
        int summaryInterval,
        int captureInterval,
        bool verbose)
    {
        var console = new CliConsole(verbose);
        console.WriteHeader("Live Gameplay Watch Mode");
        console.WritePair("Process", processName);
        console.WritePair("Duration", maxDuration.ToString(@"hh\:mm\:ss"));
        console.WritePair("Summary interval", $"{summaryInterval}s");
        console.WritePair("Capture interval", $"{captureInterval}ms");
        console.WriteLine();

        var config = new UniversalTesterConfig
        {
            Target = $"process://{processName}",
            TargetType = TargetType.DesktopApp,
            Goal = "Summarize the video game gameplay.",
            WatchOnly = true,
            MaxDuration = maxDuration,
            SummaryIntervalSeconds = summaryInterval,
            CaptureIntervalMs = captureInterval
        };

        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddSingleton<IProviderFactory, ProviderFactory>()
            .AddSingleton<IAdapterRegistry, DefaultAdapterRegistry>()
            .BuildServiceProvider();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var agent = new UniversalTesterAgent(
            services.GetRequiredService<IProviderFactory>(),
            services.GetRequiredService<IAdapterRegistry>(),
            loggerFactory.CreateLogger<UniversalTesterAgent>());
        var context = new Nexo.Infrastructure.Execution.ExecutionContext { Provider = "ollama", Variables = new Dictionary<string, object>() };

        try
        {
            var report = await agent.ExecuteAsync(config, context, runtime: null, CancellationToken.None);
            console.WriteLine();
            console.WriteHeader("Watch Complete");
            console.WritePair("Summaries generated", report.Summary.TotalTests.ToString());
            console.WritePair("Duration", report.Summary.Duration.ToString(@"hh\:mm\:ss"));
            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            console.WriteError(ex.Message);
            Environment.ExitCode = 1;
        }
    }
}
