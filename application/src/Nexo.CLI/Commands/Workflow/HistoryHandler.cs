using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands;

internal sealed class HistoryHandler
{
    public Task<int> ExecuteAsync(string repoRoot, int limit, string? benchmarkSet, bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteResult(new WorkflowHistoryResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        var rows = WorkflowLabHistoryStore.ReadRecent(fullRepoRoot, Math.Max(1, limit));
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
