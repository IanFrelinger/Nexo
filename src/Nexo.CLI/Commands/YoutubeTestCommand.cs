using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.CLI.Output;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.IO;

namespace Nexo.CLI.Commands;

/// <summary>
/// Shared goal strings for YouTube test commands.
/// </summary>
internal static class YoutubeGoals
{
    /// <summary>
    /// Goal for summarizing a YouTube video: play, watch until end, then provide a detailed summary.
    /// </summary>
    public const string SummaryGoal = "This browser is showing a YouTube video. Your task: (1) Click Play if the video has not started. (2) Watch the video until it finishes - you will see the replay button or end screen when it is done. (3) When the video has ended, provide a detailed summary of the video content in your final report. Describe the main topics, key points, and any notable visuals or information presented.";
}

/// <summary>
/// Runs the YouTube video summary test using the desktop adapter.
/// Opens the video in a browser, then runs the Universal Tester agent.
/// Replaces scripts/run-youtube-video-summary-test.sh
/// </summary>
public sealed class YoutubeTestCommand : Command
{
    public YoutubeTestCommand() : base("youtube", "Run YouTube video summary test (desktop: open browser, capture screen)")
    {
        var urlOpt = new Option<string>("--url", () => "https://www.youtube.com/watch?v=psbAgsMD8QM", "YouTube video URL");
        var outputOpt = new Option<FileInfo?>("--output", "Output report path");
        var transcriptOpt = new Option<string?>("--transcript", "Audio transcript to augment vision (or set AUDIO_TRANSCRIPT env)");
        var verboseOpt = new Option<bool>("--verbose", () => false, "Verbose output");

        AddOption(urlOpt);
        AddOption(outputOpt);
        AddOption(transcriptOpt);
        AddOption(verboseOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var url = ctx.ParseResult.GetValueForOption(urlOpt)!;
            var output = ctx.ParseResult.GetValueForOption(outputOpt);
            var transcript = ctx.ParseResult.GetValueForOption(transcriptOpt) ?? Environment.GetEnvironmentVariable("AUDIO_TRANSCRIPT");
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);
            await ExecuteAsync(url, output, transcript, verbose);
        });
    }

    private static async Task ExecuteAsync(string videoUrl, FileInfo? output, string? transcript, bool verbose)
    {
        var console = new CliConsole(verbose);
        var root = DiscoverProjectRoot();
        var reportDir = Path.Combine(root, ".tmp");
        Directory.CreateDirectory(reportDir);
        var reportPath = output?.FullName ?? Path.Combine(reportDir, $"youtube-video-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}.html");

        var (browserApp, browserProcess) = DetectBrowser();
        if (browserApp == null)
        {
            console.WriteError("No supported browser found. Install Chrome or Safari.");
            Environment.ExitCode = 1;
            return;
        }

        console.WriteHeader("YouTube Video Summary Test (Desktop Adapter + Vision)");
        console.WritePair("Video", videoUrl);
        console.WritePair("Browser", browserApp);
        console.WritePair("Target", $"process://{browserProcess}");
        console.WriteLine();

        console.WriteLine("1. Opening video in browser...");
        LaunchBrowser(browserApp, videoUrl);

        console.WriteLine("2. Waiting 15 seconds for page load...");
        await Task.Delay(15_000);

        console.WriteLine("3. Bring the browser window to the foreground. Running agent in 10 seconds...");
        await Task.Delay(10_000);

        console.WriteLine("4. Running Universal Tester agent...");
        try
        {
            var config = new UniversalTesterConfig
            {
                Target = $"process://{browserProcess}",
                Goal = YoutubeGoals.SummaryGoal,
                Depth = TestingDepth.Thorough,
                MaxDuration = TimeSpan.FromMinutes(25),
                AudioTranscript = !string.IsNullOrWhiteSpace(transcript) ? transcript : null
            };
            var runtime = UniversalTesterRuntimeConfig.Default() with { MultiFrameCount = 1 };
            var report = await RunAgentAsync(config, runtime, "ollama", reportPath);
            DisplayAndSave(report, reportPath, console);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && File.Exists(reportPath))
            {
                Process.Start(new ProcessStartInfo { FileName = "open", ArgumentList = { reportPath }, UseShellExecute = false });
            }
            Environment.ExitCode = report.Summary.OverallScore >= 80 ? 0 : 1;
        }
        catch (Exception ex)
        {
            console.WriteError(ex.Message);
            Environment.ExitCode = 1;
        }
    }

    private static (string? app, string process) DetectBrowser()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (Directory.Exists("/Applications/Google Chrome.app"))
                return ("Google Chrome", "Google Chrome");
            if (Directory.Exists("/Applications/Safari.app"))
                return ("Safari", "Safari");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            foreach (var (bin, process) in new[] { ("google-chrome-stable", "chrome"), ("google-chrome", "chrome"), ("chromium", "chromium") })
            {
                if (FindInPath(bin) != null)
                    return (bin, process);
            }
        }
        return (null, "chromium");
    }

    private static string? FindInPath(string name)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            var full = Path.Combine(dir.Trim(), name);
            if (File.Exists(full))
                return full;
        }
        return null;
    }

    private static void LaunchBrowser(string app, string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                ArgumentList = { "-a", app, url },
                UseShellExecute = false
            });
        }
        else
        {
            var bin = FindInPath(app) ?? app;
            Process.Start(new ProcessStartInfo
            {
                FileName = bin,
                ArgumentList = { url },
                UseShellExecute = true
            });
        }
    }

    private static string DiscoverProjectRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nexo.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    private static async Task<TestReport> RunAgentAsync(UniversalTesterConfig config, UniversalTesterRuntimeConfig runtime, string provider, string reportPath)
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddSingleton<IProviderFactory, ProviderFactory>()
            .BuildServiceProvider();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var agent = new UniversalTesterAgent(services.GetRequiredService<IProviderFactory>(), loggerFactory.CreateLogger<UniversalTesterAgent>());
        var context = new Nexo.Infrastructure.Execution.ExecutionContext
        {
            Provider = provider,
            Variables = new Dictionary<string, object>()
        };
        return await agent.ExecuteAsync(config, context, runtime, CancellationToken.None);
    }

    private static void DisplayAndSave(TestReport report, string path, CliConsole console)
    {
        console.WriteHeader("Test Results");
        console.WriteColoredLine($"Overall Score: {report.Summary.OverallScore:F0}%", report.Summary.OverallScore >= 80 ? ConsoleColor.Green : ConsoleColor.Yellow);
        foreach (var f in report.Summary.KeyFindings)
            console.WriteLine($"  • {f}");
        var html = report.HtmlReport ?? GenerateHtml(report);
        File.WriteAllText(path, html);
        console.WriteSuccess($"Report saved to {path}");
    }

    private static string GenerateHtml(TestReport r) =>
        $"<!DOCTYPE html><html><head><title>Nexo Report</title></head><body><h1>Score: {r.Summary.OverallScore:F0}%</h1><ul>{string.Join("", r.Summary.KeyFindings.Select(f => $"<li>{f}</li>"))}</ul></body></html>";
}
