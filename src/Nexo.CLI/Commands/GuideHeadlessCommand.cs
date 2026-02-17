using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Headless pass for Nexo Guide: DI test, app run with --headless, full-app agent test.
/// Replaces scripts/run-guide-headless.sh
/// </summary>
public sealed class GuideHeadlessCommand : Command
{
    public GuideHeadlessCommand() : base("guide-headless", "Run headless Guide validation (DI test, app headless, full-app agent)")
    {
        this.SetHandler(async (InvocationContext ctx) =>
        {
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var verbose = verboseOpt != null && ctx.ParseResult.GetValueForOption(verboseOpt);
            await ExecuteAsync(verbose);
        });
    }

    private static async Task ExecuteAsync(bool verbose)
    {
        var console = new CliConsole(verbose);
        var root = FindRepoRoot();
        var testProject = Path.Combine(root, "src", "Nexo.Tests.Infrastructure", "Nexo.Tests.Infrastructure.csproj");
        var guideProject = Path.Combine(root, "src", "Nexo.Guide", "Nexo.Guide.csproj");

        console.WriteLine("=== 1. Headless DI test (execution pipeline resolves) ===");
        var diExit = await RunProcessAsync("dotnet", $"test \"{testProject}\" -f net8.0 --filter \"Guide_HeadlessDI\" --no-build -p:TreatWarningsAsErrors=false", root);
        if (diExit != 0)
        {
            _ = await RunProcessAsync("dotnet", $"test \"{testProject}\" -f net8.0 --filter \"Guide_HeadlessDI\" -p:TreatWarningsAsErrors=false", root);
            diExit = 1;
        }
        if (diExit != 0)
        {
            console.WriteError($"FAIL: Headless DI test failed (exit {diExit})");
            Environment.ExitCode = 1;
            return;
        }
        console.WriteSuccess("PASS: Headless DI test");
        console.WriteLine();

        console.WriteLine("=== 2. Build and run Guide in headless mode ===");
        var logPath = Path.Combine(Path.GetTempPath(), "nexo-guide-headless.log");
        _ = await RunProcessAsync("dotnet", $"build \"{guideProject}\" -f net8.0 -p:TreatWarningsAsErrors=false -v q", root);
        var (appExit, appOut, appErr) = await RunProcessWithOutputAsync("dotnet", $"run --project \"{guideProject}\" -f net8.0 -p:TreatWarningsAsErrors=false --no-build -- --headless", root);
        await File.WriteAllTextAsync(logPath, appOut + "\n" + appErr);
        if (appExit != 0)
        {
            console.WriteError($"FAIL: App headless run exited with {appExit}");
            foreach (var line in (appOut + "\n" + appErr).Split('\n').TakeLast(40))
                console.WriteLine(line);
            Environment.ExitCode = 1;
            return;
        }
        var log = appOut + "\n" + appErr;
        if (log.Contains("Unable to resolve service"))
        {
            console.WriteError("FAIL: DI resolution error in app");
            foreach (var line in log.Split('\n').TakeLast(30))
                console.WriteLine(line);
            Environment.ExitCode = 1;
            return;
        }
        console.WriteSuccess("PASS: App headless run (exit 0)");
        console.WriteLine();

        console.WriteLine("=== 3. Full-app test via Universal Tester agent (Guide as CLI target, headless) ===");
        var agentExit = await RunProcessAsync("dotnet", $"test \"{testProject}\" -f net8.0 --filter \"FullyQualifiedName~GuideFullAppUniversalTesterTests\" -p:TreatWarningsAsErrors=false --no-build", root);
        if (agentExit != 0)
        {
            console.WriteError($"FAIL: Universal Tester agent full-app test failed (exit {agentExit})");
            Environment.ExitCode = 1;
            return;
        }
        console.WriteSuccess("PASS: Full-app agent test");
        Environment.ExitCode = 0;
    }

    private static async Task<(int exitCode, string stdout, string stderr)> RunProcessWithOutputAsync(string fileName, string arguments, string workingDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi);
        if (p == null) return (-1, "", "");
        var outTask = p.StandardOutput.ReadToEndAsync();
        var errTask = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        return (p.ExitCode, await outTask, await errTask);
    }

    private static async Task<int> RunProcessAsync(string fileName, string arguments, string workingDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi);
        if (p == null) return -1;
        await p.WaitForExitAsync();
        return p.ExitCode;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nexo.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Environment.CurrentDirectory;
    }
}
