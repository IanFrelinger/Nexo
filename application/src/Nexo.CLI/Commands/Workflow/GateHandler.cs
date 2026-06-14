using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands;

internal sealed class GateHandler(
    Func<string?, string, string> normalizeBenchmarkSet,
    Func<string?, GatePolicyLoadResult> loadGatePolicy,
    Func<IReadOnlyList<WorkflowLabStressHistoryRow>, string?, string?, WorkflowRunComparison> buildComparison,
    Func<WorkflowRunComparison, string, string> renderComparisonText)
{
    public Task<int> ExecuteAsync(
        string repoRoot,
        string? benchmarkSet,
        string runId,
        string? baselineRunId,
        string? policyFile,
        double minSuccessRateDelta,
        long maxP95LatencyRegressionMs,
        long maxAverageLatencyRegressionMs,
        double minAverageScoreDelta,
        int maxRegressedScenarios,
        bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteResult(new WorkflowGateResult(false, false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            WriteResult(new WorkflowGateResult(false, false, "--run-id is required."), json);
            return Task.FromResult(1);
        }

        var policyResult = loadGatePolicy(policyFile);
        if (!policyResult.Ok)
        {
            WriteResult(new WorkflowGateResult(false, false, policyResult.Error ?? "Unable to parse policy file."), json);
            return Task.FromResult(1);
        }

        var normalizedBenchmarkSet = normalizeBenchmarkSet(
            benchmarkSet,
            string.IsNullOrWhiteSpace(policyResult.Policy?.BenchmarkSet) ? "workflow-lab" : policyResult.Policy!.BenchmarkSet!);
        var resolvedBaselineRunId = string.IsNullOrWhiteSpace(baselineRunId)
            ? WorkflowBaselineStore.ReadActive(fullRepoRoot, normalizedBenchmarkSet)?.RunId
            : baselineRunId.Trim();
        if (string.IsNullOrWhiteSpace(resolvedBaselineRunId))
        {
            WriteResult(new WorkflowGateResult(
                false,
                false,
                $"No baseline run-id provided and no active baseline found for benchmark-set '{normalizedBenchmarkSet}'."), json);
            return Task.FromResult(1);
        }

        var rows = WorkflowLabHistoryStore.ReadAll(fullRepoRoot);
        if (!string.IsNullOrWhiteSpace(normalizedBenchmarkSet))
        {
            rows = rows.Where(x => string.Equals(x.BenchmarkSet, normalizedBenchmarkSet, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        var comparison = buildComparison(rows, runId, resolvedBaselineRunId);
        if (!comparison.Valid)
        {
            WriteResult(new WorkflowGateResult(false, false, comparison.Summary ?? "Failed to build comparison.", Comparison: comparison), json);
            return Task.FromResult(1);
        }

        var effectiveMinSuccessRateDelta = policyResult.Policy?.MinSuccessRateDelta ?? minSuccessRateDelta;
        var effectiveMaxP95LatencyRegressionMs = policyResult.Policy?.MaxP95LatencyRegressionMs ?? maxP95LatencyRegressionMs;
        var effectiveMaxAverageLatencyRegressionMs = policyResult.Policy?.MaxAverageLatencyRegressionMs ?? maxAverageLatencyRegressionMs;
        var effectiveMinAverageScoreDelta = policyResult.Policy?.MinAverageScoreDelta ?? minAverageScoreDelta;
        var effectiveMaxRegressedScenarios = policyResult.Policy?.MaxRegressedScenarios ?? maxRegressedScenarios;

        var failures = new List<string>();
        if (comparison.SuccessRateDelta < effectiveMinSuccessRateDelta)
            failures.Add($"successRateDelta {comparison.SuccessRateDelta:F4} < {effectiveMinSuccessRateDelta:F4}");
        if (comparison.P95LatencyDeltaMs > effectiveMaxP95LatencyRegressionMs)
            failures.Add($"p95LatencyDeltaMs {comparison.P95LatencyDeltaMs} > {effectiveMaxP95LatencyRegressionMs}");
        if (comparison.AverageLatencyDeltaMs > effectiveMaxAverageLatencyRegressionMs)
            failures.Add($"averageLatencyDeltaMs {comparison.AverageLatencyDeltaMs} > {effectiveMaxAverageLatencyRegressionMs}");
        if (comparison.AverageScoreDelta < effectiveMinAverageScoreDelta)
            failures.Add($"averageScoreDelta {comparison.AverageScoreDelta:F3} < {effectiveMinAverageScoreDelta:F3}");
        if (comparison.RegressedScenarios > effectiveMaxRegressedScenarios)
            failures.Add($"regressedScenarios {comparison.RegressedScenarios} > {effectiveMaxRegressedScenarios}");

        var passed = failures.Count == 0;
        var summary = passed
            ? $"Workflow gate passed for run {comparison.RunId} vs baseline {comparison.BaselineRunId}."
            : $"Workflow gate failed for run {comparison.RunId} vs baseline {comparison.BaselineRunId}: {string.Join("; ", failures)}";
        var result = new WorkflowGateResult(true, passed, summary, failures.ToArray(), comparison);
        WriteResult(result, json);
        return Task.FromResult(passed ? 0 : 1);
    }

    private void WriteResult(WorkflowGateResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                passed = result.Passed,
                summary = result.Summary,
                failures = result.Failures,
                comparison = result.Comparison
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow gate: {(result.Ok ? (result.Passed ? "passed" : "failed") : "error")}");
        Console.WriteLine(result.Summary);
        if (result.Failures is { Count: > 0 })
        {
            Console.WriteLine("  thresholds:");
            foreach (var failure in result.Failures)
                Console.WriteLine($"    - {failure}");
        }
        if (result.Comparison is { Valid: true })
        {
            Console.WriteLine(renderComparisonText(result.Comparison, "  "));
        }
    }
}
