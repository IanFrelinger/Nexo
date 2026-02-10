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
            "Start Ollama in Docker and pull vision model (llava:7b) before running the test");
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
            var scriptPath = Path.Combine(root, "scripts", "run-guide-agentic-test-docker.sh");
            if (!File.Exists(scriptPath))
            {
                if (!json && console != null) console.WriteError($"Script not found: {scriptPath}");
                return 1;
            }

            if (!json && console != null)
            {
                console.WriteHeader("Agentic Guide test (Ollama via Docker)");
                console.WriteLine("Starting Ollama in Docker, then running test...");
            }

            var psi = new ProcessStartInfo
            {
                FileName = "bash",
                ArgumentList = { scriptPath },
                WorkingDirectory = root,
                UseShellExecute = false
            };
            using var p = Process.Start(psi);
            if (p == null)
            {
                if (!json && console != null) console.WriteError("Failed to start script.");
                return 1;
            }

            await p.WaitForExitAsync();
            return p.ExitCode;
        }

        // Run test directly (Ollama must already be running)
        Environment.SetEnvironmentVariable("NEXO_RUN_AGENTIC_GUIDE_TEST", "1");
        var testProject = Path.Combine(root, "src", "Nexo.Tests.Infrastructure", "Nexo.Tests.Infrastructure.csproj");
        if (!File.Exists(testProject))
        {
            if (!json && console != null) console.WriteError($"Test project not found: {testProject}");
            return 1;
        }

        if (!json && console != null)
        {
            console.WriteHeader("Agentic Guide test");
            console.WriteLine("Ensure Ollama is running (e.g. ollama serve, ollama pull llava:7b).");
            console.WriteLine("Running test...");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList =
            {
                "test", testProject, "-f", "net8.0",
                "--filter", "FullyQualifiedName~Guide_Agentic_UniversalTester_ShouldTestAllInteractions_WhenRunWithOllama",
                "-p:TreatWarningsAsErrors=false",
                "--logger", "console;verbosity=normal"
            },
            WorkingDirectory = root,
            UseShellExecute = false
        };

        using var testProc = Process.Start(startInfo);
        if (testProc == null)
        {
            if (!json && console != null) console.WriteError("Failed to start dotnet test.");
            return 1;
        }

        await testProc.WaitForExitAsync();
        return testProc.ExitCode;
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
