using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Agents;
using Nexo.Core.Domain.Evidence;
using Nexo.Runtime;

namespace Nexo.BRPlaytestAgent;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var daemon = args.Contains("--daemon", StringComparer.OrdinalIgnoreCase);
        var virtualPlayer = args.Contains(
            "--virtual-player",
            StringComparer.OrdinalIgnoreCase);
        var recordVideo = args.Contains(
            "--record-video",
            StringComparer.OrdinalIgnoreCase);
        var interval = TimeSpan.FromMinutes(
            ReadIntArgument(args, "--interval-minutes", 30));

        do
        {
            var exitCode = await RunCycleAsync(
                    virtualPlayer,
                    recordVideo,
                    CancellationToken.None)
                .ConfigureAwait(false);
            if (!daemon)
                return exitCode;

            var delay = exitCode == 0
                ? interval
                : TimeSpan.FromMinutes(2);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                status = "waiting",
                lastCycle = exitCode == 0 ? "passed" : "failed",
                nextRun = DateTimeOffset.UtcNow + delay
            }));
            await Task.Delay(delay).ConfigureAwait(false);
        } while (true);
    }

    private static async Task<int> RunCycleAsync(
        bool virtualPlayer,
        bool recordVideo,
        CancellationToken cancellationToken)
    {
        var nexoRoot = FindNexoRoot();
        var gameRoot = Path.GetFullPath(
            Environment.GetEnvironmentVariable("NEXO_BR_GAME_ROOT")
            ?? Path.Combine(nexoRoot, "..", "NexoBattleRoyale"));
        var buildPath = Path.GetFullPath(
            Environment.GetEnvironmentVariable("NEXO_BR_GAME_BUILD")
            ?? Path.Combine(
                gameRoot,
                "Builds",
                "WeaponLab",
                "NexoBRWeaponLab.app"));
        var artifactRoot = Path.GetFullPath(
            Environment.GetEnvironmentVariable("NEXO_BR_PLAYTEST_ARTIFACT_ROOT")
            ?? Path.Combine(nexoRoot, ".nexo", "playtest", "br-weapon-lab"));
        Directory.CreateDirectory(artifactRoot);

        await using var session = new PlaytestSession
        {
            BuildPath = buildPath,
            ArtifactRoot = artifactRoot,
            Port = ReadEnvironmentInt("NEXO_BR_PLAYTEST_PORT", 17420),
            Token = Environment.GetEnvironmentVariable("NEXO_BR_PLAYTEST_TOKEN")
                    ?? Guid.NewGuid().ToString("N"),
            VirtualPlayerMode = virtualPlayer,
            BuiltInCaptureMode = recordVideo,
            BuiltInCaptureOutputPath = recordVideo
                ? Path.Combine(
                    artifactRoot,
                    "videos",
                    "complete-weapon-showcase.mp4")
                : null
        };

        var tools = new CapabilityRegistry();
        tools.Register(new GameLaunchTool(session));
        tools.Register(new GameStatusTool(session));
        tools.Register(new GameKeyboardTool(session));
        tools.Register(new GameApproachPickupTool(session));
        tools.Register(new GameLookTool(session));
        tools.Register(new GameScreenshotTool(session));
        tools.Register(new GameNearbyTool(session));
        tools.Register(new DesktopScreenshotTool(session));
        tools.Register(new OsAccessTool(session));
        tools.Register(new OsKeyboardTool(session));
        tools.Register(new OsMouseTool(session));
        tools.Register(new PlaytestReportTool(session));
        tools.Register(new VirtualPlaytestReportTool(session));
        tools.Register(new GameStopTool(session));

        // The capability registry itself is the allowlist: no repo write, shell,
        // network export, or arbitrary process tools are registered.
        var policies = new PolicyEngine(Array.Empty<IPolicy>());
        var model = new ScriptedPlaytestModel(virtualPlayer, recordVideo);
        var agent = new ToolCallingAgent(
            "nexo-br-playtester",
            model,
            NullLogger<ToolCallingAgent>.Instance,
            objective:
                "Playtest the BR Weapon Lab locally. Exercise movement, pickup, reload, shooting, camera capture, renderer diagnostics, and write a report.",
            modelProvider: "deterministic-local",
            maxIterations: 12,
            perCycleDeadline: TimeSpan.FromMinutes(5));
        var snapshot = WorldSnapshot.ForRepo(
            gameRoot,
            artifactRoot);

        try
        {
            var cycle = await agent.RunCycleAsync(
                snapshot,
                tools,
                policies,
                onRejected: null,
                memory: tools.MemoryFor(agent),
                cancellationToken).ConfigureAwait(false);
            var reportPath = Path.Combine(artifactRoot, "report.json");
            IReadOnlyList<UploadedArtifact> driveUploads;
            try
            {
                driveUploads = await PlaytestArtifactUpload
                    .UploadVideosAsync(session.Videos, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception uploadException)
            {
                Console.Error.WriteLine(JsonSerializer.Serialize(new
                {
                    status = "drive_upload_failed",
                    error = uploadException.Message,
                    type = uploadException.GetType().Name
                }));
                return 1;
            }

            if (driveUploads.Count > 0)
            {
                await PlaytestArtifactUpload.PatchReportAsync(
                    reportPath,
                    session.Videos,
                    driveUploads,
                    cancellationToken).ConfigureAwait(false);
            }

            var reportPassed = false;
            if (File.Exists(reportPath))
            {
                using var reportDocument = JsonDocument.Parse(
                    await File.ReadAllTextAsync(
                        reportPath,
                        cancellationToken).ConfigureAwait(false));
                reportPassed =
                    reportDocument.RootElement.TryGetProperty(
                        "verdict",
                        out var verdict) &&
                    verdict.GetString() == "passed";
            }
            var ok = reportPassed &&
                     cycle.StoppedReason is "empty" or "max_iterations";
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                status = ok ? "passed" : "failed",
                agent = agent.Name,
                cycle.Iterations,
                cycle.ToolCallsExecuted,
                cycle.ToolCallsDenied,
                cycle.StoppedReason,
                reportPath,
                screenshots = session.Screenshots,
                videos = session.Videos,
                driveUploads = driveUploads.Select(upload => new
                {
                    upload.FileName,
                    webViewLink = upload.WebViewUri.ToString(),
                    upload.Bytes,
                    upload.Provider
                })
            }, new JsonSerializerOptions { WriteIndented = true }));
            return ok ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new
            {
                status = "error",
                error = exception.Message,
                type = exception.GetType().Name
            }));
            return 1;
        }
    }

    private static string FindNexoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Nexo.sln")))
                return current.FullName;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Run Nexo.BRPlaytestAgent from the Nexo repository.");
    }

    private static int ReadEnvironmentInt(string name, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(name), out var value)
            ? value
            : fallback;

    private static int ReadIntArgument(
        string[] args,
        string name,
        int fallback)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(args[index + 1], out var value))
            {
                return value;
            }
        }

        return fallback;
    }
}
