using System.Text.Json;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Forge;
using Nexo.BackgroundAgents.Objectives;
using Nexo.BackgroundAgents.Observations;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// Read-only snapshot of runtime-studio disk state for the local operator dashboard.
/// </summary>
public static class RuntimeStudioOperatorDashboardSummary
{
    public sealed record PathsInfo(
        string ObjectivesRoot,
        string ForgeRoot,
        string ObservationsPath,
        string AgentModePath);

    public static PathsInfo ResolvePaths()
    {
        var obj = Environment.GetEnvironmentVariable("NEXO_OBJECTIVES_ROOT");
        var objectivesRoot = string.IsNullOrWhiteSpace(obj)
            ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ObjectiveStore.DefaultRelativePath))
            : Path.GetFullPath(obj.Trim());

        var forge = Environment.GetEnvironmentVariable("NEXO_FORGE_ROOT");
        var forgeRoot = string.IsNullOrWhiteSpace(forge)
            ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".nexo", "runtime-studio", "forge"))
            : Path.GetFullPath(forge.Trim());

        var obs = Environment.GetEnvironmentVariable("NEXO_OBSERVATIONS_PATH");
        var obsPath = string.IsNullOrWhiteSpace(obs)
            ? Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), JsonlObservationStore.DefaultRelativePath))
            : Path.GetFullPath(obs.Trim());

        var modePath = Environment.GetEnvironmentVariable("NEXO_AGENT_MODE_PATH");
        var agentModePath = string.IsNullOrWhiteSpace(modePath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "agent-mode.json")
            : Path.GetFullPath(modePath.Trim());

        return new PathsInfo(objectivesRoot, forgeRoot, obsPath, agentModePath);
    }

    public static string BuildJson(PathsInfo paths)
    {
        var modeStore = new FileBasedAggressivenessModeStore(paths.AgentModePath);
        var mode = modeStore.GetMode().ToString();

        var objectives = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in Enum.GetValues<ObjectiveStatus>())
        {
            var dir = Path.Combine(paths.ObjectivesRoot, ObjectiveFolder(s));
            var n = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.md").Length : 0;
            objectives[s.ToString()] = n;
        }

        var proposals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in Enum.GetValues<ChangeProposalStatus>())
        {
            var dir = Path.Combine(paths.ForgeRoot, s.ToString().ToLowerInvariant());
            var n = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.json").Length : 0;
            proposals[s.ToString()] = n;
        }

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
            objectives,
            proposals,
            observationsTail = tail
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string ObjectiveFolder(ObjectiveStatus status) => status switch
    {
        ObjectiveStatus.Pending => "pending",
        ObjectiveStatus.InProgress => "in_progress",
        ObjectiveStatus.Done => "done",
        ObjectiveStatus.Blocked => "blocked",
        _ => "pending"
    };
}
