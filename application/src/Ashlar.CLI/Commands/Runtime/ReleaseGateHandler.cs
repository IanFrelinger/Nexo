namespace Ashlar.CLI.Commands.Runtime;

/// <summary>Handles release gate requests.</summary>
internal sealed class ReleaseGateHandler(RuntimeEvaluateExecutor executeEvaluate, RuntimeGateExecutor executeGate)
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
    public async Task<int> ExecuteAsync(
        string mode,
        string repoRoot,
        string provider,
        bool allowMock,
        bool runTests,
        string testFilter,
        bool runPreflight,
        double coreMinPassRate,
        int coreMinTotal,
        int coreHistoryWindow,
        double visualMinPassRate,
        int visualMinTotal,
        int visualHistoryWindow,
        int visualPromotionStreak,
        string visualRequiredMode,
        int laneRepetitions,
        bool sloWarningOnly,
        bool emitSloEvidence,
        string? evidenceOutput,
        double ncrResolutionMsSlo,
        double ncrLoadMsSlo,
        double ncrOutcomeMsSlo,
        double ncrFailureRateSlo,
        bool json,
        CancellationToken ct)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            Console.Error.WriteLine($"runtime release-gate: repo root not found: {fullRepoRoot}");
            return 1;
        }

        var normalizedMode = RuntimeGateEvaluation.NormalizeReleaseGateMode(mode);
        if (normalizedMode == "invalid")
        {
            Console.Error.WriteLine($"runtime release-gate: unsupported mode '{mode}'. Use core | visual | chaos | full.");
            return 1;
        }
        var normalizedVisualRequiredMode = RuntimeGateEvaluation.NormalizeVisualRequiredMode(visualRequiredMode);
        if (normalizedVisualRequiredMode == "invalid")
        {
            Console.Error.WriteLine($"runtime release-gate: invalid --visual-required-mode '{visualRequiredMode}', expected auto | true | false.");
            return 1;
        }

        if (!RuntimeCommandUtilities.TryValidatePositiveCount(laneRepetitions))
        {
            Console.Error.WriteLine(RuntimeCommandUtilities.InvalidLaneRepetitionsMessage);
            return 1;
        }

        if (!RuntimeCommandUtilities.TryValidatePositiveCount(coreMinTotal))
        {
            Console.Error.WriteLine(RuntimeCommandUtilities.InvalidCoreMinTotalMessage);
            return 1;
        }

        if (!RuntimeCommandUtilities.TryValidatePositiveCount(visualMinTotal))
        {
            Console.Error.WriteLine(RuntimeCommandUtilities.InvalidVisualMinTotalMessage);
            return 1;
        }

        if (!RuntimeCommandUtilities.TryValidatePositiveCount(coreHistoryWindow))
        {
            Console.Error.WriteLine(RuntimeCommandUtilities.InvalidCoreHistoryWindowMessage);
            return 1;
        }

        if (!RuntimeCommandUtilities.TryValidatePositiveCount(visualHistoryWindow))
        {
            Console.Error.WriteLine(RuntimeCommandUtilities.InvalidVisualHistoryWindowMessage);
            return 1;
        }

        if (!RuntimeCommandUtilities.TryValidateUnitInterval(coreMinPassRate))
        {
            Console.Error.WriteLine(RuntimeCommandUtilities.InvalidCoreMinPassRateMessage);
            return 1;
        }

        if (!RuntimeCommandUtilities.TryValidateUnitInterval(visualMinPassRate))
        {
            Console.Error.WriteLine(RuntimeCommandUtilities.InvalidVisualMinPassRateMessage);
            return 1;
        }

        if (!RuntimeCommandUtilities.TryValidateNonNegativeCount(visualPromotionStreak))
        {
            Console.Error.WriteLine(RuntimeCommandUtilities.InvalidVisualPromotionStreakMessage);
            return 1;
        }
        var finalExitCode = 0;
        RuntimeGateResult? coreGateResult = null;
        RuntimeGateResult? visualGateResult = null;
        RuntimeGateResult? chaosGateResult = null;
        var visualLaneRequired = false;
        var visualBenchmarkSet = "release-visual-degraded";

        if (finalExitCode == 0 && normalizedMode is "core" or "full")
        {
            for (var rep = 1; rep <= laneRepetitions; rep++)
            {
                Console.WriteLine($"=== Runtime Release Gate: release-core matrix [{rep}/{laneRepetitions}] ===");
                var coreEvalExit = await executeEvaluate(
                    goalsJson: null,
                    goalsFile: Path.Combine(fullRepoRoot, "docs", "runtime", "benchmarks", "release_core_goals.txt"),
                    policiesCsv: "release",
                    repoRoot: fullRepoRoot,
                    provider: provider,
                    allowMock: allowMock,
                    runTests: runTests,
                    testFilter: testFilter,
                    bootstrapProfile: "auto",
                    runtimeManifestPath: null,
                    runtimeManifestJson: null,
                    maxIterationsOverride: null,
                    bootstrapApply: false,
                    runPreflight: runPreflight,
                    useHistory: true,
                    historyWindow: 200,
                    persistHistory: true,
                    benchmarkSet: "release-core",
                    allowVisualCapabilityDegrade: false,
                    json: json,
                    ct: ct).ConfigureAwait(false);
                if (coreEvalExit != 0)
                {
                    finalExitCode = coreEvalExit;
                    break;
                }
            }

            if (finalExitCode == 0)
            {
                Console.WriteLine("=== Runtime Release Gate: release-core SLO gate ===");
                var coreGateExit = await executeGate(
                    repoRoot: fullRepoRoot,
                    historyWindow: coreHistoryWindow,
                    minPassRate: coreMinPassRate,
                    minTotal: coreMinTotal,
                    goal: null,
                    policy: "release",
                    benchmarkSet: "release-core",
                    stage: null,
                    minConsecutivePasses: 2,
                    json: json).ConfigureAwait(false);
                coreGateResult = RuntimeGateEvaluation.EvaluateGateResult(
                    fullRepoRoot,
                    coreHistoryWindow,
                    coreMinPassRate,
                    coreMinTotal,
                    goal: null,
                    policy: "release",
                    benchmarkSet: "release-core",
                    stage: null,
                    minConsecutivePasses: 2);
                if (coreGateExit != 0)
                    finalExitCode = coreGateExit;
            }
        }

        if (finalExitCode == 0 && normalizedMode is "visual" or "full")
        {
            var visualRequired = RuntimeGateEvaluation.ResolveVisualRequired(
                normalizedVisualRequiredMode,
                fullRepoRoot,
                visualHistoryWindow,
                visualPromotionStreak);
            visualLaneRequired = visualRequired;
            var allowVisualCapabilityDegrade = !visualRequired;
            visualBenchmarkSet = visualRequired ? "release-visual-strict" : "release-visual-degraded";
            var visualEvalFailed = false;
            for (var rep = 1; rep <= laneRepetitions; rep++)
            {
                Console.WriteLine($"=== Runtime Release Gate: release-visual matrix [{rep}/{laneRepetitions}] ===");
                var visualEvalExit = await executeEvaluate(
                    goalsJson: null,
                    goalsFile: Path.Combine(fullRepoRoot, "docs", "runtime", "benchmarks", "release_visual_goals.txt"),
                    policiesCsv: "release",
                    repoRoot: fullRepoRoot,
                    provider: provider,
                    allowMock: allowMock,
                    runTests: runTests,
                    testFilter: testFilter,
                    bootstrapProfile: "auto",
                    runtimeManifestPath: null,
                    runtimeManifestJson: null,
                    maxIterationsOverride: null,
                    bootstrapApply: false,
                    runPreflight: runPreflight,
                    useHistory: true,
                    historyWindow: 200,
                    persistHistory: true,
                    benchmarkSet: visualBenchmarkSet,
                    allowVisualCapabilityDegrade: allowVisualCapabilityDegrade,
                    json: json,
                    ct: ct).ConfigureAwait(false);
                if (visualEvalExit != 0)
                {
                    visualEvalFailed = true;
                    if (visualRequired)
                    {
                        Console.Error.WriteLine("release gate: release-visual lane is required after green streak; matrix failed.");
                        finalExitCode = visualEvalExit;
                        break;
                    }
                }
            }

            if (finalExitCode == 0)
            {
                Console.WriteLine("=== Runtime Release Gate: release-visual SLO gate ===");
                var visualGateExit = await executeGate(
                    repoRoot: fullRepoRoot,
                    historyWindow: visualHistoryWindow,
                    minPassRate: visualMinPassRate,
                    minTotal: visualMinTotal,
                    goal: null,
                    policy: "release",
                    benchmarkSet: visualBenchmarkSet,
                    stage: null,
                    minConsecutivePasses: visualPromotionStreak,
                    json: json).ConfigureAwait(false);
                visualGateResult = RuntimeGateEvaluation.EvaluateGateResult(
                    fullRepoRoot,
                    visualHistoryWindow,
                    visualMinPassRate,
                    visualMinTotal,
                    goal: null,
                    policy: "release",
                    benchmarkSet: visualBenchmarkSet,
                    stage: null,
                    minConsecutivePasses: visualPromotionStreak);
                if (visualGateExit != 0)
                {
                    if (visualRequired)
                    {
                        Console.Error.WriteLine("release gate: release-visual lane is required after green streak; gate failed.");
                        finalExitCode = visualGateExit;
                    }

                    Console.Error.WriteLine($"release gate: release-visual lane remains advisory until streak {visualPromotionStreak} is established.");
                }
                else if (visualEvalFailed && !visualRequired)
                {
                    Console.Error.WriteLine($"release gate: release-visual matrix had failures but advisory lane still passed aggregate gate (streak target {visualPromotionStreak}).");
                }
            }
        }

        if (finalExitCode == 0 && normalizedMode is "chaos" or "full")
        {
            Console.WriteLine("=== Runtime Release Gate: chaos matrix (non-gating) ===");
            var chaosExit = await executeEvaluate(
                goalsJson: null,
                goalsFile: Path.Combine(fullRepoRoot, "docs", "runtime", "benchmarks", "chaos_goals.txt"),
                policiesCsv: "prod",
                repoRoot: fullRepoRoot,
                provider: provider,
                allowMock: allowMock,
                runTests: runTests,
                testFilter: testFilter,
                bootstrapProfile: "auto",
                runtimeManifestPath: null,
                runtimeManifestJson: null,
                maxIterationsOverride: null,
                bootstrapApply: false,
                runPreflight: runPreflight,
                useHistory: true,
                historyWindow: 200,
                persistHistory: false,
                benchmarkSet: "chaos",
                allowVisualCapabilityDegrade: false,
                json: json,
                ct: ct).ConfigureAwait(false);
            if (chaosExit != 0)
                Console.Error.WriteLine("release gate: chaos matrix reported failures (expected in stress mode).");
            chaosGateResult = RuntimeGateEvaluation.EvaluateGateResult(
                fullRepoRoot,
                historyWindow: 200,
                minPassRate: 0d,
                minTotal: 1,
                goal: null,
                policy: "prod",
                benchmarkSet: "chaos",
                stage: null,
                minConsecutivePasses: 0);
        }

        var sloEvidence = RuntimeGateEvaluation.BuildRuntimeSloEvidence(
            fullRepoRoot,
            normalizedMode,
            coreGateResult,
            visualGateResult,
            chaosGateResult,
            visualLaneRequired,
            visualBenchmarkSet,
            ncrResolutionMsSlo,
            ncrLoadMsSlo,
            ncrOutcomeMsSlo,
            ncrFailureRateSlo);
        if (emitSloEvidence)
        {
            RuntimeOutputWriter.WriteRuntimeSloEvidence(fullRepoRoot, evidenceOutput, sloEvidence);
        }

        if (finalExitCode == 0 && !sloWarningOnly && !sloEvidence.Passed)
        {
            if (allowMock)
            {
                Console.WriteLine("release gate: NCR SLO not enforced under --allow-mock (synthetic timings).");
            }
            else
            {
                Console.Error.WriteLine("release gate: NCR SLO evidence failed required thresholds.");
                finalExitCode = 1;
            }
        }
        else if (finalExitCode == 0 && sloWarningOnly && !sloEvidence.Passed)
        {
            if (allowMock)
            {
                Console.WriteLine("release gate: NCR SLO advisory under --allow-mock (synthetic timings; not gating).");
            }
            else
            {
                Console.Error.WriteLine("release gate: NCR SLO evidence failed thresholds (warning-only mode).");
            }
        }

        if (finalExitCode == 0)
            Console.WriteLine("=== Runtime Release Gate: PASSED ===");
        else
            Console.Error.WriteLine("=== Runtime Release Gate: FAILED ===");
        return finalExitCode;
    }

}
