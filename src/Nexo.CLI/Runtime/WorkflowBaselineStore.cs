using System.Text.Json;

namespace Nexo.CLI.Runtime;

public sealed record WorkflowGatePolicySpec
{
    public string? Name { get; init; }
    public string? BenchmarkSet { get; init; }
    public double? MinSuccessRateDelta { get; init; }
    public long? MaxP95LatencyRegressionMs { get; init; }
    public long? MaxAverageLatencyRegressionMs { get; init; }
    public double? MinAverageScoreDelta { get; init; }
    public int? MaxRegressedScenarios { get; init; }
}

public sealed record WorkflowBaselineRecord
{
    public string BaselineId { get; init; } = string.Empty;
    public string BenchmarkSet { get; init; } = "workflow-lab";
    public string RunId { get; init; } = string.Empty;
    public string GitSha { get; init; } = string.Empty;
    public string SpecHash { get; init; } = string.Empty;
    public string ProviderSnapshot { get; init; } = string.Empty;
    public DateTimeOffset PromotedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public bool Active { get; init; } = true;
    public string? Notes { get; init; }
    public WorkflowGatePolicySpec? Policy { get; init; }
}

public static class WorkflowBaselineStore
{
    private const string RelativePath = ".nexo/runtime/workflow_lab_baselines.json";

    public static string GetPath(string repoRoot)
        => Path.GetFullPath(Path.Combine(repoRoot, RelativePath));

    public static IReadOnlyList<WorkflowBaselineRecord> ReadAll(string repoRoot)
    {
        var path = GetPath(repoRoot);
        if (!File.Exists(path))
            return Array.Empty<WorkflowBaselineRecord>();

        try
        {
            var json = File.ReadAllText(path);
            var parsed = JsonSerializer.Deserialize<IReadOnlyList<WorkflowBaselineRecord>>(json);
            return (parsed ?? Array.Empty<WorkflowBaselineRecord>())
                .OrderByDescending(x => x.PromotedAtUtc)
                .ToArray();
        }
        catch
        {
            return Array.Empty<WorkflowBaselineRecord>();
        }
    }

    public static WorkflowBaselineRecord? ReadActive(string repoRoot, string benchmarkSet)
    {
        var normalized = NormalizeBenchmarkSet(benchmarkSet);
        return ReadAll(repoRoot)
            .FirstOrDefault(x =>
                x.Active &&
                string.Equals(x.BenchmarkSet, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static WorkflowBaselineRecord? ReadById(string repoRoot, string baselineId)
    {
        if (string.IsNullOrWhiteSpace(baselineId))
            return null;
        var normalized = baselineId.Trim();
        return ReadAll(repoRoot)
            .FirstOrDefault(x => string.Equals(x.BaselineId, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static WorkflowBaselineRecord Promote(string repoRoot, WorkflowBaselineRecord candidate)
    {
        var baseline = candidate with
        {
            BaselineId = string.IsNullOrWhiteSpace(candidate.BaselineId)
                ? BuildBaselineId(candidate.BenchmarkSet, candidate.RunId)
                : candidate.BaselineId.Trim(),
            BenchmarkSet = NormalizeBenchmarkSet(candidate.BenchmarkSet),
            RunId = candidate.RunId.Trim(),
            PromotedAtUtc = candidate.PromotedAtUtc == default ? DateTimeOffset.UtcNow : candidate.PromotedAtUtc,
            Active = true
        };

        var all = ReadAll(repoRoot).ToList();
        for (var i = 0; i < all.Count; i++)
        {
            if (string.Equals(all[i].BenchmarkSet, baseline.BenchmarkSet, StringComparison.OrdinalIgnoreCase) && all[i].Active)
                all[i] = all[i] with { Active = false };
        }

        all.Add(baseline);
        Persist(repoRoot, all);
        return baseline;
    }

    private static void Persist(string repoRoot, IReadOnlyList<WorkflowBaselineRecord> items)
    {
        var path = GetPath(repoRoot);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(items.OrderByDescending(x => x.PromotedAtUtc).ToArray(), new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(path, json);
    }

    private static string NormalizeBenchmarkSet(string value)
    {
        var normalized = (value ?? "workflow-lab").Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "workflow-lab" : normalized;
    }

    private static string BuildBaselineId(string benchmarkSet, string runId)
    {
        var prefix = NormalizeBenchmarkSet(benchmarkSet).Replace(' ', '-');
        var suffix = string.IsNullOrWhiteSpace(runId)
            ? Guid.NewGuid().ToString("N")[..8]
            : runId.Trim();
        return $"{prefix}-{suffix}";
    }
}
