using System.Text.Json;

namespace Nexo.CLI.Runtime;

public sealed record WorkflowLabStressHistoryRow
{
    public string ScenarioId { get; init; } = string.Empty;
    public string RequestId { get; init; } = string.Empty;
    public string CompositionId { get; init; } = string.Empty;
    public string ModelProfileId { get; init; } = string.Empty;
    public int Iteration { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public long ElapsedMs { get; init; }
    public bool Success { get; init; }
    public int AgentCount { get; init; }
    public int ConflictCount { get; init; }
    public int EscalationCount { get; init; }
    public double Score { get; init; }
    public string? Summary { get; init; }
    public string BenchmarkSet { get; init; } = "workflow-lab";
}

public static class WorkflowLabHistoryStore
{
    private const string RelativePath = ".nexo/runtime/workflow_lab_history.jsonl";

    public static string GetPath(string repoRoot)
        => Path.GetFullPath(Path.Combine(repoRoot, RelativePath));

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
                var item = JsonSerializer.Deserialize<WorkflowLabStressHistoryRow>(line);
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
