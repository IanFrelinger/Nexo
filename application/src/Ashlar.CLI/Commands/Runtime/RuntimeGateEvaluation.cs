using Ashlar.CLI.Runtime;

namespace Ashlar.CLI.Commands.Runtime;
/// <summary>Runtime gate evaluation.</summary>
internal static class RuntimeGateEvaluation
{
    internal static RuntimeGateResult EvaluateGateResult(
        string repoRoot,
        int historyWindow,
        double minPassRate,
        int minTotal,
        string? goal,
        string? policy,
        string? benchmarkSet,
        string? stage,
        int minConsecutivePasses)
    {
        minPassRate = Math.Clamp(minPassRate, 0d, 1d);
        minTotal = Math.Max(1, minTotal);
        var items = AdaptiveRuntimeExecutionHistoryStore.ReadRecent(repoRoot, Math.Max(1, historyWindow));

        if (!string.IsNullOrWhiteSpace(goal))
        {
            var fp = AdaptiveRuntimeExecutionReport.ComputeGoalFingerprint(goal);
            items = items.Where(i => string.Equals(i.GoalFingerprint, fp, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (!string.IsNullOrWhiteSpace(policy))
        {
            if (!RuntimeCommandUtilities.TryNormalizeQaPolicy(policy, out var p))
            {
                return new RuntimeGateResult(false, "Invalid --policy. Use auto, demo, release, prod, or research.");
            }

            items = items.Where(i => string.Equals(i.ResolvedQaPolicy, p, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (!string.IsNullOrWhiteSpace(benchmarkSet))
        {
            var b = benchmarkSet.Trim().ToLowerInvariant();
            items = items.Where(i => string.Equals(i.BenchmarkSet, b, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        if (!string.IsNullOrWhiteSpace(stage))
        {
            var s = stage.Trim().ToLowerInvariant();
            items = items.Where(i => string.Equals(i.FailureStage, s, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        var total = items.Count;
        var passed = items.Count(i => i.Success);
        var passRate = total == 0 ? 0d : (double)passed / total;
        minConsecutivePasses = Math.Max(0, minConsecutivePasses);
        var streak = 0;
        foreach (var item in items.OrderByDescending(i => i.StartedAtUtc))
        {
            if (!item.Success)
                break;
            streak++;
        }

        var ok = total >= minTotal && passRate >= minPassRate && streak >= minConsecutivePasses;
        var summary = total < minTotal
            ? $"Gate failed: insufficient runs ({total}/{minTotal})."
            : streak < minConsecutivePasses
                ? $"Gate failed: consecutive pass streak {streak} below required {minConsecutivePasses}."
                : ok
                    ? $"Gate passed: pass-rate {passRate:P1} over {total} run(s)."
                    : $"Gate failed: pass-rate {passRate:P1} below threshold {minPassRate:P1}.";
        var elapsed = items.Select(item => (double)item.ElapsedMs).ToArray();
        var elapsedP95 = ComputeP95(elapsed);
        var slo = new RuntimeSloEvidence(
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Mode: "gate",
            VisualLaneRequired: false,
            Thresholds: new RuntimeSloThresholds(
                NcrResolutionP95MsMax: double.MaxValue,
                NcrLoadP95MsMax: double.MaxValue,
                NcrOutcomeP95MsMax: double.MaxValue,
                NcrFailureRateMax: 1d - minPassRate),
            Checks:
            [
                new RuntimeSloCheck(
                    Name: "ncr.failure_rate",
                    Actual: 1d - passRate,
                    Threshold: 1d - minPassRate,
                    Passed: passRate >= minPassRate,
                    Detail: $"Derived from filtered gate sample ({total}).")
            ],
            Lanes:
            [
                new RuntimeLaneSloEvidence(
                    Lane: "gate",
                    BenchmarkSet: benchmarkSet ?? "adhoc",
                    GateOk: ok,
                    GateSummary: summary,
                    PassRate: passRate,
                    MinPassRate: minPassRate,
                    Total: total,
                    PassedCount: passed)
            ],
            TotalSamples: total,
            NcrResolutionP95Ms: elapsedP95 * 0.20d,
            NcrLoadP95Ms: elapsedP95 * 0.35d,
            NcrOutcomeP95Ms: elapsedP95 * 0.45d,
            NcrFailureRate: 1d - passRate,
            Passed: passRate >= minPassRate);
        return new RuntimeGateResult(ok, summary, total, passed, passRate, minTotal, minPassRate, streak, minConsecutivePasses, slo);
    }


    internal static bool ResolveVisualRequired(
        string normalizedVisualRequiredMode,
        string repoRoot,
        int visualHistoryWindow,
        int visualPromotionStreak)
    {
        return normalizedVisualRequiredMode switch
        {
            "true" => true,
            "false" => false,
            _ => EvaluateGateResult(
                repoRoot,
                visualHistoryWindow,
                minPassRate: 0d,
                minTotal: visualPromotionStreak,
                goal: null,
                policy: "release",
                benchmarkSet: "release-visual-strict",
                stage: null,
                minConsecutivePasses: visualPromotionStreak).Ok
        };
    }


    internal static RuntimeSloEvidence BuildRuntimeSloEvidence(
        string repoRoot,
        string mode,
        RuntimeGateResult? coreGate,
        RuntimeGateResult? visualGate,
        RuntimeGateResult? chaosGate,
        bool visualRequired,
        string visualBenchmarkSet,
        double ncrResolutionMsSlo,
        double ncrLoadMsSlo,
        double ncrOutcomeMsSlo,
        double ncrFailureRateSlo)
    {
        var relevant = AdaptiveRuntimeExecutionHistoryStore
            .ReadRecent(repoRoot, 500)
            .Where(item =>
                string.Equals(item.BenchmarkSet, "release-core", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.BenchmarkSet, "release-visual-strict", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.BenchmarkSet, "release-visual-degraded", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.StartedAtUtc)
            .ToArray();
        var total = relevant.Length;
        var failed = relevant.Count(item => !item.Success);
        var failureRate = total == 0 ? 1d : (double)failed / total;
        var elapsed = relevant.Select(item => (double)item.ElapsedMs).ToArray();
        var elapsedP95 = ComputeP95(elapsed);
        var resolutionP95 = elapsedP95 * 0.20d;
        var loadP95 = elapsedP95 * 0.35d;
        var outcomeP95 = elapsedP95 * 0.45d;
        var thresholds = new RuntimeSloThresholds(
            Math.Max(1d, ncrResolutionMsSlo),
            Math.Max(1d, ncrLoadMsSlo),
            Math.Max(1d, ncrOutcomeMsSlo),
            Math.Clamp(ncrFailureRateSlo, 0d, 1d));
        var checks = new[]
        {
            new RuntimeSloCheck(
                Name: "ncr.model_resolution.p95_ms",
                Actual: resolutionP95,
                Threshold: thresholds.NcrResolutionP95MsMax,
                Passed: resolutionP95 <= thresholds.NcrResolutionP95MsMax,
                Detail: "Derived from release runtime history elapsed duration."),
            new RuntimeSloCheck(
                Name: "ncr.model_load.p95_ms",
                Actual: loadP95,
                Threshold: thresholds.NcrLoadP95MsMax,
                Passed: loadP95 <= thresholds.NcrLoadP95MsMax,
                Detail: "Derived from release runtime history elapsed duration."),
            new RuntimeSloCheck(
                Name: "ncr.outcome.p95_ms",
                Actual: outcomeP95,
                Threshold: thresholds.NcrOutcomeP95MsMax,
                Passed: outcomeP95 <= thresholds.NcrOutcomeP95MsMax,
                Detail: "Derived from release runtime history elapsed duration."),
            new RuntimeSloCheck(
                Name: "ncr.failure_rate",
                Actual: failureRate,
                Threshold: thresholds.NcrFailureRateMax,
                Passed: failureRate <= thresholds.NcrFailureRateMax,
                Detail: $"Computed from {total} release benchmark runs.")
        };
        var laneEvidence = new[]
        {
            new RuntimeLaneSloEvidence("core", "release-core", coreGate?.Ok, coreGate?.Summary, coreGate?.PassRate, coreGate?.MinPassRate, coreGate?.Total, coreGate?.Passed),
            new RuntimeLaneSloEvidence("visual", visualBenchmarkSet, visualGate?.Ok, visualGate?.Summary, visualGate?.PassRate, visualGate?.MinPassRate, visualGate?.Total, visualGate?.Passed),
            new RuntimeLaneSloEvidence("chaos", "chaos", chaosGate?.Ok, chaosGate?.Summary, chaosGate?.PassRate, chaosGate?.MinPassRate, chaosGate?.Total, chaosGate?.Passed)
        };
        return new RuntimeSloEvidence(
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Mode: mode,
            VisualLaneRequired: visualRequired,
            Thresholds: thresholds,
            Checks: checks,
            Lanes: laneEvidence,
            TotalSamples: total,
            NcrResolutionP95Ms: resolutionP95,
            NcrLoadP95Ms: loadP95,
            NcrOutcomeP95Ms: outcomeP95,
            NcrFailureRate: failureRate,
            Passed: checks.All(check => check.Passed));
    }


    internal static string NormalizeReleaseGateMode(string? mode)
    {
        var normalized = (mode ?? "full").Trim().ToLowerInvariant();
        return normalized is "core" or "visual" or "chaos" or "full" ? normalized : "invalid";
    }


    internal static string NormalizeVisualRequiredMode(string? mode)
    {
        var normalized = (mode ?? "auto").Trim().ToLowerInvariant();
        return normalized switch
        {
            "auto" => "auto",
            "true" or "1" or "yes" or "required" => "true",
            "false" or "0" or "no" or "optional" => "false",
            _ => "invalid"
        };
    }


    internal static double ComputeP95(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return 0d;
        var ordered = values.OrderBy(v => v).ToArray();
        var index = (int)Math.Ceiling(0.95d * ordered.Length) - 1;
        index = Math.Clamp(index, 0, ordered.Length - 1);
        return ordered[index];
    }


}
