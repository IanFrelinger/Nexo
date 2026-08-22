using System.Text.Json;

namespace Ashlar.CLI.Runtime;

/// <summary>Workflow lab history store.</summary>
public static class WorkflowLabHistoryStore
{
    private const string RelativePath = ".ashlar/runtime/workflow_lab_history.jsonl";
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Resolves the workflow lab history file path under the repository root.</summary>
    public static string GetPath(string repoRoot)
        => Path.GetFullPath(Path.Combine(repoRoot, RelativePath));

    /// <summary>Reads the most recent history rows ordered by start time descending.</summary>
    public static IReadOnlyList<WorkflowLabStressHistoryRow> ReadRecent(string repoRoot, int maxItems = 300)
    {
        if (maxItems <= 0)
            return Array.Empty<WorkflowLabStressHistoryRow>();

        var path = GetPath(repoRoot);
        if (!File.Exists(path))
            return Array.Empty<WorkflowLabStressHistoryRow>();

        var parsed = new List<WorkflowLabStressHistoryRow>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            try
            {
                var item = JsonSerializer.Deserialize<WorkflowLabStressHistoryRow>(line, DeserializeOptions);
                if (item != null)
                    parsed.Add(item);
            }
            catch
            {
                // Keep history resilient to malformed lines.
            }
        }

        return parsed
            .OrderByDescending(p => p.StartedAtUtc)
            .Take(maxItems)
            .ToArray();
    }

    /// <summary>Reads all history rows ordered by start time descending.</summary>
    public static IReadOnlyList<WorkflowLabStressHistoryRow> ReadAll(string repoRoot)
    {
        var path = GetPath(repoRoot);
        if (!File.Exists(path))
            return Array.Empty<WorkflowLabStressHistoryRow>();

        var parsed = new List<WorkflowLabStressHistoryRow>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            try
            {
                var item = JsonSerializer.Deserialize<WorkflowLabStressHistoryRow>(line, DeserializeOptions);
                if (item != null)
                    parsed.Add(item);
            }
            catch
            {
                // Keep history resilient to malformed lines.
            }
        }

        return parsed
            .OrderByDescending(p => p.StartedAtUtc)
            .ToArray();
    }

    /// <summary>Appends a history row to the workflow lab JSONL store.</summary>
    public static void Append(string repoRoot, WorkflowLabStressHistoryRow report)
    {
        var path = GetPath(repoRoot);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var line = JsonSerializer.Serialize(report);
        File.AppendAllText(path, line + Environment.NewLine);
    }
}
