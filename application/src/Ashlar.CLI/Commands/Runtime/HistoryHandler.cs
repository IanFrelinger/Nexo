using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;
/// <summary>Handles runtime history requests.</summary>
internal sealed class RuntimeHistoryHandler
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public Task<int> ExecuteAsync(
        string repoRoot,
        int limit,
        string? goal,
        string? policy,
        string? benchmarkSet,
        bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            RuntimeOutputWriter.WriteHistoryResult(new RuntimeHistoryResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        var items = AdaptiveRuntimeExecutionHistoryStore.ReadRecent(fullRepoRoot, Math.Max(1, limit));
        if (!string.IsNullOrWhiteSpace(goal))
        {
            var fp = AdaptiveRuntimeExecutionReport.ComputeGoalFingerprint(goal);
            items = items.Where(i => string.Equals(i.GoalFingerprint, fp, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (!string.IsNullOrWhiteSpace(policy))
        {
            var p = RuntimeCommandUtilities.NormalizeQaPolicy(policy);
            items = items.Where(i => string.Equals(i.ResolvedQaPolicy, p, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (!string.IsNullOrWhiteSpace(benchmarkSet))
        {
            var b = benchmarkSet.Trim().ToLowerInvariant();
            items = items.Where(i => string.Equals(i.BenchmarkSet, b, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        var total = items.Count;
        var passed = items.Count(i => i.Success);
        var failed = total - passed;
        var avgElapsed = (long)Math.Round(items.Select(i => (double)i.ElapsedMs).DefaultIfEmpty(0d).Average());
        RuntimeOutputWriter.WriteHistoryResult(new RuntimeHistoryResult(
            true,
            $"Loaded {total} history entries.",
            items,
            new RuntimeHistorySummary(total, passed, failed, avgElapsed)), json);
        return Task.FromResult(0);
    }
}
