using System.Text.Json;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.BackgroundAgents.Forge;
using Ashlar.BackgroundAgents.Objectives;
using Ashlar.BackgroundAgents.RuntimeStudio;

namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>
/// Read-only snapshot of runtime-studio disk state for the local operator dashboard.
/// </summary>
public static class RuntimeStudioOperatorDashboardSummary
{
    /// <summary>Creates a new PathsInfo instance.</summary>
    public sealed record PathsInfo(
        string ObjectivesRoot,
        string ForgeRoot,
        string ObservationsPath,
        string AgentModePath);

    /// <summary>Creates a new ResolvePaths instance.</summary>
    public static PathsInfo ResolvePaths()
    {
        var p = RuntimeStudioPathResolver.Resolve(Directory.GetCurrentDirectory());
        var modePath = Environment.GetEnvironmentVariable("ASHLAR_AGENT_MODE_PATH");
        var agentModePath = string.IsNullOrWhiteSpace(modePath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ashlar", "agent-mode.json")
            : Path.GetFullPath(modePath.Trim());

        return new PathsInfo(p.ObjectivesRoot, p.ForgeRoot, p.ObservationsPath, agentModePath);
    }

    /// <summary>Creates a new BuildJson instance.</summary>
    public static string BuildJson(PathsInfo paths)
    {
        var modeStore = new FileBasedAggressivenessModeStore(paths.AgentModePath);
        var mode = modeStore.GetMode().ToString();

        var objStore = new ObjectiveStore(paths.ObjectivesRoot);
        var forgeStore = new ChangeProposalStore(paths.ForgeRoot);
        var disk = RuntimeStudioMetricsCollector.Collect(objStore, forgeStore, paths.ObservationsPath);

        var tail = new List<string>();
        try
        {
            if (File.Exists(paths.ObservationsPath))
            {
                using var fs = new FileStream(paths.ObservationsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var len = fs.Length;
                var take = (int)Math.Min(len, 16_384);
                if (take > 0)
                {
                    fs.Seek(-take, SeekOrigin.End);
                    using var sr = new StreamReader(fs);
                    var chunk = sr.ReadToEnd();
                    var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var line in lines.TakeLast(12))
                    {
                        if (line.Length > 400) tail.Add(line[..400] + "…");
                        else tail.Add(line);
                    }
                }
            }
        }
        catch
        {
            /* best-effort */
        }

        var payload = new
        {
            generatedAt = DateTimeOffset.UtcNow,
            paths = new { paths.ObjectivesRoot, paths.ForgeRoot, paths.ObservationsPath, paths.AgentModePath },
            mode,
            objectives = disk.ObjectivesByStatus,
            proposals = disk.ProposalsByStatus,
            observationsTail = tail,
            metrics = disk
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }
}
