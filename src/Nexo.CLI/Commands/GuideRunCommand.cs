using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nexo.CLI.Output;
using Nexo.Core.Application.Paths;

namespace Nexo.CLI.Commands;

/// <summary>
/// Build and run Nexo.Guide on Mac Catalyst or iOS Simulator.
/// Replaces scripts/run-guide-maccatalyst.sh and scripts/run-guide-ios-simulator.sh
/// </summary>
public sealed class GuideRunCommand : Command
{
    public GuideRunCommand() : base("guide-run", "Run Nexo.Guide on Mac Catalyst or iOS Simulator")
    {
        var targetOpt = new Option<string>("--target", () => "maccatalyst", "Target: maccatalyst or ios");
        var argsOpt = new Option<string[]>("--", "Arguments to pass to the app")
        {
            AllowMultipleArgumentsPerToken = true
        };
        AddOption(targetOpt);
        AddOption(argsOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var target = ctx.ParseResult.GetValueForOption(targetOpt) ?? "maccatalyst";
            var args = ctx.ParseResult.GetValueForOption(argsOpt) ?? Array.Empty<string>();
            await ExecuteAsync(target, args);
        });
    }

    private static async Task ExecuteAsync(string target, string[] args)
    {
        var console = new CliConsole(verbose: true);
        var root = RepoPathResolver.FindRepoRoot();
        var guideProject = Path.Combine(root, "src", "Nexo.Guide", "Nexo.Guide.csproj");

        if (!File.Exists(guideProject))
        {
            console.WriteError($"Guide project not found: {guideProject}");
            Environment.ExitCode = 1;
            return;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            console.WriteError("Guide run (maccatalyst/ios) requires macOS.");
            Environment.ExitCode = 1;
            return;
        }

        var tfm = target.Trim().ToLowerInvariant() switch
        {
            "ios" => "net9.0-ios",
            "maccatalyst" or "catalyst" => "net9.0-maccatalyst",
            _ => "net9.0-maccatalyst"
        };

        var argList = new List<string> { "run", "--project", $"\"{guideProject}\"", "-f", tfm, "-p:TreatWarningsAsErrors=false" };
        argList.AddRange(args);

        console.WriteLine($"Running Nexo.Guide ({tfm})...");
        var exitCode = await RunProcessAsync("dotnet", string.Join(" ", argList), root);
        Environment.ExitCode = exitCode;
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
}
