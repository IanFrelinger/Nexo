using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Agents.UniversalTester.Models;
using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.CLI.Output;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Commands;

/// <summary>
/// Runs inside the YouTube test Docker container.
/// Starts Xvfb, launches Chrome with the video URL, then runs the Universal Tester agent.
/// Replaces .docker/scripts/youtube-test-entrypoint.sh
/// </summary>
public sealed class YoutubeTestContainerCommand : Command
{
    public YoutubeTestContainerCommand() : base("youtube-container", "Run YouTube test inside container (Xvfb + Chrome + agent)")
    {
        this.SetHandler(ExecuteAsync);
    }

    private static async Task ExecuteAsync(InvocationContext ctx)
    {
        var videoUrl = Environment.GetEnvironmentVariable("VIDEO_URL") ?? "https://www.youtube.com/watch?v=psbAgsMD8QM";
        var browserProcess = Environment.GetEnvironmentVariable("VIDEO_TEST_BROWSER_PROCESS") ?? "chrome";
        var ollamaUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://host.docker.internal:11434";
        var reportDir = Environment.GetEnvironmentVariable("REPORT_DIR") ?? "/workspace/reports";
        var displayNum = Environment.GetEnvironmentVariable("DISPLAY_NUM") ?? "99";
        var audioTranscript = Environment.GetEnvironmentVariable("AUDIO_TRANSCRIPT");
        var audioTranscriptFile = Environment.GetEnvironmentVariable("AUDIO_TRANSCRIPT_FILE");
        if (string.IsNullOrWhiteSpace(audioTranscript) && !string.IsNullOrWhiteSpace(audioTranscriptFile) && File.Exists(audioTranscriptFile))
            audioTranscript = File.ReadAllText(audioTranscriptFile).Trim();

        Directory.CreateDirectory(reportDir);

        var console = new CliConsole(verbose: true);
        console.WriteHeader("Nexo YouTube Video Summary Test (Headless / Virtual Display)");
        console.WritePair("Video", videoUrl);
        console.WritePair("Browser process", browserProcess);
        console.WritePair("Ollama", ollamaUrl);
        if (!string.IsNullOrEmpty(audioTranscript))
            console.WritePair("Audio transcript", $"{audioTranscript.Length} chars");
        console.WriteLine();

        Process? xvfb = null;
        Process? chrome = null;

        try
        {
            Environment.SetEnvironmentVariable("DISPLAY", $":{displayNum}");
            Environment.SetEnvironmentVariable("OLLAMA_BASE_URL", ollamaUrl);

            console.WriteLine("1. Starting Xvfb...");
            xvfb = StartXvfb(displayNum);
            await Task.Delay(2000);

            console.WriteLine("2. Launching browser with video...");
            chrome = StartChrome(videoUrl);
            if (chrome == null)
            {
                console.WriteError("No Chrome/Chromium found");
                Environment.ExitCode = 1;
                return;
            }

            console.WriteLine("3. Waiting 20s for page load...");
            await Task.Delay(20_000);

            console.WriteLine("4. Running Universal Tester agent...");
            var config = new UniversalTesterConfig
            {
                Target = $"process://{browserProcess}",
                Goal = YoutubeGoals.SummaryGoal,
                Depth = TestingDepth.Thorough,
                MaxDuration = TimeSpan.FromMinutes(25),
                AudioTranscript = !string.IsNullOrWhiteSpace(audioTranscript) ? audioTranscript : null
            };
            var runtime = UniversalTesterRuntimeConfig.Default() with { MultiFrameCount = 1 };
            var reportPath = Path.Combine(reportDir, $"youtube-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}.html");
            var report = await RunAgentAsync(config, runtime, "ollama");
            var html = report.HtmlReport ?? GenerateHtml(report);
            await File.WriteAllTextAsync(reportPath, html);
            console.WriteSuccess($"Report: {reportPath}");
            Environment.ExitCode = report.Summary.OverallScore >= 80 ? 0 : 1;
        }
        finally
        {
            chrome?.Kill(entireProcessTree: true);
            xvfb?.Kill(entireProcessTree: true);
        }
    }

    private static Process StartXvfb(string displayNum)
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = "Xvfb",
            ArgumentList = { $":{displayNum}", "-screen", "0", "1920x1080x24", "-ac", "-nocursor", "-noreset" },
            UseShellExecute = false,
            CreateNoWindow = true
        });
        return p ?? throw new InvalidOperationException("Failed to start Xvfb");
    }

    private static Process? StartChrome(string url)
    {
        var chromes = new[] { "google-chrome-stable", "chromium-browser", "chromium" };
        foreach (var bin in chromes)
        {
            var path = FindInPath(bin);
            if (path == null)
                continue;
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = path,
                ArgumentList =
                {
                    "--no-sandbox",
                    "--disable-gpu",
                    "--disable-dev-shm-usage",
                    "--disable-software-rasterizer",
                    "--window-size=1920,1080",
                    "--start-maximized",
                    "--force-device-scale-factor=1",
                    url
                },
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return p;
        }
        return null;
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

    private static async Task<TestReport> RunAgentAsync(UniversalTesterConfig config, UniversalTesterRuntimeConfig runtime, string provider)
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddConsole())
            .AddSingleton<IProviderFactory, ProviderFactory>()
            .BuildServiceProvider();
        var agent = new UniversalTesterAgent(
            services.GetRequiredService<IProviderFactory>(),
            services.GetRequiredService<ILoggerFactory>().CreateLogger<UniversalTesterAgent>());
        var context = new Nexo.Infrastructure.Execution.ExecutionContext { Provider = provider, Variables = new Dictionary<string, object>() };
        return await agent.ExecuteAsync(config, context, runtime, CancellationToken.None);
    }

    private static string GenerateHtml(TestReport r) =>
        $"<!DOCTYPE html><html><head><title>Nexo Report</title></head><body><h1>Score: {r.Summary.OverallScore:F0}%</h1><ul>{string.Join("", r.Summary.KeyFindings.Select(f => $"<li>{f}</li>"))}</ul></body></html>";
}
