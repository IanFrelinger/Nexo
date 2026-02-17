using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Runs the agentic Guide test via CLI: Universal Tester agent against the Nexo Guide app
/// (desktop UI). Optionally starts Ollama via Docker first.
/// </summary>
public static class TestGuideAgenticCommand
{
    public static Command CreateCommand()
    {
        var dockerOpt = new Option<bool>("--docker", () => false,
            "Start Ollama in Docker and pull vision model (SmolVLM2/llava) before running the test");
        var skipUiOpt = new Option<bool>("--skip-ui", () => false,
            "Skip when NEXO_SKIP_UI_TESTS=1 or no display (e.g. in CI)");

        var cmd = new Command("guide-agentic", "Run agentic Guide test (Universal Tester + Ollama vs Guide app)")
        {
            dockerOpt,
            skipUiOpt
        };

        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var useDocker = ctx.ParseResult.GetValueForOption(dockerOpt);
            var skipUi = ctx.ParseResult.GetValueForOption(skipUiOpt);
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var jsonOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--format-json");
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var json = jsonOpt != null && ctx.ParseResult.GetValueForOption(jsonOpt);
            var verbose = verboseOpt != null && ctx.ParseResult.GetValueForOption(verboseOpt);

            var exitCode = await ExecuteAsync(useDocker, skipUi, json, verbose);
            ctx.ExitCode = exitCode;
        });

        return cmd;
    }

    public static async Task<int> ExecuteAsync(bool useDocker, bool skipUi, bool json, bool verbose)
    {
        var console = json ? null : new CliConsole(verbose);
        var root = FindRepoRoot();

        if (skipUi && string.Equals(Environment.GetEnvironmentVariable("NEXO_SKIP_UI_TESTS"), "1", StringComparison.OrdinalIgnoreCase))
        {
            if (!json && console != null) console.WriteLine("Skipping (NEXO_SKIP_UI_TESTS=1).");
            return 0;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
        {
            if (!json && console != null) console.WriteLine("Skipping (no DISPLAY on Linux).");
            return 0;
        }

        if (useDocker)
        {
            if (!json && console != null)
            {
                console.WriteHeader("Agentic Guide test (Ollama via Docker)");
                console.WriteLine("Starting Ollama in Docker, then running test...");
            }

            var ollamaModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2:3b";
            var ollamaVision = Environment.GetEnvironmentVariable("OLLAMA_VISION_MODEL") ?? "richardyoung/smolvlm2-2.2b-instruct";
            var container = Environment.GetEnvironmentVariable("OLLAMA_CONTAINER") ?? "ollama";

            // Start Ollama container if not running
            var psOutput = await RunProcessOutputAsync("docker", "ps --format '{{.Names}}'");
            if (!psOutput.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries).Contains(container))
            {
                var psAOutput = await RunProcessOutputAsync("docker", "ps -a --format '{{.Names}}'");
                if (psAOutput.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries).Contains(container))
                {
                    if (!json && console != null) console.WriteLine($"Starting existing container {container}...");
                    _ = await RunProcessAsync("docker", $"start {container}", root);
                }
                else
                {
                    var image = Environment.GetEnvironmentVariable("OLLAMA_IMAGE") ?? "ollama/ollama";
                    if (!json && console != null) console.WriteLine($"Pulling {image} and starting {container}...");
                    _ = await RunProcessAsync("docker", $"run -d -p 11434:11434 --name {container} {image}", root);
                }

                if (!json && console != null) console.WriteLine("Waiting for Ollama to be ready...");
                for (var i = 0; i < 30; i++)
                {
                    try
                    {
                        using var c = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                        var r = await c.GetAsync("http://localhost:11434/api/tags");
                        if (r.IsSuccessStatusCode) break;
                    }
                    catch { /* ignore */ }
                    await Task.Delay(1000);
                }
            }

            // Ensure models are available
            var tags = await RunProcessOutputAsync("curl", "-s http://localhost:11434/api/tags");
            if (!tags.Contains($"\"name\":\"{ollamaModel}\""))
            {
                if (!json && console != null) console.WriteLine($"Pulling text model {ollamaModel}...");
                _ = await RunProcessAsync("docker", $"exec {container} ollama pull {ollamaModel}", root);
            }
            if (!tags.Contains($"\"name\":\"{ollamaVision}\""))
            {
                if (!json && console != null) console.WriteLine($"Pulling vision model {ollamaVision}...");
                _ = await RunProcessAsync("docker", $"exec {container} ollama pull {ollamaVision}", root);
            }

            Environment.SetEnvironmentVariable("NEXO_RUN_AGENTIC_GUIDE_TEST", "1");
            Environment.SetEnvironmentVariable("OLLAMA_MODEL", ollamaModel);
            Environment.SetEnvironmentVariable("OLLAMA_VISION_MODEL", ollamaVision);
        }

        // Run test (Ollama must already be running when useDocker is false)
        Environment.SetEnvironmentVariable("NEXO_RUN_AGENTIC_GUIDE_TEST", "1");
        Environment.SetEnvironmentVariable("NEXO_SKIP_UI_TESTS", "");

        var testProject = Path.Combine(root, "src", "Nexo.Tests.Infrastructure", "Nexo.Tests.Infrastructure.csproj");
        if (!File.Exists(testProject))
        {
            if (!json && console != null) console.WriteError($"Test project not found: {testProject}");
            return 1;
        }

        if (!json && console != null && !useDocker)
        {
            console.WriteHeader("Agentic Guide test");
            console.WriteLine("Ensure Ollama is running (e.g. ollama serve, ollama pull llava:7b).");
            console.WriteLine("Running test...");
        }

        var filter = useDocker
            ? "FullyQualifiedName~Guide_Agentic_UniversalTester_ShouldTestAllInteractions_WhenRunWithOllama"
            : "FullyQualifiedName~Guide_Agentic_UniversalTester_ShouldTestAllInteractions|FullyQualifiedName~Guide_AvaloniaApp_UniversalTester_ShouldPerformClicksAndKeystrokes";

        var exitCode = await RunProcessAsync("dotnet", $"test \"{testProject}\" -f net8.0 --filter \"{filter}\" -p:TreatWarningsAsErrors=false --logger \"console;verbosity=normal\"", root);
        return exitCode;
    }

    private static async Task<int> RunProcessAsync(string fileName, string arguments, string? workingDir = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDir ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi);
        if (p == null) return -1;
        await p.WaitForExitAsync();
        return p.ExitCode;
    }

    private static async Task<string> RunProcessOutputAsync(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi);
        if (p == null) return "";
        return await p.StandardOutput.ReadToEndAsync();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            var sln = Path.Combine(dir.FullName, "Nexo.sln");
            if (File.Exists(sln)) return dir.FullName;
            dir = dir.Parent;
        }
        return Environment.CurrentDirectory;
    }
}
