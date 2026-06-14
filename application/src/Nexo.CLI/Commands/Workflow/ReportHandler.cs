using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands;

internal sealed class ReportHandler(
    Func<IReadOnlyList<WorkflowLabStressHistoryRow>, WorkflowBenchmarkReport> buildBenchmarkReport,
    Func<IReadOnlyList<WorkflowLabStressHistoryRow>, string?, string?, WorkflowRunComparison> buildComparison,
    Func<WorkflowReportResult, bool, string, string> renderReportContent,
    Func<WorkflowRunComparison, string, string> renderComparisonText)
{
    public Task<int> ExecuteAsync(
        string repoRoot,
        int limit,
        string? benchmarkSet,
        string? runId,
        string? baselineRunId,
        string? since,
        string? outputPath,
        bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteResult(new WorkflowReportResult(
                false,
                $"Repo root not found: {fullRepoRoot}",
                new WorkflowBenchmarkReport(
                    DateTimeOffset.UtcNow,
                    0,
                    0,
                    0,
                    0d,
                    0,
                    0,
                    0d,
                    0d,
                    0d,
                    0,
                    0,
                    0,
                    0,
                    0,
                    "unknown",
                    Array.Empty<WorkflowScenarioBenchmark>(),
                    Array.Empty<WorkflowScenarioBenchmark>(),
                    Array.Empty<WorkflowFailureCategoryStat>(),
                    Array.Empty<string>(),
                    null,
                    null,
                    null,
                    null,
                    Array.Empty<WorkflowRecommendation>())), json);
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
        if (!string.IsNullOrWhiteSpace(since) && DateTimeOffset.TryParse(since, out var sinceUtc))
        {
            rows = rows.Where(x => x.StartedAtUtc >= sinceUtc).ToArray();
        }

        var comparison = buildComparison(rows, runId, baselineRunId);
        if (comparison is { Valid: false })
        {
            WriteResult(new WorkflowReportResult(
                false,
                comparison.Summary ?? "Unable to build workflow run comparison.",
                buildBenchmarkReport(Array.Empty<WorkflowLabStressHistoryRow>()),
                null,
                comparison), json);
            return Task.FromResult(1);
        }

        var filteredRows = rows;
        if (!string.IsNullOrWhiteSpace(runId))
        {
            var normalizedRunId = runId.Trim();
            filteredRows = filteredRows.Where(x => string.Equals(x.RunId, normalizedRunId, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        var report = buildBenchmarkReport(filteredRows);
        var result = new WorkflowReportResult(
            report.TotalRuns > 0,
            report.TotalRuns > 0
                ? $"Benchmark report generated from {report.TotalRuns} run(s)."
                : "No workflow stress history found for the selected filters.",
            report,
            Comparison: comparison);

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var fullOutputPath = Path.GetFullPath(outputPath);
            var directory = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var content = renderReportContent(result, json, fullOutputPath);
            File.WriteAllText(fullOutputPath, content);
            result = result with { OutputPath = fullOutputPath };
        }

        WriteResult(result, json);
        return Task.FromResult(result.Ok ? 0 : 1);
    }

    private void WriteResult(WorkflowReportResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                outputPath = result.OutputPath,
                report = result.Report,
                comparison = result.Comparison
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow report: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.OutputPath))
            Console.WriteLine($"  output={result.OutputPath}");
        Console.WriteLine($"  success-rate={result.Report.SuccessRate:P1}, avg-latency={result.Report.AverageElapsedMs}ms, p95={result.Report.P95ElapsedMs}ms");
        if (result.Report.TopScenarios.Count > 0)
            Console.WriteLine($"  best={result.Report.TopScenarios[0].ScenarioGroupId} score={result.Report.TopScenarios[0].AverageScore:F2}");
        if (result.Report.Recommendations.Count > 0)
        {
            Console.WriteLine("  recommendations:");
            foreach (var rec in result.Report.Recommendations.Take(5))
                Console.WriteLine($"    - [{rec.Action}] {rec.Kind}: {rec.Rationale}");
        }
        if (result.Comparison is { Valid: true, RunId: not null, BaselineRunId: not null })
        {
            Console.WriteLine(renderComparisonText(result.Comparison, "  "));
        }
    }
}
