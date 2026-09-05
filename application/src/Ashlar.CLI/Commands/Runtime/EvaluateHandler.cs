using System.Text.Json;
using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;

/// <summary>Handles evaluate requests.</summary>
internal sealed class EvaluateHandler(RuntimeExecuteCore executeCore)
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public async Task<int> ExecuteAsync(
        string? goalsJson,
        string? goalsFile,
        string policiesCsv,
        string repoRoot,
        string? provider,
        bool allowMock,
        bool runTests,
        string testFilter,
        string bootstrapProfile,
        string? runtimeManifestPath,
        string? runtimeManifestJson,
        int? maxIterationsOverride,
        bool bootstrapApply,
        bool runPreflight,
        bool useHistory,
        int historyWindow,
        bool persistHistory,
        string benchmarkSet,
        bool allowVisualCapabilityDegrade,
        bool json,
        CancellationToken ct)
    {
        var goals = ResolveGoals(goalsJson, goalsFile);
        if (goals.Length == 0)
        {
            RuntimeOutputWriter.WriteEvaluateResult(new RuntimeEvaluateResult(false, "No goals provided for evaluation."), json);
            return 1;
        }

        if (!RuntimeCommandUtilities.TryNormalizeBootstrapProfile(bootstrapProfile, out _))
        {
            RuntimeOutputWriter.WriteEvaluateResult(new RuntimeEvaluateResult(false, "Invalid --bootstrap-profile. Use auto, self-extend-functional, self-extend-aesthetic, or self-extend-visual."), json);
            return 1;
        }

        if (!TryResolvePolicies(policiesCsv, out var policies, out var policyError))
        {
            RuntimeOutputWriter.WriteEvaluateResult(new RuntimeEvaluateResult(false, policyError), json);
            return 1;
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            RuntimeOutputWriter.WriteEvaluateResult(new RuntimeEvaluateResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return 1;
        }

        var scenarioResults = new List<RuntimeEvaluateScenarioResult>();
        foreach (var goal in goals)
        {
            foreach (var policy in policies)
            {
                var run = await executeCore(
                    goal,
                    fullRepoRoot,
                    provider,
                    allowMock,
                    runTests,
                    testFilter,
                    bootstrapProfile,
                    policy,
                    runtimeManifestPath,
                    runtimeManifestJson,
                    maxIterationsOverride,
                    bootstrapApply,
                    bootstrapYes: true,
                    bootstrapDryRun: false,
                    runPreflight,
                    useHistory,
                    historyWindow,
                    persistHistory,
                    benchmarkSet,
                    allowVisualCapabilityDegrade,
                    ct).ConfigureAwait(false);
                scenarioResults.Add(new RuntimeEvaluateScenarioResult(
                    AdaptiveRuntimeExecutionReport.BuildGoalPreview(goal, 80),
                    policy,
                    run.Ok,
                    run.ElapsedMs,
                    run.FailureStage,
                    run.Summary));
            }
        }

        var policySummary = scenarioResults
            .GroupBy(r => r.Policy, StringComparer.OrdinalIgnoreCase)
            .Select(g => new RuntimeEvaluatePolicySummary(
                g.Key,
                g.Count(),
                g.Count(x => x.Ok),
                g.Count(x => !x.Ok),
                (long)Math.Round(
                    g.Where(x => x.ElapsedMs.HasValue)
                     .Select(x => (double)x.ElapsedMs!.Value)
                     .DefaultIfEmpty(0d)
                     .Average())))
            .OrderBy(s => s.Policy, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var allPassed = scenarioResults.All(r => r.Ok);
        var result = new RuntimeEvaluateResult(
            allPassed,
            allPassed
                ? $"Evaluation complete: {scenarioResults.Count} scenario(s) passed."
                : $"Evaluation complete: {scenarioResults.Count(r => !r.Ok)} failing scenario(s) out of {scenarioResults.Count}.",
            scenarioResults.ToArray(),
            policySummary);
        RuntimeOutputWriter.WriteEvaluateResult(result, json);
        return allPassed ? 0 : 1;
    }

    private static string[] ResolveGoals(string? goalsJson, string? goalsFile)
    {
        if (!string.IsNullOrWhiteSpace(goalsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<string[]>(goalsJson);
                return NormalizeGoals(parsed);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        if (!string.IsNullOrWhiteSpace(goalsFile) && File.Exists(goalsFile))
        {
            var lines = File.ReadAllLines(goalsFile);
            return NormalizeGoals(lines);
        }

        return Array.Empty<string>();
    }

    private static string[] NormalizeGoals(IEnumerable<string>? goals)
    {
        return (goals ?? Array.Empty<string>())
            .Select(g => (g ?? string.Empty).Trim())
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryResolvePolicies(string csv, out string[] policies, out string error)
    {
        policies = Array.Empty<string>();
        error = "No valid policies provided for evaluation.";
        var tokens = (csv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return false;

        var resolved = new List<string>();
        foreach (var token in tokens)
        {
            if (!RuntimeCommandUtilities.TryNormalizeQaPolicy(token, out var policy))
            {
                error = "Invalid --policies. Use auto, demo, release, prod, or research.";
                return false;
            }

            if (!resolved.Contains(policy, StringComparer.OrdinalIgnoreCase))
                resolved.Add(policy);
        }

        if (resolved.Count == 0)
            return false;

        policies = resolved.ToArray();
        error = string.Empty;
        return true;
    }

}
