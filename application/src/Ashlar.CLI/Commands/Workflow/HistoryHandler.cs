using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Workflow;
/// <summary>Handles history requests.</summary>
internal sealed class HistoryHandler
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public Task<int> ExecuteAsync(string repoRoot, int limit, string? benchmarkSet, bool json)
    {
        if (limit <= 0)
        {
            WriteResult(new WorkflowHistoryResult(false, WorkflowCommandUtilities.InvalidLimitMessage), json);
            return Task.FromResult(1);
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteResult(new WorkflowHistoryResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        var rows = WorkflowLabHistoryStore.ReadRecent(fullRepoRoot, limit);
        if (!string.IsNullOrWhiteSpace(benchmarkSet))
        {
            var normalized = benchmarkSet.Trim().ToLowerInvariant();
            rows = rows
                .Where(x => string.Equals(x.BenchmarkSet, normalized, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        var total = rows.Count;
        var successCount = rows.Count(x => x.Success);
        var best = rows
            .OrderByDescending(x => x.Success)
            .ThenByDescending(x => x.Score)
            .ThenBy(x => x.ElapsedMs)
            .FirstOrDefault();

        WriteResult(new WorkflowHistoryResult(
            true,
            $"Loaded {total} workflow stress history entries.",
            rows,
            new WorkflowHistorySummary(total, successCount, total - successCount, best?.ScenarioId, best?.Score)), json);
        return Task.FromResult(0);
    }

    private static void WriteResult(WorkflowHistoryResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                summaryStats = result.SummaryStats,
                items = result.Items
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow history: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (result.SummaryStats != null)
        {
            Console.WriteLine(
                $"  total={result.SummaryStats.Total}, success={result.SummaryStats.Success}, failed={result.SummaryStats.Failed}, best={result.SummaryStats.BestScenarioId ?? "n/a"}, best-score={result.SummaryStats.BestScore?.ToString("F2") ?? "n/a"}");
        }
    }

    private sealed record WorkflowHistorySummary(
        int Total,
        int Success,
        int Failed,
        string? BestScenarioId,
        double? BestScore);

    private sealed record WorkflowHistoryResult(
        bool Ok,
        string Summary,
        IReadOnlyList<WorkflowLabStressHistoryRow>? Items = null,
        WorkflowHistorySummary? SummaryStats = null);
}
