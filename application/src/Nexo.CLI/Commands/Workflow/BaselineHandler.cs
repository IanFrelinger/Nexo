using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands;

internal sealed class BaselineHandler(
    Func<string?, string, string> normalizeBenchmarkSet,
    Func<string?, GatePolicyLoadResult> loadGatePolicy,
    Func<string, string, string> buildBaselineId)
{
    public Task<int> ExecutePromoteAsync(
        string repoRoot,
        string? benchmarkSet,
        string runId,
        string? notes,
        string? policyFile,
        bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WritePromoteResult(new WorkflowBaselinePromoteResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            WritePromoteResult(new WorkflowBaselinePromoteResult(false, "Run-id is required for baseline promotion."), json);
            return Task.FromResult(1);
        }

        var history = WorkflowLabHistoryStore.ReadAll(fullRepoRoot);
        var candidateRows = history
            .Where(x => string.Equals(x.RunId, runId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (candidateRows.Length == 0)
        {
            WritePromoteResult(new WorkflowBaselinePromoteResult(false, $"No history found for run-id '{runId.Trim()}'."), json);
            return Task.FromResult(1);
        }

        var policyResult = loadGatePolicy(policyFile);
        if (!policyResult.Ok)
        {
            WritePromoteResult(new WorkflowBaselinePromoteResult(false, policyResult.Error ?? "Unable to parse policy file."), json);
            return Task.FromResult(1);
        }

        var latest = candidateRows.OrderByDescending(x => x.StartedAtUtc).First();
        var normalizedBenchmarkSet = normalizeBenchmarkSet(
            benchmarkSet,
            string.IsNullOrWhiteSpace(latest.BenchmarkSet) ? "workflow-lab" : latest.BenchmarkSet);
        var promoted = WorkflowBaselineStore.Promote(fullRepoRoot, new WorkflowBaselineRecord
        {
            BaselineId = buildBaselineId(normalizedBenchmarkSet, latest.RunId),
            BenchmarkSet = normalizedBenchmarkSet,
            RunId = latest.RunId,
            GitSha = latest.GitSha,
            SpecHash = latest.SpecHash,
            ProviderSnapshot = latest.ProviderSnapshot,
            PromotedAtUtc = DateTimeOffset.UtcNow,
            Active = true,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Policy = policyResult.Policy is null
                ? null
                : new WorkflowGatePolicySpec
                {
                    Name = policyResult.Policy.Name,
                    BenchmarkSet = policyResult.Policy.BenchmarkSet,
                    MinSuccessRateDelta = policyResult.Policy.MinSuccessRateDelta,
                    MaxP95LatencyRegressionMs = policyResult.Policy.MaxP95LatencyRegressionMs,
                    MaxAverageLatencyRegressionMs = policyResult.Policy.MaxAverageLatencyRegressionMs,
                    MinAverageScoreDelta = policyResult.Policy.MinAverageScoreDelta,
                    MaxRegressedScenarios = policyResult.Policy.MaxRegressedScenarios
                }
        });

        WritePromoteResult(
            new WorkflowBaselinePromoteResult(
                true,
                $"Promoted run-id {promoted.RunId} as active baseline for benchmark-set {promoted.BenchmarkSet}.",
                promoted),
            json);
        return Task.FromResult(0);
    }

    public Task<int> ExecuteListAsync(string repoRoot, string? benchmarkSet, bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteListResult(new WorkflowBaselineListResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        var all = WorkflowBaselineStore.ReadAll(fullRepoRoot);
        var filtered = string.IsNullOrWhiteSpace(benchmarkSet)
            ? all
            : all.Where(x => string.Equals(x.BenchmarkSet, benchmarkSet.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
        WriteListResult(
            new WorkflowBaselineListResult(
                true,
                $"Loaded {filtered.Count} baseline record(s).",
                filtered),
            json);
        return Task.FromResult(0);
    }

    public Task<int> ExecuteShowAsync(string repoRoot, string? benchmarkSet, string? baselineId, bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteShowResult(new WorkflowBaselineShowResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        WorkflowBaselineRecord? selected = !string.IsNullOrWhiteSpace(baselineId)
            ? WorkflowBaselineStore.ReadById(fullRepoRoot, baselineId)
            : WorkflowBaselineStore.ReadActive(fullRepoRoot, normalizeBenchmarkSet(benchmarkSet, "workflow-lab"));
        if (selected is null)
        {
            var missing = !string.IsNullOrWhiteSpace(baselineId)
                ? $"No baseline found for id '{baselineId.Trim()}'."
                : $"No active baseline found for benchmark-set '{normalizeBenchmarkSet(benchmarkSet, "workflow-lab")}'.";
            WriteShowResult(new WorkflowBaselineShowResult(false, missing), json);
            return Task.FromResult(1);
        }

        WriteShowResult(new WorkflowBaselineShowResult(true, "Baseline loaded.", selected), json);
        return Task.FromResult(0);
    }

    private static void WritePromoteResult(WorkflowBaselinePromoteResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                baseline = result.Baseline
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow baseline promote: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (result.Baseline is not null)
        {
            Console.WriteLine($"  baseline-id={result.Baseline.BaselineId}");
            Console.WriteLine($"  benchmark-set={result.Baseline.BenchmarkSet}");
            Console.WriteLine($"  run-id={result.Baseline.RunId}");
            Console.WriteLine($"  promoted-at={result.Baseline.PromotedAtUtc:O}");
        }
    }

    private static void WriteListResult(WorkflowBaselineListResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                baselines = result.Baselines
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow baseline list: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        foreach (var baseline in result.Baselines ?? Array.Empty<WorkflowBaselineRecord>())
        {
            Console.WriteLine($"  {baseline.BaselineId} | benchmark-set={baseline.BenchmarkSet} | run-id={baseline.RunId} | promoted-at={baseline.PromotedAtUtc:O}");
        }
    }

    private static void WriteShowResult(WorkflowBaselineShowResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                baseline = result.Baseline
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow baseline show: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (result.Baseline is not null)
        {
            Console.WriteLine($"  baseline-id={result.Baseline.BaselineId}");
            Console.WriteLine($"  benchmark-set={result.Baseline.BenchmarkSet}");
            Console.WriteLine($"  run-id={result.Baseline.RunId}");
            Console.WriteLine($"  git-sha={result.Baseline.GitSha}");
            Console.WriteLine($"  spec-hash={result.Baseline.SpecHash}");
            Console.WriteLine($"  provider-snapshot={result.Baseline.ProviderSnapshot}");
            Console.WriteLine($"  promoted-at={result.Baseline.PromotedAtUtc:O}");
            Console.WriteLine($"  active={result.Baseline.Active}");
            if (!string.IsNullOrWhiteSpace(result.Baseline.Notes))
                Console.WriteLine($"  notes={result.Baseline.Notes}");
        }
    }

    private sealed record WorkflowBaselinePromoteResult(
        bool Ok,
        string Summary,
        WorkflowBaselineRecord? Baseline = null);

    private sealed record WorkflowBaselineListResult(
        bool Ok,
        string Summary,
        IReadOnlyList<WorkflowBaselineRecord>? Baselines = null);

    private sealed record WorkflowBaselineShowResult(
        bool Ok,
        string Summary,
        WorkflowBaselineRecord? Baseline = null);
}
