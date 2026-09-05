using System.CommandLine;
using System.CommandLine.Invocation;

namespace Ashlar.CLI.Commands;

/// <summary>CLI command for runtime.</summary>
public sealed partial class RuntimeCommand
{
    private void ConfigureExecuteCommand()
    {
        var executeCmd = new Command("execute", "Prepare and run adaptive self-extension end-to-end.");
        var goalOpt = new Option<string>("--goal", "Objective to execute adaptively.") { IsRequired = true };
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var providerOpt = new Option<string?>("--provider", () => "mock-json", "Model provider override.");
        var allowMockOpt = new Option<bool>("--allow-mock", () => true, "Enable mock/offline providers.");
        var runTestsOpt = new Option<bool>("--run-tests", () => true, "Run generated extension QA/test gates.");
        var testFilterOpt = new Option<string>("--test-filter", () => "SelfExtendGenerated", "Functional test filter.");
        var bootstrapProfileOpt = new Option<string>("--bootstrap-profile", () => "auto", "Bootstrap profile: auto | self-extend-functional | self-extend-aesthetic | self-extend-visual.");
        var qaPolicyOpt = new Option<string>("--qa-policy", () => "auto", "QA policy: auto | demo | release | prod | research.");
        var manifestPathOpt = new Option<string?>("--runtime-manifest", () => null, "Path to adaptive runtime manifest JSON.");
        var manifestJsonOpt = new Option<string?>("--runtime-manifest-json", () => null, "Inline adaptive runtime manifest JSON.");
        var maxIterationsOpt = new Option<int?>("--max-iterations", () => null, "Override max iterations from policy.");
        var bootstrapApplyOpt = new Option<bool>("--bootstrap-apply", () => true, "Apply missing required bootstrap dependencies.");
        var bootstrapYesOpt = new Option<bool>("--bootstrap-yes", () => true, "Auto-approve bootstrap install plan.");
        var bootstrapDryRunOpt = new Option<bool>("--bootstrap-dry-run", () => false, "Show bootstrap install plan without executing.");
        var preflightOpt = new Option<bool>("--preflight", () => true, "Run self-extend preflight before execution.");
        var useHistoryOpt = new Option<bool>("--use-history", () => true, "Use recent runtime history to adapt auto policy selection.");
        var historyWindowOpt = new Option<int>("--history-window", () => 200, "How many recent runtime reports are considered for adaptation.");
        var persistHistoryOpt = new Option<bool>("--persist-history", () => true, "Persist this execution result into runtime history.");
        var benchmarkSetOpt = new Option<string>("--benchmark-set", () => "adhoc", "Benchmark set tag persisted with execution history.");
        var allowVisualCapabilityDegradeOpt = new Option<bool>("--allow-visual-capability-degrade", () => false, "If visual infra is unavailable, downgrade strict visual fallback to degrade for this run.");
        var autoRemediateOpt = new Option<bool>("--auto-remediate", () => true, "If execution fails, attempt one adaptive policy remediation pass.");
        var maxRemediationAttemptsOpt = new Option<int>("--max-remediation-attempts", () => 1, "Maximum remediation attempts after initial failure.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output.");

        executeCmd.AddOption(goalOpt);
        executeCmd.AddOption(repoRootOpt);
        executeCmd.AddOption(providerOpt);
        executeCmd.AddOption(allowMockOpt);
        executeCmd.AddOption(runTestsOpt);
        executeCmd.AddOption(testFilterOpt);
        executeCmd.AddOption(bootstrapProfileOpt);
        executeCmd.AddOption(qaPolicyOpt);
        executeCmd.AddOption(manifestPathOpt);
        executeCmd.AddOption(manifestJsonOpt);
        executeCmd.AddOption(maxIterationsOpt);
        executeCmd.AddOption(bootstrapApplyOpt);
        executeCmd.AddOption(bootstrapYesOpt);
        executeCmd.AddOption(bootstrapDryRunOpt);
        executeCmd.AddOption(preflightOpt);
        executeCmd.AddOption(useHistoryOpt);
        executeCmd.AddOption(historyWindowOpt);
        executeCmd.AddOption(persistHistoryOpt);
        executeCmd.AddOption(benchmarkSetOpt);
        executeCmd.AddOption(allowVisualCapabilityDegradeOpt);
        executeCmd.AddOption(autoRemediateOpt);
        executeCmd.AddOption(maxRemediationAttemptsOpt);
        executeCmd.AddOption(jsonOpt);
        executeCmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ExecuteAsync(
                ctx.ParseResult.GetValueForOption(goalOpt) ?? string.Empty,
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(providerOpt),
                ctx.ParseResult.GetValueForOption(allowMockOpt),
                ctx.ParseResult.GetValueForOption(runTestsOpt),
                ctx.ParseResult.GetValueForOption(testFilterOpt) ?? "SelfExtendGenerated",
                ctx.ParseResult.GetValueForOption(bootstrapProfileOpt) ?? "auto",
                ctx.ParseResult.GetValueForOption(qaPolicyOpt) ?? "auto",
                ctx.ParseResult.GetValueForOption(manifestPathOpt),
                ctx.ParseResult.GetValueForOption(manifestJsonOpt),
                ctx.ParseResult.GetValueForOption(maxIterationsOpt),
                ctx.ParseResult.GetValueForOption(bootstrapApplyOpt),
                ctx.ParseResult.GetValueForOption(bootstrapYesOpt),
                ctx.ParseResult.GetValueForOption(bootstrapDryRunOpt),
                ctx.ParseResult.GetValueForOption(preflightOpt),
                ctx.ParseResult.GetValueForOption(useHistoryOpt),
                ctx.ParseResult.GetValueForOption(historyWindowOpt),
                ctx.ParseResult.GetValueForOption(persistHistoryOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt) ?? "adhoc",
                ctx.ParseResult.GetValueForOption(allowVisualCapabilityDegradeOpt),
                ctx.ParseResult.GetValueForOption(autoRemediateOpt),
                ctx.ParseResult.GetValueForOption(maxRemediationAttemptsOpt),
                CommandExecutionSupport.WantsJson(ctx.ParseResult, jsonOpt),
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        /// <summary>Add command.</summary>
        AddCommand(executeCmd);
    }

    private void ConfigurePlanCommand()
    {
        var planCmd = new Command("plan", "Dry-run adaptive planning without executing self-extend.");
        var goalOpt = new Option<string>("--goal", "Objective to plan adaptively.") { IsRequired = true };
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var testFilterOpt = new Option<string>("--test-filter", () => "SelfExtendGenerated", "Functional test filter.");
        var bootstrapProfileOpt = new Option<string>("--bootstrap-profile", () => "auto", "Bootstrap profile: auto | self-extend-functional | self-extend-aesthetic | self-extend-visual.");
        var qaPolicyOpt = new Option<string>("--qa-policy", () => "auto", "QA policy: auto | demo | release | prod | research.");
        var manifestPathOpt = new Option<string?>("--runtime-manifest", () => null, "Path to adaptive runtime manifest JSON.");
        var manifestJsonOpt = new Option<string?>("--runtime-manifest-json", () => null, "Inline adaptive runtime manifest JSON.");
        var maxIterationsOpt = new Option<int?>("--max-iterations", () => null, "Override max iterations from policy.");
        var useHistoryOpt = new Option<bool>("--use-history", () => true, "Use recent runtime history to adapt auto policy selection.");
        var historyWindowOpt = new Option<int>("--history-window", () => 200, "How many recent runtime reports are considered for adaptation.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output.");

        planCmd.AddOption(goalOpt);
        planCmd.AddOption(repoRootOpt);
        planCmd.AddOption(testFilterOpt);
        planCmd.AddOption(bootstrapProfileOpt);
        planCmd.AddOption(qaPolicyOpt);
        planCmd.AddOption(manifestPathOpt);
        planCmd.AddOption(manifestJsonOpt);
        planCmd.AddOption(maxIterationsOpt);
        planCmd.AddOption(useHistoryOpt);
        planCmd.AddOption(historyWindowOpt);
        planCmd.AddOption(jsonOpt);
        planCmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ExecutePlanAsync(
                ctx.ParseResult.GetValueForOption(goalOpt) ?? string.Empty,
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(testFilterOpt) ?? "SelfExtendGenerated",
                ctx.ParseResult.GetValueForOption(bootstrapProfileOpt) ?? "auto",
                ctx.ParseResult.GetValueForOption(qaPolicyOpt) ?? "auto",
                ctx.ParseResult.GetValueForOption(manifestPathOpt),
                ctx.ParseResult.GetValueForOption(manifestJsonOpt),
                ctx.ParseResult.GetValueForOption(maxIterationsOpt),
                ctx.ParseResult.GetValueForOption(useHistoryOpt),
                ctx.ParseResult.GetValueForOption(historyWindowOpt),
                CommandExecutionSupport.WantsJson(ctx.ParseResult, jsonOpt)).ConfigureAwait(false);
        });
        /// <summary>Add command.</summary>
        AddCommand(planCmd);
    }

    private void ConfigureEvaluateCommand()
    {
        var evaluateCmd = new Command("evaluate", "Run a runtime policy matrix over one or more goals.");
        var goalsJsonOpt = new Option<string?>("--goals-json", () => null, "JSON array of goals to evaluate.");
        var goalsFileOpt = new Option<string?>("--goals-file", () => null, "Path to a newline-delimited goals file.");
        var policiesOpt = new Option<string>("--policies", () => "demo,prod,research", "Comma-separated policy list for matrix evaluation.");
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var providerOpt = new Option<string?>("--provider", () => "mock-json", "Model provider override.");
        var allowMockOpt = new Option<bool>("--allow-mock", () => true, "Enable mock/offline providers.");
        var runTestsOpt = new Option<bool>("--run-tests", () => false, "Run generated extension QA/test gates.");
        var testFilterOpt = new Option<string>("--test-filter", () => "SelfExtendGenerated", "Functional test filter.");
        var bootstrapProfileOpt = new Option<string>("--bootstrap-profile", () => "auto", "Bootstrap profile: auto | self-extend-functional | self-extend-aesthetic | self-extend-visual.");
        var manifestPathOpt = new Option<string?>("--runtime-manifest", () => null, "Path to adaptive runtime manifest JSON.");
        var manifestJsonOpt = new Option<string?>("--runtime-manifest-json", () => null, "Inline adaptive runtime manifest JSON.");
        var maxIterationsOpt = new Option<int?>("--max-iterations", () => null, "Override max iterations from policy.");
        var bootstrapApplyOpt = new Option<bool>("--bootstrap-apply", () => false, "Apply missing required bootstrap dependencies.");
        var preflightOpt = new Option<bool>("--preflight", () => true, "Run self-extend preflight before execution.");
        var useHistoryOpt = new Option<bool>("--use-history", () => true, "Use recent runtime history for adaptive decisions when policy=auto.");
        var historyWindowOpt = new Option<int>("--history-window", () => 200, "How many recent runtime reports are considered for adaptation.");
        var persistHistoryOpt = new Option<bool>("--persist-history", () => false, "Persist each matrix scenario into runtime history.");
        var benchmarkSetOpt = new Option<string>("--benchmark-set", () => "adhoc", "Benchmark set tag persisted with matrix execution history.");
        var allowVisualCapabilityDegradeOpt = new Option<bool>("--allow-visual-capability-degrade", () => false, "If visual infra is unavailable, downgrade strict visual fallback to degrade for this matrix execution.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output.");

        evaluateCmd.AddOption(goalsJsonOpt);
        evaluateCmd.AddOption(goalsFileOpt);
        evaluateCmd.AddOption(policiesOpt);
        evaluateCmd.AddOption(repoRootOpt);
        evaluateCmd.AddOption(providerOpt);
        evaluateCmd.AddOption(allowMockOpt);
        evaluateCmd.AddOption(runTestsOpt);
        evaluateCmd.AddOption(testFilterOpt);
        evaluateCmd.AddOption(bootstrapProfileOpt);
        evaluateCmd.AddOption(manifestPathOpt);
        evaluateCmd.AddOption(manifestJsonOpt);
        evaluateCmd.AddOption(maxIterationsOpt);
        evaluateCmd.AddOption(bootstrapApplyOpt);
        evaluateCmd.AddOption(preflightOpt);
        evaluateCmd.AddOption(useHistoryOpt);
        evaluateCmd.AddOption(historyWindowOpt);
        evaluateCmd.AddOption(persistHistoryOpt);
        evaluateCmd.AddOption(benchmarkSetOpt);
        evaluateCmd.AddOption(allowVisualCapabilityDegradeOpt);
        evaluateCmd.AddOption(jsonOpt);
        evaluateCmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ExecuteEvaluateAsync(
                ctx.ParseResult.GetValueForOption(goalsJsonOpt),
                ctx.ParseResult.GetValueForOption(goalsFileOpt),
                ctx.ParseResult.GetValueForOption(policiesOpt) ?? "demo,prod,research",
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(providerOpt),
                ctx.ParseResult.GetValueForOption(allowMockOpt),
                ctx.ParseResult.GetValueForOption(runTestsOpt),
                ctx.ParseResult.GetValueForOption(testFilterOpt) ?? "SelfExtendGenerated",
                ctx.ParseResult.GetValueForOption(bootstrapProfileOpt) ?? "auto",
                ctx.ParseResult.GetValueForOption(manifestPathOpt),
                ctx.ParseResult.GetValueForOption(manifestJsonOpt),
                ctx.ParseResult.GetValueForOption(maxIterationsOpt),
                ctx.ParseResult.GetValueForOption(bootstrapApplyOpt),
                ctx.ParseResult.GetValueForOption(preflightOpt),
                ctx.ParseResult.GetValueForOption(useHistoryOpt),
                ctx.ParseResult.GetValueForOption(historyWindowOpt),
                ctx.ParseResult.GetValueForOption(persistHistoryOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt) ?? "adhoc",
                ctx.ParseResult.GetValueForOption(allowVisualCapabilityDegradeOpt),
                CommandExecutionSupport.WantsJson(ctx.ParseResult, jsonOpt),
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        /// <summary>Add command.</summary>
        AddCommand(evaluateCmd);
    }

    private void ConfigureHistoryCommand()
    {
        var historyCmd = new Command("history", "Show persisted runtime execution history.");
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var limitOpt = new Option<int>("--limit", () => 20, "Maximum number of history records to return.");
        var goalOpt = new Option<string?>("--goal", () => null, "Optional goal text to filter by goal fingerprint.");
        var policyOpt = new Option<string?>("--policy", () => null, "Optional resolved QA policy filter.");
        var benchmarkSetOpt = new Option<string?>("--benchmark-set", () => null, "Optional benchmark set tag filter.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output.");
        historyCmd.AddOption(repoRootOpt);
        historyCmd.AddOption(limitOpt);
        historyCmd.AddOption(goalOpt);
        historyCmd.AddOption(policyOpt);
        historyCmd.AddOption(benchmarkSetOpt);
        historyCmd.AddOption(jsonOpt);
        historyCmd.SetHandler((InvocationContext ctx) =>
        {
            ctx.ExitCode = ExecuteHistoryAsync(
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(limitOpt),
                ctx.ParseResult.GetValueForOption(goalOpt),
                ctx.ParseResult.GetValueForOption(policyOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt),
                CommandExecutionSupport.WantsJson(ctx.ParseResult, jsonOpt)).GetAwaiter().GetResult();
        });
        /// <summary>Add command.</summary>
        AddCommand(historyCmd);
    }

    private void ConfigureRecommendCommand()
    {
        var recommendCmd = new Command("recommend", "Recommend QA policy for a goal based on runtime history.");
        var goalOpt = new Option<string>("--goal", "Goal to recommend QA policy for.") { IsRequired = true };
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var historyWindowOpt = new Option<int>("--history-window", () => 200, "How many recent runtime reports are considered.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output.");
        recommendCmd.AddOption(goalOpt);
        recommendCmd.AddOption(repoRootOpt);
        recommendCmd.AddOption(historyWindowOpt);
        recommendCmd.AddOption(jsonOpt);
        recommendCmd.SetHandler((InvocationContext ctx) =>
        {
            ctx.ExitCode = ExecuteRecommendAsync(
                ctx.ParseResult.GetValueForOption(goalOpt) ?? string.Empty,
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(historyWindowOpt),
                CommandExecutionSupport.WantsJson(ctx.ParseResult, jsonOpt)).GetAwaiter().GetResult();
        });
        /// <summary>Add command.</summary>
        AddCommand(recommendCmd);
    }

    private void ConfigureGateCommand()
    {
        var gateCmd = new Command("gate", "Evaluate runtime SLO gate from persisted history.");
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var historyWindowOpt = new Option<int>("--history-window", () => 100, "How many recent runtime reports are considered.");
        var minPassRateOpt = new Option<double>("--min-pass-rate", () => 0.8, "Minimum required pass rate in [0,1].");
        var minTotalOpt = new Option<int>("--min-total", () => 5, "Minimum runs required to evaluate gate.");
        var goalOpt = new Option<string?>("--goal", () => null, "Optional goal text to filter by goal fingerprint.");
        var policyOpt = new Option<string?>("--policy", () => null, "Optional resolved QA policy filter.");
        var benchmarkSetOpt = new Option<string?>("--benchmark-set", () => null, "Optional benchmark set tag filter.");
        var stageOpt = new Option<string?>("--stage", () => null, "Optional failure stage filter (none|bootstrap|preflight|self-extend).");
        var minConsecutivePassesOpt = new Option<int>("--min-consecutive-passes", () => 0, "Minimum required consecutive recent pass streak.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output.");
        gateCmd.AddOption(repoRootOpt);
        gateCmd.AddOption(historyWindowOpt);
        gateCmd.AddOption(minPassRateOpt);
        gateCmd.AddOption(minTotalOpt);
        gateCmd.AddOption(goalOpt);
        gateCmd.AddOption(policyOpt);
        gateCmd.AddOption(benchmarkSetOpt);
        gateCmd.AddOption(stageOpt);
        gateCmd.AddOption(minConsecutivePassesOpt);
        gateCmd.AddOption(jsonOpt);
        gateCmd.SetHandler((InvocationContext ctx) =>
        {
            ctx.ExitCode = ExecuteGateAsync(
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(historyWindowOpt),
                ctx.ParseResult.GetValueForOption(minPassRateOpt),
                ctx.ParseResult.GetValueForOption(minTotalOpt),
                ctx.ParseResult.GetValueForOption(goalOpt),
                ctx.ParseResult.GetValueForOption(policyOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(stageOpt),
                ctx.ParseResult.GetValueForOption(minConsecutivePassesOpt),
                CommandExecutionSupport.WantsJson(ctx.ParseResult, jsonOpt)).GetAwaiter().GetResult();
        });
        /// <summary>Add command.</summary>
        AddCommand(gateCmd);
    }

    private void ConfigureReleaseGateCommand()
    {
        var releaseGateCmd = new Command("release-gate", "Run release promotion lanes (core, visual, chaos) without shell scripts.");
        var modeOpt = new Option<string>("--mode", () => "full", "Lane mode: core | visual | chaos | full.");
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var providerOpt = new Option<string>("--provider", () => RuntimeCommandUtilities.ReadEnvString("ASHLAR_RELEASE_PROVIDER", "mock-json"), "Model provider override.");
        var allowMockOpt = new Option<bool>("--allow-mock", () => true, "Enable mock/offline providers.");
        var runTestsOpt = new Option<bool>("--run-tests", () => true, "Run generated extension QA/test gates.");
        var testFilterOpt = new Option<string>("--test-filter", () => "SelfExtendGenerated", "Functional test filter.");
        var preflightOpt = new Option<bool>("--preflight", () => true, "Run self-extend preflight before execution.");

        var coreMinPassRateOpt = new Option<double>("--core-min-pass-rate",
            () => RuntimeCommandUtilities.ReadEnvDouble("ASHLAR_RELEASE_CORE_MIN_PASS_RATE", RuntimeCommandUtilities.ReadEnvDouble("ASHLAR_RELEASE_MIN_PASS_RATE", 0.85d)),
            "Minimum pass-rate required for release-core gate.");
        var coreMinTotalOpt = new Option<int>("--core-min-total",
            () => RuntimeCommandUtilities.ReadEnvInt("ASHLAR_RELEASE_CORE_MIN_TOTAL", RuntimeCommandUtilities.ReadEnvInt("ASHLAR_RELEASE_MIN_TOTAL", 10)),
            "Minimum sample size required for release-core gate.");
        var coreHistoryWindowOpt = new Option<int>("--core-history-window",
            () => RuntimeCommandUtilities.ReadEnvInt("ASHLAR_RELEASE_CORE_HISTORY_WINDOW", RuntimeCommandUtilities.ReadEnvInt("ASHLAR_RELEASE_HISTORY_WINDOW", 20)),
            "History window for release-core gate.");

        var visualMinPassRateOpt = new Option<double>("--visual-min-pass-rate",
            () => RuntimeCommandUtilities.ReadEnvDouble("ASHLAR_RELEASE_VISUAL_MIN_PASS_RATE", 0.80d),
            "Minimum pass-rate required for release-visual gate.");
        var visualMinTotalOpt = new Option<int>("--visual-min-total",
            () => RuntimeCommandUtilities.ReadEnvInt("ASHLAR_RELEASE_VISUAL_MIN_TOTAL", 8),
            "Minimum sample size required for release-visual gate.");
        var visualHistoryWindowOpt = new Option<int>("--visual-history-window",
            () => RuntimeCommandUtilities.ReadEnvInt("ASHLAR_RELEASE_VISUAL_HISTORY_WINDOW", 20),
            "History window for release-visual gate.");
        var visualPromotionStreakOpt = new Option<int>("--visual-promotion-streak",
            () => RuntimeCommandUtilities.ReadEnvInt("ASHLAR_VISUAL_PROMOTION_STREAK", 3),
            "Consecutive pass streak required before visual lane becomes mandatory in auto mode.");
        var visualRequiredModeOpt = new Option<string>("--visual-required-mode",
            () => RuntimeCommandUtilities.ReadEnvString("ASHLAR_VISUAL_REQUIRED_MODE", "auto"),
            "Visual lane requirement mode: auto | true | false.");
        var laneRepetitionsOpt = new Option<int>("--lane-repetitions",
            () => RuntimeCommandUtilities.ReadEnvInt("ASHLAR_RELEASE_LANE_REPETITIONS", 1),
            "How many times each release lane matrix should execute before gating.");
        var sloWarningOnlyOpt = new Option<bool>("--slo-warning-only", () => false, "Emit SLO evidence but do not fail release gate on SLO threshold breaches.");
        var emitSloEvidenceOpt = new Option<bool>("--emit-slo-evidence", () => true, "Emit machine-readable SLO evidence artifacts.");
        var evidenceOutputOpt = new Option<string?>(
            ["--evidence-output", "--slo-evidence-path"],
            () => null,
            "Optional path to write machine-readable SLO evidence JSON.");
        var ncrResolutionMsSloOpt = new Option<double>(
            "--ncr-resolution-ms-slo",
            () => RuntimeCommandUtilities.ReadEnvDouble("ASHLAR_RELEASE_SLO_NCR_RESOLUTION_MS", 250d),
            "Maximum NCR model-resolution P95 duration (ms).");
        var ncrLoadMsSloOpt = new Option<double>(
            "--ncr-load-ms-slo",
            () => RuntimeCommandUtilities.ReadEnvDouble("ASHLAR_RELEASE_SLO_NCR_LOAD_MS", 1000d),
            "Maximum NCR model-load P95 duration (ms).");
        var ncrOutcomeMsSloOpt = new Option<double>(
            "--ncr-outcome-ms-slo",
            () => RuntimeCommandUtilities.ReadEnvDouble("ASHLAR_RELEASE_SLO_NCR_OUTCOME_MS", 1500d),
            "Maximum NCR execution outcome P95 duration (ms).");
        var ncrFailureRateSloOpt = new Option<double>(
            "--ncr-failure-rate-slo",
            () => RuntimeCommandUtilities.ReadEnvDouble("ASHLAR_RELEASE_SLO_NCR_FAILURE_RATE", 0.2d),
            "Maximum allowed NCR failure rate across release benchmark history [0,1].");

        var jsonOpt = new Option<bool>("--json", () => false, "Emit JSON output for underlying lane evaluations.");

        releaseGateCmd.AddOption(modeOpt);
        releaseGateCmd.AddOption(repoRootOpt);
        releaseGateCmd.AddOption(providerOpt);
        releaseGateCmd.AddOption(allowMockOpt);
        releaseGateCmd.AddOption(runTestsOpt);
        releaseGateCmd.AddOption(testFilterOpt);
        releaseGateCmd.AddOption(preflightOpt);
        releaseGateCmd.AddOption(coreMinPassRateOpt);
        releaseGateCmd.AddOption(coreMinTotalOpt);
        releaseGateCmd.AddOption(coreHistoryWindowOpt);
        releaseGateCmd.AddOption(visualMinPassRateOpt);
        releaseGateCmd.AddOption(visualMinTotalOpt);
        releaseGateCmd.AddOption(visualHistoryWindowOpt);
        releaseGateCmd.AddOption(visualPromotionStreakOpt);
        releaseGateCmd.AddOption(visualRequiredModeOpt);
        releaseGateCmd.AddOption(laneRepetitionsOpt);
        releaseGateCmd.AddOption(sloWarningOnlyOpt);
        releaseGateCmd.AddOption(emitSloEvidenceOpt);
        releaseGateCmd.AddOption(evidenceOutputOpt);
        releaseGateCmd.AddOption(ncrResolutionMsSloOpt);
        releaseGateCmd.AddOption(ncrLoadMsSloOpt);
        releaseGateCmd.AddOption(ncrOutcomeMsSloOpt);
        releaseGateCmd.AddOption(ncrFailureRateSloOpt);
        releaseGateCmd.AddOption(jsonOpt);

        releaseGateCmd.SetHandler(async (InvocationContext ctx) =>
        {
            ctx.ExitCode = await ExecuteReleaseGateAsync(
                ctx.ParseResult.GetValueForOption(modeOpt) ?? "full",
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(providerOpt) ?? "mock-json",
                ctx.ParseResult.GetValueForOption(allowMockOpt),
                ctx.ParseResult.GetValueForOption(runTestsOpt),
                ctx.ParseResult.GetValueForOption(testFilterOpt) ?? "SelfExtendGenerated",
                ctx.ParseResult.GetValueForOption(preflightOpt),
                ctx.ParseResult.GetValueForOption(coreMinPassRateOpt),
                ctx.ParseResult.GetValueForOption(coreMinTotalOpt),
                ctx.ParseResult.GetValueForOption(coreHistoryWindowOpt),
                ctx.ParseResult.GetValueForOption(visualMinPassRateOpt),
                ctx.ParseResult.GetValueForOption(visualMinTotalOpt),
                ctx.ParseResult.GetValueForOption(visualHistoryWindowOpt),
                ctx.ParseResult.GetValueForOption(visualPromotionStreakOpt),
                ctx.ParseResult.GetValueForOption(visualRequiredModeOpt) ?? "auto",
                ctx.ParseResult.GetValueForOption(laneRepetitionsOpt),
                ctx.ParseResult.GetValueForOption(sloWarningOnlyOpt),
                ctx.ParseResult.GetValueForOption(emitSloEvidenceOpt),
                ctx.ParseResult.GetValueForOption(evidenceOutputOpt),
                ctx.ParseResult.GetValueForOption(ncrResolutionMsSloOpt),
                ctx.ParseResult.GetValueForOption(ncrLoadMsSloOpt),
                ctx.ParseResult.GetValueForOption(ncrOutcomeMsSloOpt),
                ctx.ParseResult.GetValueForOption(ncrFailureRateSloOpt),
                CommandExecutionSupport.WantsJson(ctx.ParseResult, jsonOpt),
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        /// <summary>Add command.</summary>
        AddCommand(releaseGateCmd);
    }

}
