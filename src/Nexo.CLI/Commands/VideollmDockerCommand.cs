using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using Nexo.CLI.Output;
using Nexo.Core.Application.Paths;

namespace Nexo.CLI.Commands;

/// <summary>
/// Build and run VideoLLM-online in Docker for streaming video understanding.
/// Replaces scripts/run-videollm-online-docker.sh
/// </summary>
public sealed class VideollmDockerCommand : Command
{
    public VideollmDockerCommand() : base("videollm-docker", "Build and run VideoLLM-online in Docker (NVIDIA GPU required)")
    {
        var actionOpt = new Option<string>("--action", () => "up", "Action: build, up, or down");
        AddOption(actionOpt);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var action = ctx.ParseResult.GetValueForOption(actionOpt) ?? "up";
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var verbose = verboseOpt != null && ctx.ParseResult.GetValueForOption(verboseOpt);
            await ExecuteAsync(action, verbose);
        });
    }

    private static async Task ExecuteAsync(string action, bool verbose)
    {
        var console = new CliConsole(verbose);
        var root = RepoPathResolver.FindRepoRoot();
        var composeFile = Path.Combine(root, "docker-compose.videollm-online.yml");
        if (!File.Exists(composeFile))
        {
            console.WriteError($"Compose file not found: {composeFile}");
            Environment.ExitCode = 1;
            return;
        }

        var actionLower = action.Trim().ToLowerInvariant();
        switch (actionLower)
        {
            case "build":
                console.WriteLine("Building VideoLLM-online image...");
                var buildExit = await RunProcessAsync("docker", $"compose -f \"{composeFile}\" build", root);
                if (buildExit == 0)
                    console.WriteSuccess("Done. Run 'nexo demo videollm-docker --action up' to start.");
                Environment.ExitCode = buildExit;
                break;
            case "up":
                console.WriteLine("Starting VideoLLM-online (first run may download ~8GB model)...");
                var upExit = await RunProcessAsync("docker", $"compose -f \"{composeFile}\" up -d", root);
                if (upExit == 0)
                {
                    console.WriteLine();
                    console.WriteSuccess("VideoLLM-online Gradio UI: http://localhost:7860");
                    console.WriteLine($"Check logs: docker compose -f {composeFile} logs -f");
                }
                Environment.ExitCode = upExit;
                break;
            case "down":
                _ = await RunProcessAsync("docker", $"compose -f \"{composeFile}\" down", root);
                console.WriteSuccess("VideoLLM-online stopped.");
                Environment.ExitCode = 0;
                break;
            default:
                console.WriteError($"Unknown action: {action}");
                console.WriteLine("Usage: nexo demo videollm-docker --action [build|up|down]");
                Environment.ExitCode = 1;
                break;
        }
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
