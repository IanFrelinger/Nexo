using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands;

/// <summary>
/// Adaptive runtime control-plane command.
/// Provides planning, execution, and matrix evaluation with history-informed policy tuning.
/// </summary>
public sealed class RuntimeCommand : Command
{
    public RuntimeCommand() : base("runtime", "Adaptive runtime control plane commands.")
    {
        ConfigureExecuteCommand();
        ConfigurePlanCommand();
        ConfigureEvaluateCommand();
        ConfigureHistoryCommand();
        ConfigureRecommendCommand();
        ConfigureGateCommand();
        ConfigureReleaseGateCommand();
    }

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
                ctx.ParseResult.GetValueForOption(jsonOpt),
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });
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
                ctx.ParseResult.GetValueForOption(jsonOpt)).ConfigureAwait(false);
        });
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
                ctx.ParseResult.GetValueForOption(jsonOpt),
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });
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
                ctx.ParseResult.GetValueForOption(jsonOpt)).GetAwaiter().GetResult();
        });
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
                ctx.ParseResult.GetValueForOption(jsonOpt)).GetAwaiter().GetResult();
        });
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
                ctx.ParseResult.GetValueForOption(jsonOpt)).GetAwaiter().GetResult();
        });
        AddCommand(gateCmd);
    }

    private void ConfigureReleaseGateCommand()
    {
        var releaseGateCmd = new Command("release-gate", "Run release promotion lanes (core, visual, chaos) without shell scripts.");
        var modeOpt = new Option<string>("--mode", () => "full", "Lane mode: core | visual | chaos | full.");
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var providerOpt = new Option<string>("--provider", () => ReadEnvString("NEXO_RELEASE_PROVIDER", "mock-json"), "Model provider override.");
        var allowMockOpt = new Option<bool>("--allow-mock", () => true, "Enable mock/offline providers.");
        var runTestsOpt = new Option<bool>("--run-tests", () => true, "Run generated extension QA/test gates.");
        var testFilterOpt = new Option<string>("--test-filter", () => "SelfExtendGenerated", "Functional test filter.");
        var preflightOpt = new Option<bool>("--preflight", () => true, "Run self-extend preflight before execution.");

        var coreMinPassRateOpt = new Option<double>("--core-min-pass-rate",
            () => ReadEnvDouble("NEXO_RELEASE_CORE_MIN_PASS_RATE", ReadEnvDouble("NEXO_RELEASE_MIN_PASS_RATE", 0.85d)),
            "Minimum pass-rate required for release-core gate.");
        var coreMinTotalOpt = new Option<int>("--core-min-total",
            () => ReadEnvInt("NEXO_RELEASE_CORE_MIN_TOTAL", ReadEnvInt("NEXO_RELEASE_MIN_TOTAL", 10)),
            "Minimum sample size required for release-core gate.");
        var coreHistoryWindowOpt = new Option<int>("--core-history-window",
            () => ReadEnvInt("NEXO_RELEASE_CORE_HISTORY_WINDOW", ReadEnvInt("NEXO_RELEASE_HISTORY_WINDOW", 20)),
            "History window for release-core gate.");

        var visualMinPassRateOpt = new Option<double>("--visual-min-pass-rate",
            () => ReadEnvDouble("NEXO_RELEASE_VISUAL_MIN_PASS_RATE", 0.80d),
            "Minimum pass-rate required for release-visual gate.");
        var visualMinTotalOpt = new Option<int>("--visual-min-total",
            () => ReadEnvInt("NEXO_RELEASE_VISUAL_MIN_TOTAL", 8),
            "Minimum sample size required for release-visual gate.");
        var visualHistoryWindowOpt = new Option<int>("--visual-history-window",
            () => ReadEnvInt("NEXO_RELEASE_VISUAL_HISTORY_WINDOW", 20),
            "History window for release-visual gate.");
        var visualPromotionStreakOpt = new Option<int>("--visual-promotion-streak",
            () => ReadEnvInt("NEXO_VISUAL_PROMOTION_STREAK", 3),
            "Consecutive pass streak required before visual lane becomes mandatory in auto mode.");
        var visualRequiredModeOpt = new Option<string>("--visual-required-mode",
            () => ReadEnvString("NEXO_VISUAL_REQUIRED_MODE", "auto"),
            "Visual lane requirement mode: auto | true | false.");
        var laneRepetitionsOpt = new Option<int>("--lane-repetitions",
            () => ReadEnvInt("NEXO_RELEASE_LANE_REPETITIONS", 1),
            "How many times each release lane matrix should execute before gating.");
        var sloWarningOnlyOpt = new Option<bool>("--slo-warning-only", () => false, "Emit SLO evidence but do not fail release gate on SLO threshold breaches.");
        var emitSloEvidenceOpt = new Option<bool>("--emit-slo-evidence", () => true, "Emit machine-readable SLO evidence artifacts.");
        var evidenceOutputOpt = new Option<string?>(
            ["--evidence-output", "--slo-evidence-path"],
            () => null,
            "Optional path to write machine-readable SLO evidence JSON.");
        var ncrResolutionMsSloOpt = new Option<double>(
            "--ncr-resolution-ms-slo",
            () => ReadEnvDouble("NEXO_RELEASE_SLO_NCR_RESOLUTION_MS", 250d),
            "Maximum NCR model-resolution P95 duration (ms).");
        var ncrLoadMsSloOpt = new Option<double>(
            "--ncr-load-ms-slo",
            () => ReadEnvDouble("NEXO_RELEASE_SLO_NCR_LOAD_MS", 1000d),
            "Maximum NCR model-load P95 duration (ms).");
        var ncrOutcomeMsSloOpt = new Option<double>(
            "--ncr-outcome-ms-slo",
            () => ReadEnvDouble("NEXO_RELEASE_SLO_NCR_OUTCOME_MS", 1500d),
            "Maximum NCR execution outcome P95 duration (ms).");
        var ncrFailureRateSloOpt = new Option<double>(
            "--ncr-failure-rate-slo",
            () => ReadEnvDouble("NEXO_RELEASE_SLO_NCR_FAILURE_RATE", 0.2d),
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
                ctx.ParseResult.GetValueForOption(jsonOpt),
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        AddCommand(releaseGateCmd);
    }

    internal async Task<int> ExecuteAsync(
        string goal,
        string repoRoot,
        string? provider,
        bool allowMock,
        bool runTests,
        string testFilter,
        string bootstrapProfile,
        string qaPolicy,
        string? runtimeManifestPath,
        string? runtimeManifestJson,
        int? maxIterationsOverride,
        bool bootstrapApply,
        bool bootstrapYes,
        bool bootstrapDryRun,
        bool runPreflight,
        bool useHistory,
        int historyWindow,
        bool persistHistory,
        string benchmarkSet,
        bool allowVisualCapabilityDegrade,
        bool autoRemediate,
        int maxRemediationAttempts,
        bool json,
        CancellationToken ct)
    {
        var result = await ExecuteCoreAsync(
            goal,
            repoRoot,
            provider,
            allowMock,
            runTests,
            testFilter,
            bootstrapProfile,
            qaPolicy,
            runtimeManifestPath,
            runtimeManifestJson,
            maxIterationsOverride,
            bootstrapApply,
            bootstrapYes,
            bootstrapDryRun,
            runPreflight,
            useHistory,
            historyWindow,
            persistHistory,
            benchmarkSet,
            allowVisualCapabilityDegrade,
            ct).ConfigureAwait(false);

        var remediationAttempts = new List<RuntimeRemediationAttempt>();
        var attemptsBudget = Math.Max(0, maxRemediationAttempts);
        var remediationAllowed = autoRemediate &&
            !string.Equals(result.ResolvedQaPolicy, "release", StringComparison.OrdinalIgnoreCase);
        if (autoRemediate && !remediationAllowed && !result.Ok)
        {
            result = result with
            {
                Summary = $"{result.Summary} (auto-remediation disabled for release policy)"
            };
        }
        while (remediationAllowed && !result.Ok && attemptsBudget > 0)
        {
            var remediation = ChooseRemediationPolicy(result);
            if (remediation == null)
                break;
            if (remediationAttempts.Any(a => string.Equals(a.Policy, remediation.Policy, StringComparison.OrdinalIgnoreCase)))
                break;

            var retried = await ExecuteCoreAsync(
                goal,
                repoRoot,
                provider,
                allowMock,
                runTests,
                testFilter,
                bootstrapProfile,
                remediation.Policy,
                runtimeManifestPath,
                runtimeManifestJson,
                maxIterationsOverride,
                bootstrapApply,
                bootstrapYes,
                bootstrapDryRun,
                runPreflight,
                useHistory: false,
                historyWindow,
                persistHistory,
                benchmarkSet,
                allowVisualCapabilityDegrade,
                ct).ConfigureAwait(false);

            remediationAttempts.Add(new RuntimeRemediationAttempt(
                remediation.Policy,
                remediation.Reason,
                retried.Ok,
                retried.FailureStage,
                retried.RunId,
                retried.Summary));
            attemptsBudget--;
            result = retried;

            if (retried.Ok)
            {
                result = result with
                {
                    Summary = $"{result.Summary} (auto-remediated)",
                    RemediationAttempts = remediationAttempts.ToArray()
                };
                break;
            }
        }

        if (remediationAttempts.Count > 0 && result.RemediationAttempts == null)
            result = result with { RemediationAttempts = remediationAttempts.ToArray() };

        RuntimeOutputWriter.WriteResult(result, json);
        return result.Ok ? 0 : 1;
    }

    internal Task<int> ExecutePlanAsync(
        string goal,
        string repoRoot,
        string testFilter,
        string bootstrapProfile,
        string qaPolicy,
        string? runtimeManifestPath,
        string? runtimeManifestJson,
        int? maxIterationsOverride,
        bool useHistory,
        int historyWindow,
        bool json)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            RuntimeOutputWriter.WritePlanResult(new RuntimePlanResult(false, "Goal is required."), json);
            return Task.FromResult(1);
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            RuntimeOutputWriter.WritePlanResult(new RuntimePlanResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        try
        {
            var context = BuildPlanContext(
                goal,
                fullRepoRoot,
                testFilter,
                bootstrapProfile,
                qaPolicy,
                runtimeManifestPath,
                runtimeManifestJson,
                maxIterationsOverride,
                useHistory,
                historyWindow);
            RuntimeOutputWriter.WritePlanResult(new RuntimePlanResult(
                true,
                "Plan computed successfully.",
                context.Plan,
                context.WorkflowSpec,
                context.AdaptivePolicyReason), json);
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            RuntimeOutputWriter.WritePlanResult(new RuntimePlanResult(false, $"Failed to compute plan: {ex.Message}"), json);
            return Task.FromResult(1);
        }
    }

    internal Task<int> ExecuteHistoryAsync(
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
            var p = NormalizeQaPolicy(policy);
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

    internal Task<int> ExecuteRecommendAsync(
        string goal,
        string repoRoot,
        int historyWindow,
        bool json)
    {
        if (string.IsNullOrWhiteSpace(goal))
        {
            RuntimeOutputWriter.WriteRecommendResult(new RuntimeRecommendResult(false, "Goal is required."), json);
            return Task.FromResult(1);
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            RuntimeOutputWriter.WriteRecommendResult(new RuntimeRecommendResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        var history = AdaptiveRuntimeExecutionHistoryStore.ReadRecent(fullRepoRoot, Math.Max(1, historyWindow));
        var recommendation = AdaptiveRuntimePolicyAdvisor.RecommendQaPolicy(goal, history);
        var result = recommendation == null
            ? new RuntimeRecommendResult(true, "No recommendation from history.", Policy: null, Reason: null)
            : new RuntimeRecommendResult(true, "Recommendation computed from history.", recommendation.QaPolicy, recommendation.Reason);
        RuntimeOutputWriter.WriteRecommendResult(result, json);
        return Task.FromResult(0);
    }

    internal Task<int> ExecuteGateAsync(
        string repoRoot,
        int historyWindow,
        double minPassRate,
        int minTotal,
        string? goal,
        string? policy,
        string? benchmarkSet,
        string? stage,
        int minConsecutivePasses,
        bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            RuntimeOutputWriter.WriteGateResult(new RuntimeGateResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        var gate = EvaluateGateResult(
            fullRepoRoot,
            historyWindow,
            minPassRate,
            minTotal,
            goal,
            policy,
            benchmarkSet,
            stage,
            minConsecutivePasses);
        RuntimeOutputWriter.WriteGateResult(gate, json);
        return Task.FromResult(gate.Ok ? 0 : 1);
    }

    internal async Task<int> ExecuteReleaseGateAsync(
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

        var normalizedMode = NormalizeReleaseGateMode(mode);
        if (normalizedMode == "invalid")
        {
            Console.Error.WriteLine($"runtime release-gate: unsupported mode '{mode}'. Use core | visual | chaos | full.");
            return 1;
        }
        var normalizedVisualRequiredMode = NormalizeVisualRequiredMode(visualRequiredMode);
        if (normalizedVisualRequiredMode == "invalid")
        {
            Console.Error.WriteLine($"runtime release-gate: invalid --visual-required-mode '{visualRequiredMode}', expected auto | true | false.");
            return 1;
        }
        laneRepetitions = Math.Max(1, laneRepetitions);
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
                var coreEvalExit = await ExecuteEvaluateAsync(
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
                var coreGateExit = await ExecuteGateAsync(
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
                coreGateResult = EvaluateGateResult(
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
            var visualRequired = ResolveVisualRequired(
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
                var visualEvalExit = await ExecuteEvaluateAsync(
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
                var visualGateExit = await ExecuteGateAsync(
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
                visualGateResult = EvaluateGateResult(
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
            var chaosExit = await ExecuteEvaluateAsync(
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
            chaosGateResult = EvaluateGateResult(
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

        var sloEvidence = BuildRuntimeSloEvidence(
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

    internal async Task<int> ExecuteEvaluateAsync(
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

        var policies = ResolvePolicies(policiesCsv);
        if (policies.Length == 0)
        {
            RuntimeOutputWriter.WriteEvaluateResult(new RuntimeEvaluateResult(false, "No valid policies provided for evaluation."), json);
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
                var run = await ExecuteCoreAsync(
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

    private RuntimePlanContext BuildPlanContext(
        string goal,
        string repoRoot,
        string testFilter,
        string bootstrapProfile,
        string qaPolicy,
        string? runtimeManifestPath,
        string? runtimeManifestJson,
        int? maxIterationsOverride,
        bool useHistory,
        int historyWindow)
    {
        var manifest = AdaptiveRuntimeManifestLoader.Load(runtimeManifestPath, runtimeManifestJson);
        var requestedQaPolicy = NormalizeQaPolicy(qaPolicy);
        string? adaptivePolicyReason = null;
        var effectiveQaPolicy = requestedQaPolicy;

        if (useHistory && string.Equals(requestedQaPolicy, "auto", StringComparison.OrdinalIgnoreCase))
        {
            var history = AdaptiveRuntimeExecutionHistoryStore.ReadRecent(repoRoot, Math.Max(1, historyWindow));
            var recommendation = AdaptiveRuntimePolicyAdvisor.RecommendQaPolicy(goal, history);
            if (recommendation != null)
            {
                effectiveQaPolicy = recommendation.QaPolicy;
                adaptivePolicyReason = recommendation.Reason;
            }
        }

        var plan = AdaptiveRuntimePlanResolver.Resolve(goal, manifest, bootstrapProfile, effectiveQaPolicy);
        if (maxIterationsOverride.HasValue && maxIterationsOverride.Value > 0)
            plan = plan with { MaxIterations = maxIterationsOverride.Value };
        if (!string.IsNullOrWhiteSpace(adaptivePolicyReason))
            plan = plan with { Reasons = plan.Reasons.Concat([$"adaptive-policy: {adaptivePolicyReason}"]).ToArray() };

        var workflowSpec = AdaptiveRuntimePlanResolver.BuildRuntimeSpec(plan, testFilter);
        return new RuntimePlanContext(goal, manifest, plan, workflowSpec, requestedQaPolicy, adaptivePolicyReason);
    }

    private async Task<RuntimeExecuteResult> ExecuteCoreAsync(
        string goal,
        string repoRoot,
        string? provider,
        bool allowMock,
        bool runTests,
        string testFilter,
        string bootstrapProfile,
        string qaPolicy,
        string? runtimeManifestPath,
        string? runtimeManifestJson,
        int? maxIterationsOverride,
        bool bootstrapApply,
        bool bootstrapYes,
        bool bootstrapDryRun,
        bool runPreflight,
        bool useHistory,
        int historyWindow,
        bool persistHistory,
        string benchmarkSet,
        bool allowVisualCapabilityDegrade,
        CancellationToken ct)
    {
        var runId = Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;
        var timer = Stopwatch.StartNew();
        var goalFingerprint = AdaptiveRuntimeExecutionReport.ComputeGoalFingerprint(goal);
        var goalPreview = AdaptiveRuntimeExecutionReport.BuildGoalPreview(goal);

        RuntimeExecuteResult Finalize(RuntimeExecuteResult input)
        {
            var elapsed = timer.ElapsedMilliseconds;
            var output = input with
            {
                RunId = runId,
                StartedAtUtc = startedAt,
                ElapsedMs = elapsed,
                GoalFingerprint = goalFingerprint,
                GoalPreview = goalPreview
            };
            if (persistHistory)
            {
                try
                {
                    AdaptiveRuntimeExecutionHistoryStore.Append(
                        output.RepoRoot ?? repoRoot,
                        new AdaptiveRuntimeExecutionReport
                        {
                            RunId = output.RunId ?? runId,
                            StartedAtUtc = output.StartedAtUtc ?? startedAt,
                            ElapsedMs = output.ElapsedMs ?? elapsed,
                            GoalFingerprint = output.GoalFingerprint ?? goalFingerprint,
                            GoalPreview = output.GoalPreview ?? goalPreview,
                            BenchmarkSet = NormalizeBenchmarkSet(benchmarkSet),
                            RequestedQaPolicy = output.RequestedQaPolicy ?? "auto",
                            ResolvedQaPolicy = output.ResolvedQaPolicy ?? "demo",
                            BootstrapProfile = output.Plan?.BootstrapProfile ?? "self-extend-functional",
                            RunVisualQa = output.Plan?.RunVisualQa ?? false,
                            VisualQaFallbackPolicy = output.Plan?.VisualQaFallbackPolicy ?? "degrade",
                            Success = output.Ok,
                            FailureStage = output.FailureStage ?? "none",
                            BootstrapOk = output.BootstrapOk ?? false,
                            PreflightRan = output.PreflightRan ?? false,
                            PreflightOk = output.PreflightOk ?? false,
                            SelfExtendRan = output.SelfExtendRan ?? false,
                            SelfExtendOk = output.SelfExtendOk ?? false,
                            PlanReasons = output.Plan?.Reasons ?? Array.Empty<string>(),
                            AdaptivePolicyReason = output.AdaptivePolicyReason
                        });
                }
                catch
                {
                    // Persistence must never block runtime execution flow.
                }
            }

            return output;
        }

        if (string.IsNullOrWhiteSpace(goal))
            return Finalize(new RuntimeExecuteResult(false, "Goal is required.", FailureStage: "input"));

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
            return Finalize(new RuntimeExecuteResult(false, $"Repo root not found: {fullRepoRoot}", RepoRoot: fullRepoRoot, FailureStage: "input"));

        RuntimePlanContext context;
        try
        {
            context = BuildPlanContext(
                goal,
                fullRepoRoot,
                testFilter,
                bootstrapProfile,
                qaPolicy,
                runtimeManifestPath,
                runtimeManifestJson,
                maxIterationsOverride,
                useHistory,
                historyWindow);
        }
        catch (Exception ex)
        {
            return Finalize(new RuntimeExecuteResult(
                false,
                $"Failed to resolve runtime plan: {ex.Message}",
                RepoRoot: fullRepoRoot,
                FailureStage: "plan"));
        }

        context = await ApplyVisualCapabilityFallbackAsync(
            context,
            fullRepoRoot,
            testFilter,
            allowVisualCapabilityDegrade,
            ct).ConfigureAwait(false);

        var enrichedGoal = AdaptiveRuntimePlanResolver.EnrichGoal(goal, context.Manifest, context.Plan);

        var bootstrapBefore = await BootstrapRuntime.AssessDemoAsync(
            context.Plan.BootstrapProfile,
            includeOptional: false,
            ct,
            relaxStrictVisualHostDeps: allowMock).ConfigureAwait(false);
        BootstrapAssessment bootstrapAfter = bootstrapBefore;
        int? bootstrapApplyExit = null;
        var bootstrapApplied = false;
        if (bootstrapBefore.Supported && bootstrapBefore.MissingRequired.Any() && bootstrapApply)
        {
            bootstrapApplied = true;
            bootstrapApplyExit = await BootstrapCommand.RunApplyAsync(
                context.Plan.BootstrapProfile,
                includeOptional: false,
                yes: bootstrapYes,
                dryRun: bootstrapDryRun,
                json: false,
                ct).ConfigureAwait(false);
            bootstrapAfter = await BootstrapRuntime.AssessDemoAsync(
                context.Plan.BootstrapProfile,
                includeOptional: false,
                ct,
                relaxStrictVisualHostDeps: allowMock).ConfigureAwait(false);
        }

        var bootstrapOk =
            bootstrapAfter.Supported &&
            !bootstrapAfter.MissingRequired.Any() &&
            (!bootstrapApplyExit.HasValue || bootstrapApplyExit.Value == 0);
        if (!bootstrapOk)
        {
            var summary = !bootstrapAfter.Supported
                ? bootstrapAfter.Reason ?? "Bootstrap profile unsupported."
                : bootstrapAfter.MissingRequired.Any()
                    ? $"Missing required bootstrap dependencies: {string.Join(", ", bootstrapAfter.MissingRequired.Select(d => d.Id))}"
                    : $"Bootstrap apply failed (exit={bootstrapApplyExit}).";

            return Finalize(new RuntimeExecuteResult(
                false,
                $"runtime execute failed during bootstrap: {summary}",
                context.Plan,
                bootstrapBefore,
                bootstrapAfter,
                bootstrapApplied,
                bootstrapApplyExit,
                RequestedQaPolicy: context.RequestedQaPolicy,
                ResolvedQaPolicy: context.Plan.QaPolicyProfile,
                AdaptivePolicyReason: context.AdaptivePolicyReason,
                RepoRoot: fullRepoRoot,
                FailureStage: "bootstrap",
                BootstrapOk: false,
                PreflightRan: false,
                PreflightOk: false,
                SelfExtendRan: false,
                SelfExtendOk: false));
        }

        JsonElement? preflightPayload = null;
        RuntimeSubprocessResult? preflightRun = null;
        var preflightOkResult = true;
        if (runPreflight)
        {
            var preflightArgs = BuildSelfExtendPreflightArgs(
                fullRepoRoot,
                provider,
                allowMock,
                runTests,
                testFilter,
                JsonSerializer.Serialize(context.WorkflowSpec));
            preflightRun = await RunCliSubcommandAsync(fullRepoRoot, preflightArgs, ct).ConfigureAwait(false);
            preflightPayload = TryExtractLastJsonObject(preflightRun.StdOut);
            preflightOkResult = preflightRun.ExitCode == 0 && IsPayloadOk(preflightPayload);
            if (!preflightOkResult)
            {
                return Finalize(new RuntimeExecuteResult(
                    false,
                    "runtime execute failed during preflight.",
                    context.Plan,
                    bootstrapBefore,
                    bootstrapAfter,
                    bootstrapApplied,
                    bootstrapApplyExit,
                    preflightPayload,
                    preflightRun,
                    RequestedQaPolicy: context.RequestedQaPolicy,
                    ResolvedQaPolicy: context.Plan.QaPolicyProfile,
                    AdaptivePolicyReason: context.AdaptivePolicyReason,
                    RepoRoot: fullRepoRoot,
                    FailureStage: "preflight",
                    BootstrapOk: true,
                    PreflightRan: true,
                    PreflightOk: false,
                    SelfExtendRan: false,
                    SelfExtendOk: false));
            }
        }

        var runSpec = context.WorkflowSpec with
        {
            Workflow = context.WorkflowSpec.Workflow with { RequirePreflight = false }
        };
        var runArgs = BuildSelfExtendRunArgs(
            enrichedGoal,
            fullRepoRoot,
            provider,
            allowMock,
            runTests,
            testFilter,
            JsonSerializer.Serialize(runSpec));
        var selfExtendRun = await RunCliSubcommandAsync(fullRepoRoot, runArgs, ct).ConfigureAwait(false);
        var selfExtendPayload = TryExtractLastJsonObject(selfExtendRun.StdOut);
        var selfExtendOk = selfExtendRun.ExitCode == 0 && IsPayloadOk(selfExtendPayload);

        return Finalize(new RuntimeExecuteResult(
            selfExtendOk,
            selfExtendOk ? "runtime execute completed successfully." : "runtime execute failed during self-extend run.",
            context.Plan,
            bootstrapBefore,
            bootstrapAfter,
            bootstrapApplied,
            bootstrapApplyExit,
            preflightPayload,
            preflightRun,
            selfExtendPayload,
            selfExtendRun,
            context.RequestedQaPolicy,
            context.Plan.QaPolicyProfile,
            context.AdaptivePolicyReason,
            RepoRoot: fullRepoRoot,
            FailureStage: selfExtendOk ? "none" : "self-extend",
            BootstrapOk: true,
            PreflightRan: runPreflight,
            PreflightOk: !runPreflight || preflightOkResult,
            SelfExtendRan: true,
            SelfExtendOk: selfExtendOk));
    }

    private static async Task<RuntimePlanContext> ApplyVisualCapabilityFallbackAsync(
        RuntimePlanContext context,
        string repoRoot,
        string testFilter,
        bool allowVisualCapabilityDegrade,
        CancellationToken ct)
    {
        if (!allowVisualCapabilityDegrade ||
            !context.Plan.RunVisualQa ||
            !string.Equals(context.Plan.VisualQaFallbackPolicy, "strict", StringComparison.OrdinalIgnoreCase))
        {
            return context;
        }

        var capability = await AssessVisualInfraCapabilityAsync(repoRoot, ct).ConfigureAwait(false);
        if (capability.Ready)
            return context;

        var reason = $"runtime capability degrade applied: {capability.Summary}";
        var adjustedPlan = context.Plan with
        {
            VisualQaFallbackPolicy = "degrade",
            Reasons = context.Plan.Reasons.Concat([reason]).ToArray()
        };
        var adjustedWorkflow = AdaptiveRuntimePlanResolver.BuildRuntimeSpec(adjustedPlan, testFilter);
        return context with
        {
            Plan = adjustedPlan,
            WorkflowSpec = adjustedWorkflow
        };
    }

    private static async Task<RuntimeVisualInfraCapability> AssessVisualInfraCapabilityAsync(string repoRoot, CancellationToken ct)
    {
        var dockerCli = await RunShellProbeAsync(repoRoot, "command -v docker", ct).ConfigureAwait(false);
        if (dockerCli.ExitCode != 0)
            return new RuntimeVisualInfraCapability(false, "Visual QA infra missing docker CLI.");

        var dockerDaemon = await RunShellProbeAsync(repoRoot, "docker info > /dev/null 2>&1", ct).ConfigureAwait(false);
        if (dockerDaemon.ExitCode != 0)
            return new RuntimeVisualInfraCapability(false, "Visual QA infra missing usable Docker daemon.");

        var ollamaCli = await RunShellProbeAsync(repoRoot, "command -v ollama", ct).ConfigureAwait(false);
        if (ollamaCli.ExitCode != 0)
            return new RuntimeVisualInfraCapability(false, "Visual QA infra missing Ollama CLI.");

        return new RuntimeVisualInfraCapability(true, "Visual QA infra ready (docker + daemon + ollama).");
    }

    private static async Task<RuntimeSubprocessResult> RunShellProbeAsync(
        string workingDirectory,
        string script,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.ArgumentList.Add("-lc");
        psi.ArgumentList.Add(script);

        using var process = Process.Start(psi);
        if (process == null)
            return new RuntimeSubprocessResult(1, string.Empty, $"Failed to start shell probe: {script}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        return new RuntimeSubprocessResult(process.ExitCode, stdout, stderr);
    }

    private static string[] BuildSelfExtendPreflightArgs(
        string repoRoot,
        string? provider,
        bool allowMock,
        bool runTests,
        string testFilter,
        string runtimeSpecJson)
    {
        var args = new List<string>
        {
            "self-extend",
            "preflight",
            "--repo-root", repoRoot,
            "--runtime-spec-json", runtimeSpecJson,
            "--json"
        };
        if (!string.IsNullOrWhiteSpace(provider))
        {
            args.Add("--provider");
            args.Add(provider.Trim());
        }
        if (allowMock)
            args.Add("--allow-mock");
        if (runTests)
        {
            args.Add("--run-tests");
            args.Add("--test-filter");
            args.Add(testFilter);
        }
        return args.ToArray();
    }

    private static string[] BuildSelfExtendRunArgs(
        string goal,
        string repoRoot,
        string? provider,
        bool allowMock,
        bool runTests,
        string testFilter,
        string runtimeSpecJson)
    {
        var args = new List<string>
        {
            "self-extend",
            "run",
            "--goal", goal,
            "--repo-root", repoRoot,
            "--runtime-spec-json", runtimeSpecJson,
            "--preflight", "false",
            "--json"
        };
        if (!string.IsNullOrWhiteSpace(provider))
        {
            args.Add("--provider");
            args.Add(provider.Trim());
        }
        if (allowMock)
            args.Add("--allow-mock");
        if (runTests)
        {
            args.Add("--run-tests");
            args.Add("--test-filter");
            args.Add(testFilter);
        }
        return args.ToArray();
    }

    private static async Task<RuntimeSubprocessResult> RunCliSubcommandAsync(
        string workingDirectory,
        IReadOnlyList<string> args,
        CancellationToken ct)
    {
        var (fileName, prefixArgs) = ResolveInvoker();
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var prefix in prefixArgs)
            psi.ArgumentList.Add(prefix);
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);
        if (process == null)
            return new RuntimeSubprocessResult(1, string.Empty, "Failed to start runtime subprocess.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        return new RuntimeSubprocessResult(process.ExitCode, stdout, stderr);
    }

    private static (string FileName, string[] PrefixArgs) ResolveInvoker()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath) &&
            !string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return (processPath, Array.Empty<string>());
        }

        var entry = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(entry) && File.Exists(entry))
            return ("dotnet", new[] { entry });

        return ("dotnet", new[] { "run", "--project", "application/src/Nexo.CLI", "--" });
    }

    private static JsonElement? TryExtractLastJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var idx = text.LastIndexOf('{');
        while (idx >= 0)
        {
            var candidate = text[idx..].Trim();
            try
            {
                using var doc = JsonDocument.Parse(candidate);
                return doc.RootElement.Clone();
            }
            catch
            {
                idx = idx > 0 ? text.LastIndexOf('{', idx - 1) : -1;
            }
        }

        return null;
    }

    private static bool IsPayloadOk(JsonElement? payload)
    {
        if (!payload.HasValue || payload.Value.ValueKind != JsonValueKind.Object)
            return false;
        if (!payload.Value.TryGetProperty("ok", out var okNode))
            return false;
        return okNode.ValueKind == JsonValueKind.True;
    }

    private static RuntimeRemediationPolicy? ChooseRemediationPolicy(RuntimeExecuteResult failed)
    {
        if (failed.Ok || failed.Plan == null)
            return null;

        var stage = (failed.FailureStage ?? string.Empty).Trim().ToLowerInvariant();
        if (stage == "preflight" &&
            failed.Plan.RunVisualQa &&
            string.Equals(failed.Plan.VisualQaFallbackPolicy, "strict", StringComparison.OrdinalIgnoreCase))
        {
            return new RuntimeRemediationPolicy(
                "demo",
                "Switch to demo policy to allow visual degrade fallback after strict visual preflight failure.");
        }

        if (stage == "self-extend" &&
            !string.Equals(failed.ResolvedQaPolicy, "research", StringComparison.OrdinalIgnoreCase))
        {
            return new RuntimeRemediationPolicy(
                "research",
                "Escalate to research policy after self-extend failure to increase retry depth.");
        }

        return null;
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

    private static string[] ResolvePolicies(string csv)
    {
        return (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeQaPolicy)
            .Where(p => p is "demo" or "release" or "prod" or "research" or "auto")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static RuntimeGateResult EvaluateGateResult(
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
            var p = NormalizeQaPolicy(policy);
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

    private static bool ResolveVisualRequired(
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

    private static RuntimeSloEvidence BuildRuntimeSloEvidence(
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

    private static double ComputeP95(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return 0d;
        var ordered = values.OrderBy(v => v).ToArray();
        var index = (int)Math.Ceiling(0.95d * ordered.Length) - 1;
        index = Math.Clamp(index, 0, ordered.Length - 1);
        return ordered[index];
    }

    private static string NormalizeReleaseGateMode(string? mode)
    {
        var normalized = (mode ?? "full").Trim().ToLowerInvariant();
        return normalized is "core" or "visual" or "chaos" or "full" ? normalized : "invalid";
    }

    private static string NormalizeVisualRequiredMode(string? mode)
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

    private static string ReadEnvString(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static int ReadEnvInt(string key, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (int.TryParse(value, out var parsed))
            return parsed;
        return fallback;
    }

    private static double ReadEnvDouble(string key, double fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return fallback;
    }

    private static string NormalizeQaPolicy(string? qaPolicy)
    {
        var normalized = (qaPolicy ?? "auto").Trim().ToLowerInvariant();
        return normalized switch
        {
            "demo" => "demo",
            "release" => "release",
            "prod" => "prod",
            "research" => "research",
            _ => "auto"
        };
    }

    private static string NormalizeBenchmarkSet(string? benchmarkSet)
    {
        var normalized = (benchmarkSet ?? "adhoc").Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "adhoc" : normalized;
    }

}
