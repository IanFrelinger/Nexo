using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Runs the YouTube video summary test in Docker (headless, virtualized).
/// Launches docker-compose.youtube-test.yml with Chrome, optional audio transcription,
/// and optional SmolVLM2-Video service for temporal analysis.
/// Replaces scripts/run-youtube-test-docker.sh
/// </summary>
public sealed class YoutubeTestDockerCommand : Command
{
    /// <summary>
    /// Registers the youtube-docker command and its options (URL, parallel videos,
    /// Ollama URL, audio transcription, watch mode, video model, capture interval, duration).
    /// </summary>
    public YoutubeTestDockerCommand() : base("youtube-docker", "Run YouTube video summary test in Docker (headless, parallel-safe)")
    {
        var urlOpt = new Option<string>("--url", () => "https://www.youtube.com/watch?v=psbAgsMD8QM", "YouTube video URL");
        var parallelOpt = new Option<string[]>("--parallel", "Video IDs or URLs to run in parallel")
        {
            AllowMultipleArgumentsPerToken = true
        };
        var ollamaOpt = new Option<string>("--ollama-url", () => "http://host.docker.internal:11434", "Ollama base URL for containers");
        var withAudioOpt = new Option<bool>("--with-audio", () => false, "Transcribe video audio first (Whisper tiny) and pass to vision");
        var watchOpt = new Option<bool>("--watch", () => false, "Watch mode: live summaries at high capture rate (virtual desktop in Docker)");
        var useVideoModelOpt = new Option<bool>("--use-video-model", () => false, "Use SmolVLM2-Video container for temporal video analysis (watch mode; starts video-service)");
        var captureIntervalOpt = new Option<int>("--capture-interval", () => 200, "Watch mode: ms between frame captures (100=10fps, 200=5fps)");
        var durationOpt = new Option<string>("--duration", () => "10m", "Watch mode: how long (e.g. 5m, 30m)");
        var verboseOpt = new Option<bool>("--verbose", () => false, "Verbose output");

        AddOption(urlOpt);
        AddOption(parallelOpt);
        AddOption(ollamaOpt);
        AddOption(withAudioOpt);
        AddOption(watchOpt);
        AddOption(useVideoModelOpt);
        AddOption(captureIntervalOpt);
        AddOption(durationOpt);
        AddOption(verboseOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var url = ctx.ParseResult.GetValueForOption(urlOpt)!;
            var parallel = ctx.ParseResult.GetValueForOption(parallelOpt) ?? Array.Empty<string>();
            var ollamaUrl = ctx.ParseResult.GetValueForOption(ollamaOpt)!;
            var withAudio = ctx.ParseResult.GetValueForOption(withAudioOpt);
            var watch = ctx.ParseResult.GetValueForOption(watchOpt);
            var useVideoModel = ctx.ParseResult.GetValueForOption(useVideoModelOpt);
            var captureInterval = ctx.ParseResult.GetValueForOption(captureIntervalOpt);
            var duration = ctx.ParseResult.GetValueForOption(durationOpt)!;
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);
            await ExecuteAsync(url, parallel, ollamaUrl, withAudio, watch, useVideoModel, captureInterval, duration, verbose);
        });
    }

    /// <summary>
    /// Main execution: discovers project root, ensures video-service if needed, runs one or more videos.
    /// </summary>
    private static async Task ExecuteAsync(string defaultUrl, string[] parallel, string ollamaUrl, bool withAudio, bool watch, bool useVideoModel, int captureInterval, string duration, bool verbose)
    {
        var console = new CliConsole(verbose);
        var root = DiscoverProjectRoot();
        var composePath = Path.Combine(root, "docker-compose.youtube-test.yml");
        if (!File.Exists(composePath))
        {
            console.WriteError($"Docker Compose file not found: {composePath}");
            Environment.ExitCode = 1;
            return;
        }

        var reportDir = Path.Combine(root, "test-results", "youtube-reports");
        Directory.CreateDirectory(reportDir);

        if (useVideoModel)
        {
            await EnsureVideoServiceRunningAsync(root, composePath, console);
        }

        if (parallel.Length > 0)
        {
            var tasks = parallel.Select(vid => RunOneAsync(NormalizeUrl(vid), ollamaUrl, withAudio, watch, useVideoModel, captureInterval, duration, root, composePath, console));
            var results = await Task.WhenAll(tasks);
            var failed = results.Count(r => r != 0);
            console.WriteLine();
            console.WriteSuccess($"Reports: {reportDir}");
            Environment.ExitCode = failed;
        }
        else
        {
            Environment.ExitCode = await RunOneAsync(defaultUrl, ollamaUrl, withAudio, watch, useVideoModel, captureInterval, duration, root, composePath, console);
            console.WriteLine();
            console.WriteSuccess($"Report: {reportDir}");
        }
    }

    /// <summary>Parses duration string (e.g. "5m", "1h") to minutes.</summary>
    private static int ParseDurationMinutes(string s)
    {
        s = s.Trim().ToLowerInvariant();
        if (s.EndsWith("m")) return int.TryParse(s[..^1], out var v) ? v : 10;
        if (s.EndsWith("h")) return (int.TryParse(s[..^1], out var v) ? v : 1) * 60;
        return int.TryParse(s, out var mins) ? mins : 10;
    }

    /// <summary>
    /// Runs a single YouTube test/watch in Docker. Builds docker compose args (env vars for URL, watch mode, video service),
    /// optionally transcribes audio first, and invokes docker compose run.
    /// </summary>
    private static async Task<int> RunOneAsync(string url, string ollamaUrl, bool withAudio, bool watch, bool useVideoModel, int captureInterval, string duration, string root, string composePath, CliConsole console)
    {
        console.WriteLine($"Starting {(watch ? "watch" : "test")} for: {url}");
        var argList = new List<string>
        {
            "compose", "-f", composePath, "run", "--rm", "--no-deps",
            "-e", $"VIDEO_URL={url}",
            "-e", $"OLLAMA_BASE_URL={ollamaUrl}"
        };
        if (watch)
        {
            argList.Add("-e");
            argList.Add("WATCH_MODE=1");
            argList.Add("-e");
            argList.Add($"CAPTURE_INTERVAL_MS={captureInterval}");
            argList.Add("-e");
            argList.Add($"MAX_DURATION_MINUTES={ParseDurationMinutes(duration)}");
        }

        if (useVideoModel)
        {
            if (!watch)
            {
                console.WriteWarning("--use-video-model requires --watch; enabling watch mode.");
                argList.Add("-e");
                argList.Add("WATCH_MODE=1");
            }
            argList.Add("-e");
            argList.Add("VIDEO_SERVICE_URL=http://video-service:8080");
        }

        string? transcriptFile = null;
        if (withAudio)
        {
            transcriptFile = Path.Combine(root, ".tmp", $"transcript-{Guid.NewGuid():N}.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(transcriptFile)!);
            console.WriteLine("Transcribing audio (Whisper tiny)...");
            var transcribed = await RunTranscribeAsync(url, root, console);
            if (!string.IsNullOrEmpty(transcribed))
            {
                await File.WriteAllTextAsync(transcriptFile, transcribed);
                argList.Add("-v");
                argList.Add($"{Path.GetFullPath(transcriptFile)}:/workspace/audio-transcript.txt");
                argList.Add("-e");
                argList.Add("AUDIO_TRANSCRIPT_FILE=/workspace/audio-transcript.txt");
            }
        }

        argList.Add("youtube-test");
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var a in argList)
            psi.ArgumentList.Add(a);
        using var process = Process.Start(psi);
        if (process == null)
        {
            console.WriteError("Failed to start docker compose");
            return 1;
        }
        var outTask = process.StandardOutput.ReadToEndAsync();
        var errTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var stdout = await outTask;
        var stderr = await errTask;
        if (!string.IsNullOrEmpty(stdout))
            console.WriteLine(stdout.TrimEnd());
        if (!string.IsNullOrEmpty(stderr) && process.ExitCode != 0)
            console.WriteError(stderr.TrimEnd());
        if (!string.IsNullOrEmpty(transcriptFile) && File.Exists(transcriptFile))
        {
            try { File.Delete(transcriptFile); } catch { /* ignore */ }
        }
        return process.ExitCode;
    }

    /// <summary>Starts the video-service container (SmolVLM2-Video) and waits for model load.</summary>
    private static async Task EnsureVideoServiceRunningAsync(string root, string composePath, CliConsole console)
    {
        console.WriteLine("Starting video-service (SmolVLM2-Video)...");
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            ArgumentList = { "compose", "-f", composePath, "up", "-d", "video-service" },
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi);
        if (p == null) return;
        await p.WaitForExitAsync();
        if (p.ExitCode != 0)
        {
            var err = await p.StandardError.ReadToEndAsync();
            console.WriteError($"Failed to start video-service: {err}");
            return;
        }
        console.WriteLine("Waiting 30s for video-service to load model...");
        await Task.Delay(30_000);
    }

    /// <summary>Runs nexo demo youtube-transcribe to capture audio transcript for the video.</summary>
    private static async Task<string> RunTranscribeAsync(string url, string root, CliConsole console)
    {
        var nexoPath = FindNexoPath();
        if (nexoPath == null)
        {
            console.WriteError("nexo CLI not found - cannot run --with-audio. Run without it or use: nexo demo youtube-transcribe --url URL > transcript.txt");
            return "";
        }
        var psi = new ProcessStartInfo
        {
            FileName = nexoPath,
            ArgumentList = { "demo", "youtube-transcribe", "--url", url },
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi);
        if (p == null) return "";
        var stdout = await p.StandardOutput.ReadToEndAsync();
        await p.WaitForExitAsync();
        return p.ExitCode == 0 ? stdout.Trim() : "";
    }

    /// <summary>Locates the nexo executable in PATH or from current process.</summary>
    private static string? FindNexoPath()
    {
        var exe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "nexo.exe" : "nexo";
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator))
        {
            var full = Path.Combine(dir.Trim(), exe);
            if (File.Exists(full)) return full;
        }
        var self = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(self) && Path.GetFileNameWithoutExtension(self).Contains("nexo", StringComparison.OrdinalIgnoreCase))
            return self;
        return null;
    }

    /// <summary>Converts a video ID or partial URL to a full YouTube watch URL.</summary>
    private static string NormalizeUrl(string vid)
    {
        if (vid.Contains("youtube") || vid.Contains("watch"))
            return vid;
        return $"https://www.youtube.com/watch?v={vid}";
    }

    /// <summary>Walks up from current directory to find the folder containing Nexo.sln.</summary>
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
}
