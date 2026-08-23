using System.CommandLine;
using System.CommandLine.Invocation;

namespace Ashlar.CLI.Commands;

/// <summary>CLI command for workflow.</summary>
public sealed partial class WorkflowCommand
{
    private void ConfigureScaffoldCommand()
    {
        var scaffold = new Command("scaffold", "Write a workflow lab runtime spec template.");
        var outputOpt = new Option<string>(
            "--output",
            () => Path.Combine(Environment.CurrentDirectory, ".ashlar", "workflow", "workflow_lab.runtime.json"),
            "Destination path for scaffolded workflow lab runtime spec.");
        var forceOpt = new Option<bool>("--force", () => false, "Overwrite output if it already exists.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        scaffold.AddOption(outputOpt);
        scaffold.AddOption(forceOpt);
        scaffold.AddOption(jsonOpt);
        scaffold.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteScaffoldAsync(
                ctx.ParseResult.GetValueForOption(outputOpt) ?? string.Empty,
                ctx.ParseResult.GetValueForOption(forceOpt),
                ctx.ParseResult.GetValueForOption(jsonOpt)).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        /// <summary>Add command.</summary>
        AddCommand(scaffold);
    }

    private void ConfigureStressCommand()
    {
        var stress = new Command("stress", "Execute a workflow composition/model stress matrix.");
        var requestOverrideOpt = new Option<string?>(
            "--request",
            () => null,
            "Optional request override used for all scenarios.");
        var specPathOpt = new Option<string?>(
            "--spec",
            () => null,
            "Path to workflow lab runtime spec JSON (defaults to .ashlar/workflow/workflow_lab.runtime.json).");
        var specJsonOpt = new Option<string?>("--spec-json", () => null, "Inline workflow lab runtime spec JSON.");
        var providerOpt = new Option<string?>(
            "--provider",
            () => null,
            "Override provider for all profile entries unless explicitly set.");
        var preferOpt = new Option<string?>(
            "--prefer",
            () => null,
            "Override model preference for all profile entries (agentic|deterministic|auto).");
        var iterationsOverrideOpt = new Option<int?>(
            "--iterations",
            () => null,
            "Override iteration count from execution spec.");
        var benchmarkSetOpt = new Option<string?>(
            "--benchmark-set",
            () => null,
            "Benchmark set tag to persist in workflow lab history.");
        var persistHistoryOpt = new Option<bool?>(
            "--persist-history",
            () => null,
            "Persist workflow lab results in JSONL history.");
        var warmupRunsOpt = new Option<int?>(
            "--warmup-runs",
            () => null,
            "Override warmup runs per scenario group (defaults to execution.warmupRuns).");
        var shuffleScenariosOpt = new Option<bool?>(
            "--shuffle-scenarios",
            () => null,
            "Override scenario shuffle behavior (defaults to execution.shuffleScenarios).");
        var randomSeedOpt = new Option<int?>(
            "--random-seed",
            () => null,
            "Override scenario shuffle seed (defaults to execution.randomSeed).");
        var cooldownMsOpt = new Option<int?>(
            "--cooldown-ms",
            () => null,
            "Override cooldown delay between scenario executions in milliseconds.");
        var includeMeshPeersOpt = new Option<bool>(
            "--include-mesh-peers",
            () => true,
            "Treat discovered mesh peers as stress execution participants.");
        var meshCapabilityOpt = new Option<string>(
            "--mesh-capability",
            () => "ashlar-cli",
            "Capability tag required for discovered mesh peers to participate.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        var verboseOpt = new Option<bool>("--verbose", () => false, "Emit orchestrator progress output.");

        stress.AddOption(requestOverrideOpt);
        stress.AddOption(specPathOpt);
        stress.AddOption(specJsonOpt);
        stress.AddOption(providerOpt);
        stress.AddOption(preferOpt);
        stress.AddOption(iterationsOverrideOpt);
        stress.AddOption(benchmarkSetOpt);
        stress.AddOption(persistHistoryOpt);
        stress.AddOption(warmupRunsOpt);
        stress.AddOption(shuffleScenariosOpt);
        stress.AddOption(randomSeedOpt);
        stress.AddOption(cooldownMsOpt);
        stress.AddOption(includeMeshPeersOpt);
        stress.AddOption(meshCapabilityOpt);
        stress.AddOption(jsonOpt);
        stress.AddOption(verboseOpt);
        stress.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteStressAsync(
                ctx.ParseResult.GetValueForOption(requestOverrideOpt),
                ctx.ParseResult.GetValueForOption(specPathOpt),
                ctx.ParseResult.GetValueForOption(specJsonOpt),
                ctx.ParseResult.GetValueForOption(providerOpt),
                ctx.ParseResult.GetValueForOption(preferOpt),
                ctx.ParseResult.GetValueForOption(iterationsOverrideOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(persistHistoryOpt),
                ctx.ParseResult.GetValueForOption(warmupRunsOpt),
                ctx.ParseResult.GetValueForOption(shuffleScenariosOpt),
                ctx.ParseResult.GetValueForOption(randomSeedOpt),
                ctx.ParseResult.GetValueForOption(cooldownMsOpt),
                ctx.ParseResult.GetValueForOption(includeMeshPeersOpt),
                ctx.ParseResult.GetValueForOption(meshCapabilityOpt),
                ctx.ParseResult.GetValueForOption(jsonOpt),
                ctx.ParseResult.GetValueForOption(verboseOpt),
                ctx.GetCancellationToken()).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        /// <summary>Add command.</summary>
        AddCommand(stress);
    }

    private void ConfigureHistoryCommand()
    {
        var history = new Command("history", "Show recent workflow stress runs.");
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var limitOpt = new Option<int>("--limit", () => 20, "Maximum history entries to return.");
        var benchmarkSetOpt = new Option<string?>("--benchmark-set", () => null, "Optional benchmark-set filter.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        history.AddOption(repoRootOpt);
        history.AddOption(limitOpt);
        history.AddOption(benchmarkSetOpt);
        history.AddOption(jsonOpt);
        history.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteHistoryAsync(
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(limitOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(jsonOpt)).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        /// <summary>Add command.</summary>
        AddCommand(history);
    }

    private void ConfigureReportCommand()
    {
        var report = new Command("report", "Generate benchmark report from workflow stress history.");
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var limitOpt = new Option<int>("--limit", () => 200, "Maximum history entries to analyze.");
        var benchmarkSetOpt = new Option<string?>("--benchmark-set", () => null, "Optional benchmark-set filter.");
        var runIdOpt = new Option<string?>("--run-id", () => null, "Optional run-id filter for a single benchmark session.");
        var baselineRunIdOpt = new Option<string?>("--baseline-run-id", () => null, "Optional baseline run-id for comparison against --run-id.");
        var sinceOpt = new Option<string?>("--since", () => null, "Optional ISO-8601 UTC timestamp filter (e.g. 2026-04-11T00:00:00Z).");
        var outputOpt = new Option<string?>("--output", () => null, "Optional report output file path (.json, .md, .txt).");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        report.AddOption(repoRootOpt);
        report.AddOption(limitOpt);
        report.AddOption(benchmarkSetOpt);
        report.AddOption(runIdOpt);
        report.AddOption(baselineRunIdOpt);
        report.AddOption(sinceOpt);
        report.AddOption(outputOpt);
        report.AddOption(jsonOpt);
        report.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteReportAsync(
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(limitOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(runIdOpt),
                ctx.ParseResult.GetValueForOption(baselineRunIdOpt),
                ctx.ParseResult.GetValueForOption(sinceOpt),
                ctx.ParseResult.GetValueForOption(outputOpt),
                ctx.ParseResult.GetValueForOption(jsonOpt)).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        /// <summary>Add command.</summary>
        AddCommand(report);
    }

    private void ConfigureGateCommand()
    {
        var gate = new Command("gate", "Evaluate a candidate run against a baseline run and fail on regression.");
        var repoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var benchmarkSetOpt = new Option<string?>("--benchmark-set", () => null, "Optional benchmark-set filter.");
        var runIdOpt = new Option<string>("--run-id", "Candidate run-id to evaluate.");
        var baselineRunIdOpt = new Option<string?>("--baseline-run-id", () => null, "Baseline run-id used for regression comparison. Defaults to active promoted baseline for benchmark-set.");
        var policyFileOpt = new Option<string?>("--policy-file", () => null, "Optional workflow gate policy JSON file.");
        var minSuccessRateDeltaOpt = new Option<double>("--min-success-rate-delta", () => -0.05, "Minimum allowed success-rate delta (candidate - baseline).");
        var maxP95LatencyRegressionMsOpt = new Option<long>("--max-p95-latency-regression-ms", () => 250, "Maximum allowed P95 latency regression in ms.");
        var maxAverageLatencyRegressionMsOpt = new Option<long>("--max-avg-latency-regression-ms", () => 150, "Maximum allowed average latency regression in ms.");
        var minAverageScoreDeltaOpt = new Option<double>("--min-average-score-delta", () => -5.0, "Minimum allowed average score delta (candidate - baseline).");
        var maxRegressedScenariosOpt = new Option<int>("--max-regressed-scenarios", () => 2, "Maximum allowed count of regressed scenario groups.");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");

        gate.AddOption(repoRootOpt);
        gate.AddOption(benchmarkSetOpt);
        gate.AddOption(runIdOpt);
        gate.AddOption(baselineRunIdOpt);
        gate.AddOption(policyFileOpt);
        gate.AddOption(minSuccessRateDeltaOpt);
        gate.AddOption(maxP95LatencyRegressionMsOpt);
        gate.AddOption(maxAverageLatencyRegressionMsOpt);
        gate.AddOption(minAverageScoreDeltaOpt);
        gate.AddOption(maxRegressedScenariosOpt);
        gate.AddOption(jsonOpt);
        gate.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteGateAsync(
                ctx.ParseResult.GetValueForOption(repoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(runIdOpt) ?? string.Empty,
                ctx.ParseResult.GetValueForOption(baselineRunIdOpt),
                ctx.ParseResult.GetValueForOption(policyFileOpt),
                ctx.ParseResult.GetValueForOption(minSuccessRateDeltaOpt),
                ctx.ParseResult.GetValueForOption(maxP95LatencyRegressionMsOpt),
                ctx.ParseResult.GetValueForOption(maxAverageLatencyRegressionMsOpt),
                ctx.ParseResult.GetValueForOption(minAverageScoreDeltaOpt),
                ctx.ParseResult.GetValueForOption(maxRegressedScenariosOpt),
                ctx.ParseResult.GetValueForOption(jsonOpt)).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        /// <summary>Add command.</summary>
        AddCommand(gate);
    }

    private void ConfigureBaselineCommand()
    {
        var baseline = new Command("baseline", "Manage promoted workflow benchmark baselines.");

        var promote = new Command("promote", "Promote a stress run-id to active baseline.");
        var promoteRepoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var promoteBenchmarkSetOpt = new Option<string?>("--benchmark-set", () => null, "Optional benchmark-set override.");
        var promoteRunIdOpt = new Option<string>("--run-id", "Run-id to promote.");
        var promoteNotesOpt = new Option<string?>("--notes", () => null, "Optional notes for baseline promotion.");
        var promotePolicyFileOpt = new Option<string?>("--policy-file", () => null, "Optional gate policy JSON to snapshot with promoted baseline.");
        var promoteJsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        promote.AddOption(promoteRepoRootOpt);
        promote.AddOption(promoteBenchmarkSetOpt);
        promote.AddOption(promoteRunIdOpt);
        promote.AddOption(promoteNotesOpt);
        promote.AddOption(promotePolicyFileOpt);
        promote.AddOption(promoteJsonOpt);
        promote.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteBaselinePromoteAsync(
                ctx.ParseResult.GetValueForOption(promoteRepoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(promoteBenchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(promoteRunIdOpt) ?? string.Empty,
                ctx.ParseResult.GetValueForOption(promoteNotesOpt),
                ctx.ParseResult.GetValueForOption(promotePolicyFileOpt),
                ctx.ParseResult.GetValueForOption(promoteJsonOpt)).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        baseline.AddCommand(promote);

        var list = new Command("list", "List promoted workflow baselines.");
        var listRepoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var listBenchmarkSetOpt = new Option<string?>("--benchmark-set", () => null, "Optional benchmark-set filter.");
        var listJsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        list.AddOption(listRepoRootOpt);
        list.AddOption(listBenchmarkSetOpt);
        list.AddOption(listJsonOpt);
        list.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteBaselineListAsync(
                ctx.ParseResult.GetValueForOption(listRepoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(listBenchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(listJsonOpt)).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        baseline.AddCommand(list);

        var show = new Command("show", "Show baseline by id or active baseline for benchmark-set.");
        var showRepoRootOpt = new Option<string>("--repo-root", () => Environment.CurrentDirectory, "Repository root path.");
        var showBenchmarkSetOpt = new Option<string?>("--benchmark-set", () => "workflow-lab", "Benchmark-set used when selecting active baseline.");
        var showBaselineIdOpt = new Option<string?>("--baseline-id", () => null, "Specific baseline id to display.");
        var showJsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        show.AddOption(showRepoRootOpt);
        show.AddOption(showBenchmarkSetOpt);
        show.AddOption(showBaselineIdOpt);
        show.AddOption(showJsonOpt);
        show.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteBaselineShowAsync(
                ctx.ParseResult.GetValueForOption(showRepoRootOpt) ?? Environment.CurrentDirectory,
                ctx.ParseResult.GetValueForOption(showBenchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(showBaselineIdOpt),
                ctx.ParseResult.GetValueForOption(showJsonOpt)).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        baseline.AddCommand(show);

        /// <summary>Add command.</summary>
        AddCommand(baseline);
    }

    private void ConfigureOptimizeCommand()
    {
        var optimize = new Command("optimize", "Auto-select workflow composition/model combinations, stress-test, and emit recommendations.");
        var requestOverrideOpt = new Option<string?>(
            "--request",
            () => null,
            "Optional request override used for all optimize candidates.");
        var objectiveOpt = new Option<string?>(
            "--objective",
            () => null,
            "Optional high-level objective used to prioritize candidate generation.");
        var objectiveFileOpt = new Option<string?>(
            "--objective-file",
            () => null,
            "Optional file path containing objective text used to prioritize candidate generation.");
        var specPathOpt = new Option<string?>(
            "--spec",
            () => null,
            "Path to workflow lab runtime spec JSON (defaults to .ashlar/workflow/workflow_lab.runtime.json).");
        var specJsonOpt = new Option<string?>("--spec-json", () => null, "Inline workflow lab runtime spec JSON.");
        var providerOpt = new Option<string?>(
            "--provider",
            () => null,
            "Override provider for all profile entries unless explicitly set.");
        var preferOpt = new Option<string?>(
            "--prefer",
            () => null,
            "Override model preference for all profile entries (agentic|deterministic|auto).");
        var iterationsOverrideOpt = new Option<int?>(
            "--iterations",
            () => null,
            "Override iteration count from execution spec.");
        var benchmarkSetOpt = new Option<string?>(
            "--benchmark-set",
            () => null,
            "Benchmark set tag used for optimize runs.");
        var persistHistoryOpt = new Option<bool?>(
            "--persist-history",
            () => null,
            "Persist optimize-run history entries in JSONL history.");
        var warmupRunsOpt = new Option<int?>(
            "--warmup-runs",
            () => null,
            "Override warmup runs per candidate (defaults to execution.warmupRuns).");
        var shuffleScenariosOpt = new Option<bool?>(
            "--shuffle-scenarios",
            () => null,
            "Override candidate order shuffle behavior (defaults to execution.shuffleScenarios).");
        var randomSeedOpt = new Option<int?>(
            "--random-seed",
            () => null,
            "Override candidate shuffle seed (defaults to execution.randomSeed).");
        var cooldownMsOpt = new Option<int?>(
            "--cooldown-ms",
            () => null,
            "Override cooldown delay between candidate runs in milliseconds.");
        var maxCandidatesOpt = new Option<int>(
            "--max-candidates",
            () => 24,
            "Maximum request/composition/profile candidates to evaluate.");
        var budgetRunsOpt = new Option<int?>(
            "--budget-runs",
            () => null,
            "Optional maximum measured runs budget across all optimize candidates.");
        var searchStrategyOpt = new Option<string>(
            "--search-strategy",
            () => "successive-halving",
            "Candidate search strategy (successive-halving|objective-first|exhaustive).");
        var earlyStopMinRunsOpt = new Option<int?>(
            "--early-stop-min-runs",
            () => 2,
            "Minimum measured runs before early-stop checks trigger.");
        var earlyStopMinSuccessRateOpt = new Option<double?>(
            "--early-stop-min-success-rate",
            () => 0.35,
            "Minimum measured success-rate required after early-stop minimum runs.");
        var includeMeshPeersOpt = new Option<bool>(
            "--include-mesh-peers",
            () => true,
            "Treat discovered mesh peers as optimize execution participants.");
        var meshCapabilityOpt = new Option<string>(
            "--mesh-capability",
            () => "ashlar-cli",
            "Capability tag required for discovered mesh peers to participate.");
        var autoPullModelsOpt = new Option<bool>(
            "--auto-pull-models",
            () => true,
            "Automatically pull required Ollama models before candidate execution.");
        var promoteWinnerOpt = new Option<bool>(
            "--promote-winner",
            () => true,
            "Auto-promote winner candidate as active baseline when policy checks pass.");
        var policyFileOpt = new Option<string?>(
            "--policy-file",
            () => null,
            "Optional workflow gate policy JSON used for winner promotion checks.");
        var reportOutputOpt = new Option<string?>(
            "--report-output",
            () => null,
            "Optional recommendation report output path (.md, .txt, .json).");
        var jsonOpt = new Option<bool>("--json", () => false, "Emit machine-readable JSON output.");
        var verboseOpt = new Option<bool>("--verbose", () => false, "Emit orchestrator progress output.");

        optimize.AddOption(requestOverrideOpt);
        optimize.AddOption(objectiveOpt);
        optimize.AddOption(objectiveFileOpt);
        optimize.AddOption(specPathOpt);
        optimize.AddOption(specJsonOpt);
        optimize.AddOption(providerOpt);
        optimize.AddOption(preferOpt);
        optimize.AddOption(iterationsOverrideOpt);
        optimize.AddOption(benchmarkSetOpt);
        optimize.AddOption(persistHistoryOpt);
        optimize.AddOption(warmupRunsOpt);
        optimize.AddOption(shuffleScenariosOpt);
        optimize.AddOption(randomSeedOpt);
        optimize.AddOption(cooldownMsOpt);
        optimize.AddOption(maxCandidatesOpt);
        optimize.AddOption(budgetRunsOpt);
        optimize.AddOption(searchStrategyOpt);
        optimize.AddOption(earlyStopMinRunsOpt);
        optimize.AddOption(earlyStopMinSuccessRateOpt);
        optimize.AddOption(includeMeshPeersOpt);
        optimize.AddOption(meshCapabilityOpt);
        optimize.AddOption(autoPullModelsOpt);
        optimize.AddOption(promoteWinnerOpt);
        optimize.AddOption(policyFileOpt);
        optimize.AddOption(reportOutputOpt);
        optimize.AddOption(jsonOpt);
        optimize.AddOption(verboseOpt);
        optimize.SetHandler((InvocationContext ctx) =>
        {
            var exitCode = ExecuteOptimizeAsync(
                ctx.ParseResult.GetValueForOption(requestOverrideOpt),
                ctx.ParseResult.GetValueForOption(objectiveOpt),
                ctx.ParseResult.GetValueForOption(objectiveFileOpt),
                ctx.ParseResult.GetValueForOption(specPathOpt),
                ctx.ParseResult.GetValueForOption(specJsonOpt),
                ctx.ParseResult.GetValueForOption(providerOpt),
                ctx.ParseResult.GetValueForOption(preferOpt),
                ctx.ParseResult.GetValueForOption(iterationsOverrideOpt),
                ctx.ParseResult.GetValueForOption(benchmarkSetOpt),
                ctx.ParseResult.GetValueForOption(persistHistoryOpt),
                ctx.ParseResult.GetValueForOption(warmupRunsOpt),
                ctx.ParseResult.GetValueForOption(shuffleScenariosOpt),
                ctx.ParseResult.GetValueForOption(randomSeedOpt),
                ctx.ParseResult.GetValueForOption(cooldownMsOpt),
                ctx.ParseResult.GetValueForOption(maxCandidatesOpt),
                ctx.ParseResult.GetValueForOption(budgetRunsOpt),
                ctx.ParseResult.GetValueForOption(searchStrategyOpt),
                ctx.ParseResult.GetValueForOption(earlyStopMinRunsOpt),
                ctx.ParseResult.GetValueForOption(earlyStopMinSuccessRateOpt),
                ctx.ParseResult.GetValueForOption(includeMeshPeersOpt),
                ctx.ParseResult.GetValueForOption(meshCapabilityOpt),
                ctx.ParseResult.GetValueForOption(autoPullModelsOpt),
                ctx.ParseResult.GetValueForOption(promoteWinnerOpt),
                ctx.ParseResult.GetValueForOption(policyFileOpt),
                ctx.ParseResult.GetValueForOption(reportOutputOpt),
                ctx.ParseResult.GetValueForOption(jsonOpt),
                ctx.ParseResult.GetValueForOption(verboseOpt),
                ctx.GetCancellationToken()).GetAwaiter().GetResult();
            ctx.ExitCode = exitCode;
        });
        /// <summary>Add command.</summary>
        AddCommand(optimize);
    }

}
