using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexo.CLI.Runtime;

public sealed record AdaptiveRuntimeExecutionReport
{
    public string RunId { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public long ElapsedMs { get; init; }
    public string GoalFingerprint { get; init; } = string.Empty;
    public string GoalPreview { get; init; } = string.Empty;
    public string BenchmarkSet { get; init; } = "adhoc";
    public string RequestedQaPolicy { get; init; } = "auto";
    public string ResolvedQaPolicy { get; init; } = "demo";
    public string BootstrapProfile { get; init; } = "self-extend-functional";
    public bool RunVisualQa { get; init; }
    public string VisualQaFallbackPolicy { get; init; } = "degrade";
    public bool Success { get; init; }
    public string FailureStage { get; init; } = "none";
    public bool BootstrapOk { get; init; }
    public bool PreflightRan { get; init; }
    public bool PreflightOk { get; init; }
    public bool SelfExtendRan { get; init; }
    public bool SelfExtendOk { get; init; }
    public string[] PlanReasons { get; init; } = Array.Empty<string>();
    public string? AdaptivePolicyReason { get; init; }

    public static string ComputeGoalFingerprint(string goal)
    {
        var normalized = NormalizeGoal(goal);
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string BuildGoalPreview(string goal, int maxLength = 160)
    {
        var normalized = NormalizeGoal(goal);
        if (normalized.Length <= maxLength)
            return normalized;
        return normalized[..maxLength] + "...";
    }

    private static string NormalizeGoal(string goal)
    {
        var trimmed = (goal ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        var collapsed = string.Join(
            ' ',
            trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return collapsed.Length <= 2000 ? collapsed : collapsed[..2000];
    }
}

public static class AdaptiveRuntimeExecutionHistoryStore
{
    private const string RelativePath = ".nexo/runtime/runtime_execute_history.jsonl";

    public static string GetPath(string repoRoot)
        => Path.GetFullPath(Path.Combine(repoRoot, RelativePath));

    public static IReadOnlyList<AdaptiveRuntimeExecutionReport> ReadRecent(string repoRoot, int maxItems = 200)
    {
        if (maxItems <= 0)
            return Array.Empty<AdaptiveRuntimeExecutionReport>();

        var path = GetPath(repoRoot);
        if (!File.Exists(path))
            return Array.Empty<AdaptiveRuntimeExecutionReport>();

        var parsed = new List<AdaptiveRuntimeExecutionReport>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            try
            {
                var item = JsonSerializer.Deserialize<AdaptiveRuntimeExecutionReport>(line);
                if (item != null)
                    parsed.Add(item);
            }
            catch
            {
                // Ignore malformed lines to keep store resilient.
            }
        }

        return parsed
            .OrderByDescending(p => p.StartedAtUtc)
            .Take(maxItems)
            .ToArray();
    }

    public static void Append(string repoRoot, AdaptiveRuntimeExecutionReport report)
    {
        var path = GetPath(repoRoot);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var line = JsonSerializer.Serialize(report);
        File.AppendAllText(path, line + Environment.NewLine);
    }
}
