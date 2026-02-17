using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Transcribes YouTube video audio using yt-dlp + Whisper tiny (smol audio pipeline).
/// Output transcript to stdout for use with youtube test: AUDIO_TRANSCRIPT=$(nexo demo youtube-transcribe URL)
/// </summary>
public sealed class YoutubeTranscribeCommand : Command
{
    public YoutubeTranscribeCommand() : base("youtube-transcribe", "Extract and transcribe YouTube audio (yt-dlp + Whisper tiny)")
    {
        var urlOpt = new Option<string>("--url", "YouTube video URL");
        urlOpt.IsRequired = true;
        AddOption(urlOpt);

        var modelOpt = new Option<string>("--model", () => "tiny", "Whisper model: tiny, base, small (tiny=39M params, smol default)");
        AddOption(modelOpt);

        var verboseOpt = new Option<bool>("--verbose", () => false, "Verbose output");
        AddOption(verboseOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var url = ctx.ParseResult.GetValueForOption(urlOpt)!;
            var model = ctx.ParseResult.GetValueForOption(modelOpt)!;
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);
            await ExecuteAsync(url, model, verbose);
        });
    }

    private static async Task ExecuteAsync(string videoUrl, string model, bool verbose)
    {
        var console = verbose ? new CliConsole(verbose: true) : null;
        var root = DiscoverProjectRoot();
        var dockerfile = Path.Combine(root, ".docker", "Dockerfile.youtube-transcribe");
        var buildContext = root;

        if (!File.Exists(dockerfile))
        {
            console?.WriteError($"Dockerfile not found: {dockerfile}");
            Console.Error.WriteLine($"Dockerfile not found: {dockerfile}");
            Environment.ExitCode = 1;
            return;
        }

        var imageName = "nexo-youtube-transcribe:latest";
        console?.WriteHeader("YouTube Audio Transcribe (Smol: Whisper tiny)");
        console?.WritePair("Video", videoUrl);
        console?.WritePair("Model", model);
        console?.WriteLine();

        // Build if needed (idempotent - docker build is fast when cached)
        console?.WriteLine("Building transcribe image...");
        var buildPsi = new ProcessStartInfo
        {
            FileName = "docker",
            ArgumentList = { "build", "-f", dockerfile, "-t", imageName, buildContext },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using (var build = Process.Start(buildPsi))
        {
            if (build == null)
            {
                console?.WriteError("Failed to start docker build");
                Environment.ExitCode = 1;
                return;
            }
            await build.WaitForExitAsync();
            if (build.ExitCode != 0)
            {
                var err = await build.StandardError.ReadToEndAsync();
                console?.WriteError(err);
                Console.Error.WriteLine(err);
                Environment.ExitCode = 1;
                return;
            }
        }

        // Run transcribe container
        var runPsi = new ProcessStartInfo
        {
            FileName = "docker",
            ArgumentList =
            {
                "run", "--rm",
                "-e", $"VIDEO_URL={videoUrl}",
                "-e", $"WHISPER_MODEL={model}",
                imageName
            },
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var run = Process.Start(runPsi);
        if (run == null)
        {
            console?.WriteError("Failed to start docker run");
            Environment.ExitCode = 1;
            return;
        }

        var stdout = run.StandardOutput;
        var stderr = run.StandardError;
        var outTask = stdout.ReadToEndAsync();
        var errTask = stderr.ReadToEndAsync();
        await run.WaitForExitAsync();
        var transcript = await outTask;
        var stderrContent = await errTask;

        if (verbose && !string.IsNullOrEmpty(stderrContent))
            console?.WriteLine(stderrContent.TrimEnd());

        Console.Write(transcript);
        Environment.ExitCode = run.ExitCode;
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
