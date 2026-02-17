using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Runs the YouTube video summary test in Docker (headless, virtualized).
/// Replaces scripts/run-youtube-test-docker.sh
/// </summary>
public sealed class YoutubeTestDockerCommand : Command
{
    public YoutubeTestDockerCommand() : base("youtube-docker", "Run YouTube video summary test in Docker (headless, parallel-safe)")
    {
        var urlOpt = new Option<string>("--url", () => "https://www.youtube.com/watch?v=psbAgsMD8QM", "YouTube video URL");
        var parallelOpt = new Option<string[]>("--parallel", "Video IDs or URLs to run in parallel")
        {
            AllowMultipleArgumentsPerToken = true
        };
        var ollamaOpt = new Option<string>("--ollama-url", () => "http://host.docker.internal:11434", "Ollama base URL for containers");
        var withAudioOpt = new Option<bool>("--with-audio", () => false, "Transcribe video audio first (Whisper tiny) and pass to vision");
        var verboseOpt = new Option<bool>("--verbose", () => false, "Verbose output");

        AddOption(urlOpt);
        AddOption(parallelOpt);
        AddOption(ollamaOpt);
        AddOption(withAudioOpt);
        AddOption(verboseOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var url = ctx.ParseResult.GetValueForOption(urlOpt)!;
            var parallel = ctx.ParseResult.GetValueForOption(parallelOpt) ?? Array.Empty<string>();
            var ollamaUrl = ctx.ParseResult.GetValueForOption(ollamaOpt)!;
            var withAudio = ctx.ParseResult.GetValueForOption(withAudioOpt);
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);
            await ExecuteAsync(url, parallel, ollamaUrl, withAudio, verbose);
        });
    }

    private static async Task ExecuteAsync(string defaultUrl, string[] parallel, string ollamaUrl, bool withAudio, bool verbose)
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

        if (parallel.Length > 0)
        {
            var tasks = parallel.Select(vid => RunOneAsync(NormalizeUrl(vid), ollamaUrl, withAudio, root, composePath, console));
            var results = await Task.WhenAll(tasks);
            var failed = results.Count(r => r != 0);
            console.WriteLine();
            console.WriteSuccess($"Reports: {reportDir}");
            Environment.ExitCode = failed;
        }
        else
        {
            Environment.ExitCode = await RunOneAsync(defaultUrl, ollamaUrl, withAudio, root, composePath, console);
            console.WriteLine();
            console.WriteSuccess($"Report: {reportDir}");
        }
    }

    private static async Task<int> RunOneAsync(string url, string ollamaUrl, bool withAudio, string root, string composePath, CliConsole console)
    {
        console.WriteLine($"Starting test for: {url}");
        var argList = new List<string>
        {
            "compose", "-f", composePath, "run", "--rm", "--no-deps",
            "-e", $"VIDEO_URL={url}",
            "-e", $"OLLAMA_BASE_URL={ollamaUrl}"
        };

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

    private static string NormalizeUrl(string vid)
    {
        if (vid.Contains("youtube") || vid.Contains("watch"))
            return vid;
        return $"https://www.youtube.com/watch?v={vid}";
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
}
