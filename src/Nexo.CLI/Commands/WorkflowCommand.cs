using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.CLI.Runtime;
using Nexo.Infrastructure.Execution;
using Nexo.Orchestration.Models;
using Process = System.Diagnostics.Process;

namespace Nexo.CLI.Commands;

/// <summary>
/// First-class workflow lab command for composing and stress-testing orchestrated agent workflows.
/// </summary>
public sealed class WorkflowCommand : Command
{
    internal delegate Task<ScenarioExecutionResult> ScenarioExecutor(
        string request,
        string runtimeSpecJson,
        string? provider,
        bool outputJson,
        bool verbose,
        CancellationToken cancellationToken);
    internal delegate Task<ScenarioExecutionResult> MeshScenarioExecutor(
        string endpoint,
        string request,
        string runtimeSpecJson,
        string? provider,
        bool outputJson,
        bool verbose,
        CancellationToken cancellationToken);

    private readonly ScenarioExecutor _scenarioExecutor;
    private readonly MeshScenarioExecutor _meshScenarioExecutor;
    private readonly Func<string, CancellationToken, Task<PreflightResult>> _providerPreflight;
    private readonly Func<IReadOnlyList<string>, CancellationToken, Task<ModelPullResult>> _ollamaModelPuller;
    private readonly Func<CancellationToken, Task<IReadOnlyList<PeerInfo>>> _meshPeerDiscovery;

    public WorkflowCommand(Func<OrchestrateCommand> orchestrateFactory)
        : this(
            async (request, runtimeSpecJson, provider, outputJson, verbose, ct) =>
            {
                var orchestrate = orchestrateFactory();
                return await ExecuteScenarioAsync(orchestrate, request, runtimeSpecJson, provider, outputJson, verbose, ct).ConfigureAwait(false);
            },
            (provider, ct) => PreflightProviderAsync(provider, ct),
            null,
            _ => Task.FromResult<IReadOnlyList<PeerInfo>>(Array.Empty<PeerInfo>()),
            ExecuteScenarioOnMeshPeerAsync)
    {
    }

    public WorkflowCommand(Func<IServiceScope> orchestrationScopeFactory)
        : this(
            async (request, runtimeSpecJson, provider, outputJson, verbose, ct) =>
            {
                using var scope = orchestrationScopeFactory();
                var orchestrate = scope.ServiceProvider.GetRequiredService<OrchestrateCommand>();
                return await ExecuteScenarioAsync(orchestrate, request, runtimeSpecJson, provider, outputJson, verbose, ct).ConfigureAwait(false);
            },
            async (provider, ct) =>
            {
                using var scope = orchestrationScopeFactory();
                return await PreflightProviderAsync(
                    provider,
                    ct,
                    scope.ServiceProvider.GetService<IProviderFactory>()).ConfigureAwait(false);
            },
            null,
            async ct =>
            {
                using var scope = orchestrationScopeFactory();
                var discovery = scope.ServiceProvider.GetService<IInstanceDiscovery>();
                if (discovery is null)
                    return Array.Empty<PeerInfo>();
                var peers = await discovery.DiscoverAsync(ct).ConfigureAwait(false);
                return peers ?? Array.Empty<PeerInfo>();
            },
            ExecuteScenarioOnMeshPeerAsync)
    {
    }

    internal WorkflowCommand(ScenarioExecutor scenarioExecutor)
        : this(
            scenarioExecutor,
            (provider, ct) => Task.FromResult(
                new PreflightResult(
                    Ok: true,
                    Provider: string.IsNullOrWhiteSpace(provider) ? "unset" : provider,
                    Detail: "Test preflight override: provider assumed available.")),
            (models, _) => Task.FromResult(
                new ModelPullResult(
                    Ok: true,
                    Summary: $"Test model pull override: assumed available for {models.Count} model(s).",
                    Models: models.ToArray(),
                    PulledModels: models.ToArray())),
            _ => Task.FromResult<IReadOnlyList<PeerInfo>>(Array.Empty<PeerInfo>()),
            ExecuteScenarioOnMeshPeerAsync)
    {
    }

    internal WorkflowCommand(
        ScenarioExecutor scenarioExecutor,
        Func<string, CancellationToken, Task<bool>>? providerPreflight)
        : this(
            scenarioExecutor,
            providerPreflight,
            null,
            _ => Task.FromResult<IReadOnlyList<PeerInfo>>(Array.Empty<PeerInfo>()),
            ExecuteScenarioOnMeshPeerAsync)
    {
    }

    internal WorkflowCommand(
        ScenarioExecutor scenarioExecutor,
        Func<string, CancellationToken, Task<bool>>? providerPreflight,
        Func<IReadOnlyList<string>, CancellationToken, Task<ModelPullResult>>? ollamaModelPuller)
        : this(
            scenarioExecutor,
            providerPreflight,
            ollamaModelPuller,
            _ => Task.FromResult<IReadOnlyList<PeerInfo>>(Array.Empty<PeerInfo>()),
            ExecuteScenarioOnMeshPeerAsync)
    {
    }

    internal WorkflowCommand(
        ScenarioExecutor scenarioExecutor,
        Func<string, CancellationToken, Task<bool>>? providerPreflight,
        Func<IReadOnlyList<string>, CancellationToken, Task<ModelPullResult>>? ollamaModelPuller,
        Func<CancellationToken, Task<IReadOnlyList<PeerInfo>>>? meshPeerDiscovery,
        MeshScenarioExecutor? meshScenarioExecutor)
        : this(
            scenarioExecutor,
            providerPreflight is null
                ? (provider, ct) => Task.FromResult(
                    new PreflightResult(
                        Ok: true,
                        Provider: string.IsNullOrWhiteSpace(provider) ? "unset" : provider,
                        Detail: "Test preflight override: provider assumed available."))
                : async (provider, ct) =>
                {
                    var ok = await providerPreflight(provider, ct).ConfigureAwait(false);
                    return new PreflightResult(
                        Ok: ok,
                        Provider: string.IsNullOrWhiteSpace(provider) ? "unset" : provider.Trim(),
                        Detail: ok ? "Provider available." : "Provider unavailable.");
                },
            ollamaModelPuller,
            meshPeerDiscovery,
            meshScenarioExecutor)
    {
    }

    private WorkflowCommand(
        ScenarioExecutor scenarioExecutor,
        Func<string, CancellationToken, Task<PreflightResult>> providerPreflight,
        Func<IReadOnlyList<string>, CancellationToken, Task<ModelPullResult>>? ollamaModelPuller = null,
        Func<CancellationToken, Task<IReadOnlyList<PeerInfo>>>? meshPeerDiscovery = null,
        MeshScenarioExecutor? meshScenarioExecutor = null)
        : base("workflow", "Scaffold and stress-test agentic workflow compositions.")
    {
        _scenarioExecutor = scenarioExecutor ?? throw new ArgumentNullException(nameof(scenarioExecutor));
        _providerPreflight = providerPreflight ?? throw new ArgumentNullException(nameof(providerPreflight));
        _ollamaModelPuller = ollamaModelPuller ?? PullOllamaModelsAsync;
        _meshPeerDiscovery = meshPeerDiscovery ?? (_ => Task.FromResult<IReadOnlyList<PeerInfo>>(Array.Empty<PeerInfo>()));
        _meshScenarioExecutor = meshScenarioExecutor ?? ExecuteScenarioOnMeshPeerAsync;
        ConfigureScaffoldCommand();
        ConfigureStressCommand();
        ConfigureHistoryCommand();
        ConfigureReportCommand();
        ConfigureGateCommand();
        ConfigureBaselineCommand();
        ConfigureOptimizeCommand();
    }

    private void ConfigureScaffoldCommand()
    {
        var scaffold = new Command("scaffold", "Write a workflow lab runtime spec template.");
        var outputOpt = new Option<string>(
            "--output",
            () => Path.Combine(Environment.CurrentDirectory, ".nexo", "workflow", "workflow_lab.runtime.json"),
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
            "Path to workflow lab runtime spec JSON (defaults to .nexo/workflow/workflow_lab.runtime.json).");
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
            () => "nexo-cli",
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
            "Path to workflow lab runtime spec JSON (defaults to .nexo/workflow/workflow_lab.runtime.json).");
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
            () => "nexo-cli",
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
        AddCommand(optimize);
    }

    internal Task<int> ExecuteScaffoldAsync(string outputPath, bool force, bool json)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            WriteScaffoldResult(new WorkflowScaffoldResult(false, "Output path is required."), json);
            return Task.FromResult(1);
        }

        var fullPath = Path.GetFullPath(outputPath);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        if (File.Exists(fullPath) && !force)
        {
            WriteScaffoldResult(new WorkflowScaffoldResult(false, $"File already exists: {fullPath}. Use --force to overwrite.", fullPath), json);
            return Task.FromResult(1);
        }

        var scaffold = WorkflowLabRuntimeSpec.Default();
        var payload = JsonSerializer.Serialize(scaffold, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(fullPath, payload);
        WriteScaffoldResult(new WorkflowScaffoldResult(true, "Workflow lab spec scaffolded successfully.", fullPath), json);
        return Task.FromResult(0);
    }

    internal Task<int> ExecuteHistoryAsync(string repoRoot, int limit, string? benchmarkSet, bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteHistoryResult(new WorkflowHistoryResult(false, $"Repo root not found: {fullRepoRoot}"), json);
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

        WriteHistoryResult(new WorkflowHistoryResult(
            true,
            $"Loaded {total} workflow stress history entries.",
            rows,
            new WorkflowHistorySummary(total, successCount, total - successCount, best?.ScenarioId, best?.Score)), json);
        return Task.FromResult(0);
    }

    internal Task<int> ExecuteBaselinePromoteAsync(
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
            WriteBaselinePromoteResult(new WorkflowBaselinePromoteResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            WriteBaselinePromoteResult(new WorkflowBaselinePromoteResult(false, "Run-id is required for baseline promotion."), json);
            return Task.FromResult(1);
        }

        var history = WorkflowLabHistoryStore.ReadAll(fullRepoRoot);
        var candidateRows = history
            .Where(x => string.Equals(x.RunId, runId.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (candidateRows.Length == 0)
        {
            WriteBaselinePromoteResult(new WorkflowBaselinePromoteResult(false, $"No history found for run-id '{runId.Trim()}'."), json);
            return Task.FromResult(1);
        }

        var policyResult = LoadGatePolicy(policyFile);
        if (!policyResult.Ok)
        {
            WriteBaselinePromoteResult(new WorkflowBaselinePromoteResult(false, policyResult.Error ?? "Unable to parse policy file."), json);
            return Task.FromResult(1);
        }

        var latest = candidateRows.OrderByDescending(x => x.StartedAtUtc).First();
        var normalizedBenchmarkSet = NormalizeBenchmarkSet(
            benchmarkSet,
            string.IsNullOrWhiteSpace(latest.BenchmarkSet) ? "workflow-lab" : latest.BenchmarkSet);
        var promoted = WorkflowBaselineStore.Promote(fullRepoRoot, new WorkflowBaselineRecord
        {
            BaselineId = BuildBaselineId(normalizedBenchmarkSet, latest.RunId),
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

        WriteBaselinePromoteResult(
            new WorkflowBaselinePromoteResult(
                true,
                $"Promoted run-id {promoted.RunId} as active baseline for benchmark-set {promoted.BenchmarkSet}.",
                promoted),
            json);
        return Task.FromResult(0);
    }

    internal Task<int> ExecuteBaselineListAsync(string repoRoot, string? benchmarkSet, bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteBaselineListResult(new WorkflowBaselineListResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        var all = WorkflowBaselineStore.ReadAll(fullRepoRoot);
        var filtered = string.IsNullOrWhiteSpace(benchmarkSet)
            ? all
            : all.Where(x => string.Equals(x.BenchmarkSet, benchmarkSet.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
        WriteBaselineListResult(
            new WorkflowBaselineListResult(
                true,
                $"Loaded {filtered.Count} baseline record(s).",
                filtered),
            json);
        return Task.FromResult(0);
    }

    internal Task<int> ExecuteBaselineShowAsync(string repoRoot, string? benchmarkSet, string? baselineId, bool json)
    {
        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteBaselineShowResult(new WorkflowBaselineShowResult(false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        WorkflowBaselineRecord? selected = !string.IsNullOrWhiteSpace(baselineId)
            ? WorkflowBaselineStore.ReadById(fullRepoRoot, baselineId)
            : WorkflowBaselineStore.ReadActive(fullRepoRoot, NormalizeBenchmarkSet(benchmarkSet, "workflow-lab"));
        if (selected is null)
        {
            var missing = !string.IsNullOrWhiteSpace(baselineId)
                ? $"No baseline found for id '{baselineId.Trim()}'."
                : $"No active baseline found for benchmark-set '{NormalizeBenchmarkSet(benchmarkSet, "workflow-lab")}'.";
            WriteBaselineShowResult(new WorkflowBaselineShowResult(false, missing), json);
            return Task.FromResult(1);
        }

        WriteBaselineShowResult(new WorkflowBaselineShowResult(true, "Baseline loaded.", selected), json);
        return Task.FromResult(0);
    }

    internal Task<int> ExecuteReportAsync(
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
            WriteReportResult(new WorkflowReportResult(
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

        var comparison = BuildComparison(rows, runId, baselineRunId);
        if (comparison is { Valid: false })
        {
            WriteReportResult(new WorkflowReportResult(
                false,
                comparison.Summary ?? "Unable to build workflow run comparison.",
                BuildBenchmarkReport(Array.Empty<WorkflowLabStressHistoryRow>()),
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

        var report = BuildBenchmarkReport(filteredRows);
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

            var content = RenderReportContent(result, json, fullOutputPath);
            File.WriteAllText(fullOutputPath, content);
            result = result with { OutputPath = fullOutputPath };
        }

        WriteReportResult(result, json);
        return Task.FromResult(result.Ok ? 0 : 1);
    }

    internal Task<int> ExecuteGateAsync(
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
            WriteGateResult(new WorkflowGateResult(false, false, $"Repo root not found: {fullRepoRoot}"), json);
            return Task.FromResult(1);
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            WriteGateResult(new WorkflowGateResult(false, false, "--run-id is required."), json);
            return Task.FromResult(1);
        }

        var policyResult = LoadGatePolicy(policyFile);
        if (!policyResult.Ok)
        {
            WriteGateResult(new WorkflowGateResult(false, false, policyResult.Error ?? "Unable to parse policy file."), json);
            return Task.FromResult(1);
        }

        var normalizedBenchmarkSet = NormalizeBenchmarkSet(
            benchmarkSet,
            string.IsNullOrWhiteSpace(policyResult.Policy?.BenchmarkSet) ? "workflow-lab" : policyResult.Policy!.BenchmarkSet!);
        var resolvedBaselineRunId = string.IsNullOrWhiteSpace(baselineRunId)
            ? WorkflowBaselineStore.ReadActive(fullRepoRoot, normalizedBenchmarkSet)?.RunId
            : baselineRunId.Trim();
        if (string.IsNullOrWhiteSpace(resolvedBaselineRunId))
        {
            WriteGateResult(new WorkflowGateResult(
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

        var comparison = BuildComparison(rows, runId, resolvedBaselineRunId);
        if (!comparison.Valid)
        {
            WriteGateResult(new WorkflowGateResult(false, false, comparison.Summary ?? "Failed to build comparison.", Comparison: comparison), json);
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
        WriteGateResult(result, json);
        return Task.FromResult(passed ? 0 : 1);
    }

    internal async Task<int> ExecuteStressAsync(
        string? requestOverride,
        string? specPath,
        string? specJson,
        string? providerOverride,
        string? preferOverride,
        int? iterationsOverride,
        string? benchmarkSetOverride,
        bool? persistHistoryOverride,
        int? warmupRunsOverride,
        bool? shuffleScenariosOverride,
        int? randomSeedOverride,
        int? cooldownMsOverride,
        bool includeMeshPeers,
        string? meshCapability,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        var resolvedSpecPath = ResolveDefaultSpecPath(specPath);
        WorkflowLabRuntimeSpec spec;
        try
        {
            spec = WorkflowLabRuntimeSpecLoader.Load(resolvedSpecPath, specJson);
        }
        catch (Exception ex)
        {
            WriteStressResult(new WorkflowStressResult(false, $"Failed to load workflow lab spec: {ex.Message}"), json);
            return 1;
        }

        var repoRoot = Environment.CurrentDirectory;
        var requests = NormalizeRequests(spec.Requests);
        var compositions = NormalizeCompositions(spec.Compositions);
        var profiles = NormalizeProfiles(spec.ModelProfiles, providerOverride, preferOverride);
        if (requests.Length == 0 || compositions.Length == 0 || profiles.Length == 0)
        {
            WriteStressResult(new WorkflowStressResult(
                false,
                "Workflow stress spec must include at least one request, composition, and model profile."), json);
            return 1;
        }

        var benchmarkSet = NormalizeBenchmarkSet(benchmarkSetOverride, spec.Execution.BenchmarkSet);
        var persistHistory = persistHistoryOverride ?? spec.Execution.PersistHistory;
        var iterations = Math.Max(1, iterationsOverride ?? spec.Execution.Iterations);
        var sharedRequest = string.IsNullOrWhiteSpace(requestOverride) ? null : requestOverride.Trim();
        var runId = BuildRunId();
        var specHash = ComputeSpecHash(JsonSerializer.Serialize(spec));
        var gitSha = ResolveGitSha();
        var providerSnapshot = BuildProviderSnapshot(profiles);
        var warmupRuns = Math.Max(0, warmupRunsOverride ?? spec.Execution.WarmupRuns);
        var cooldownMs = Math.Max(0, cooldownMsOverride ?? spec.Execution.CooldownMs);
        var shuffleScenarios = shuffleScenariosOverride ?? spec.Execution.ShuffleScenarioOrder;
        var randomSeed = randomSeedOverride ?? spec.Execution.RandomSeed;
        var rng = randomSeed.HasValue ? new Random(randomSeed.Value) : null;

        var preflightByProvider = new Dictionary<string, PreflightResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in profiles
                     .Select(x => x.Default.Provider)
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Select(x => x!.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            preflightByProvider[provider] = await _providerPreflight(provider, ct).ConfigureAwait(false);
        }

        var executionTargets = await ResolveExecutionTargetsAsync(
            includeMeshPeers,
            meshCapability,
            ct).ConfigureAwait(false);

        var scenarioPlans = BuildScenarioPlans(requests, compositions, profiles, iterations);
        if (shuffleScenarios && scenarioPlans.Count > 1)
            ShuffleScenarioPlans(scenarioPlans, rng ?? new Random());

        var runs = new List<WorkflowStressRunRecord>();
        var executionTargetCursor = 0;
        foreach (var plan in scenarioPlans)
        {
            var request = plan.Request;
            var composition = plan.Composition;
            var profile = plan.Profile;
            var i = plan.Iteration;
            var executionTarget = executionTargets[executionTargetCursor % executionTargets.Count];
            executionTargetCursor++;
            var scenarioId = BuildScenarioId(request.Id, composition.Id, profile.Id, i) +
                             $"::target-{NormalizeScenarioTargetSegment(executionTarget.Id)}";
            var runtime = BuildRuntimeSpec(composition, profile);
            var runtimeJson = JsonSerializer.Serialize(runtime);
            var runtimeExecutionRequest = BuildExecutionRequest(request, composition, profile, sharedRequest);
            var profileProvider = profile.Default.Provider?.Trim();

            for (var warmup = 0; warmup < warmupRuns; warmup++)
            {
                ct.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(profileProvider) &&
                    preflightByProvider.TryGetValue(profileProvider, out var warmupPreflight) &&
                    !warmupPreflight.Ok)
                {
                    break;
                }

                try
                {
                    _ = await ExecuteScenarioForTargetAsync(
                        executionTarget,
                        runtimeExecutionRequest,
                        runtimeJson,
                        profile.Default.Provider,
                        verbose,
                        ct).ConfigureAwait(false);
                }
                catch
                {
                    // Warmups are intentionally ignored in persisted metrics.
                }
            }

            ct.ThrowIfCancellationRequested();
            var startedAt = DateTimeOffset.UtcNow;
            var cpuStart = Process.GetCurrentProcess().TotalProcessorTime;
            var sw = Stopwatch.StartNew();
            ScenarioExecutionResult scenario;
            if (!string.IsNullOrWhiteSpace(profileProvider) &&
                preflightByProvider.TryGetValue(profileProvider, out var preflight) &&
                !preflight.Ok)
            {
                scenario = new ScenarioExecutionResult(
                    Ok: false,
                    Summary: $"Skipped due to provider preflight failure ({profileProvider}): {preflight.Detail}",
                    ConflictCount: 0,
                    EscalationCount: 0,
                    FailureCategory: "skipped_infra",
                    Skipped: true);
            }
            else
            {
                try
                {
                    scenario = await ExecuteScenarioForTargetAsync(
                        executionTarget,
                        runtimeExecutionRequest,
                        runtimeJson,
                        profile.Default.Provider,
                        verbose,
                        ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scenario = new ScenarioExecutionResult(
                        Ok: false,
                        Summary: $"Scenario executor failed: {ex.Message}",
                        ConflictCount: 0,
                        EscalationCount: 0,
                        FailureCategory: "executor_failure");
                }
            }
            var elapsedMs = sw.ElapsedMilliseconds;
            var telemetry = CaptureRuntimeTelemetry(startedAt, cpuStart);
            var score = ComputeScore(scenario.Ok, elapsedMs, composition, profile);
            var runSummary = $"{scenario.Summary} [target={executionTarget.Id}]";
            runs.Add(new WorkflowStressRunRecord(
                runId,
                gitSha,
                specHash,
                providerSnapshot,
                scenarioId,
                request.Id,
                composition.Id,
                profile.Id,
                i,
                scenario.Ok,
                composition.Roles.Count,
                scenario.ConflictCount,
                scenario.EscalationCount,
                elapsedMs,
                score,
                runSummary,
                scenario.FailureCategory,
                scenario.Skipped,
                startedAt,
                telemetry.CpuTimeDeltaMs,
                telemetry.WorkingSetMb,
                telemetry.PrivateMemoryMb,
                telemetry.ManagedMemoryMb,
                telemetry.ThreadCount,
                telemetry.HardwareProfile,
                benchmarkSet));

            if (cooldownMs > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(cooldownMs), ct).ConfigureAwait(false);
            }
        }

        var aggregates = runs
            .GroupBy(x => new { x.RequestId, x.CompositionId, x.ModelProfileId })
            .Select(group =>
            {
                var list = group.ToArray();
                var successCount = list.Count(x => x.Success);
                var avgElapsed = (long)Math.Round(list.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average());
                var avgScore = Math.Round(list.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3);
                return new WorkflowStressAggregate(
                    ScenarioGroupId: $"{group.Key.RequestId}::{group.Key.CompositionId}::{group.Key.ModelProfileId}",
                    group.Key.RequestId,
                    group.Key.CompositionId,
                    group.Key.ModelProfileId,
                    list.Length,
                    successCount,
                    list.Length - successCount,
                    avgElapsed,
                    avgScore);
            })
            .OrderByDescending(x => x.AverageScore)
            .ThenByDescending(x => x.Successes)
            .ThenBy(x => x.AverageElapsedMs)
            .ToArray();

        if (persistHistory)
        {
            foreach (var run in runs)
            {
                WorkflowLabHistoryStore.Append(
                    repoRoot,
                    new WorkflowLabStressHistoryRow
                    {
                        RunId = run.RunId,
                        GitSha = run.GitSha,
                        SpecHash = run.SpecHash,
                        ProviderSnapshot = run.ProviderSnapshot,
                        ScenarioId = run.ScenarioId,
                        RequestId = run.RequestId,
                        CompositionId = run.CompositionId,
                        ModelProfileId = run.ModelProfileId,
                        Iteration = run.Iteration,
                        StartedAtUtc = run.StartedAtUtc,
                        ElapsedMs = run.ElapsedMs,
                        Success = run.Success,
                        AgentCount = run.AgentCount,
                        ConflictCount = run.ConflictCount,
                        EscalationCount = run.EscalationCount,
                        Score = run.Score,
                        Summary = run.Summary,
                        Skipped = run.Skipped,
                        CpuTimeDeltaMs = run.CpuTimeDeltaMs,
                        WorkingSetMb = run.WorkingSetMb,
                        PrivateMemoryMb = run.PrivateMemoryMb,
                        ManagedMemoryMb = run.ManagedMemoryMb,
                        ThreadCount = run.ThreadCount,
                        HardwareProfile = run.HardwareProfile,
                        FailureCategory = run.FailureCategory,
                        BenchmarkSet = run.BenchmarkSet
                    });
            }
        }

        var allPassed = runs.All(run => run.Success || run.Skipped);
        var failureCount = runs.Count(run => !run.Success && !run.Skipped);
        var skippedCount = runs.Count(run => run.Skipped);
        var best = aggregates.FirstOrDefault(x => x.Successes > 0);
        var result = new WorkflowStressResult(
            allPassed,
            allPassed
                ? $"Workflow stress completed: {runs.Count} run(s) across {aggregates.Length} scenario groups (run-id={runId})."
                : $"Workflow stress completed with {failureCount} failing run(s), {skippedCount} skipped run(s), out of {runs.Count} (run-id={runId}).",
            runs,
            aggregates,
            best,
            runId,
            benchmarkSet,
            persistHistory);
        WriteStressResult(result, json);
        return result.Ok ? 0 : 1;
    }

    internal async Task<int> ExecuteOptimizeAsync(
        string? requestOverride,
        string? objective,
        string? objectiveFile,
        string? specPath,
        string? specJson,
        string? providerOverride,
        string? preferOverride,
        int? iterationsOverride,
        string? benchmarkSetOverride,
        bool? persistHistoryOverride,
        int? warmupRunsOverride,
        bool? shuffleScenariosOverride,
        int? randomSeedOverride,
        int? cooldownMsOverride,
        int maxCandidates,
        int? budgetRuns,
        string? searchStrategy,
        int? earlyStopMinRuns,
        double? earlyStopMinSuccessRate,
        bool includeMeshPeers,
        string? meshCapability,
        bool autoPullModels,
        bool promoteWinner,
        string? policyFile,
        string? reportOutputPath,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        var resolvedSpecPath = ResolveDefaultSpecPath(specPath);
        WorkflowLabRuntimeSpec spec;
        try
        {
            spec = WorkflowLabRuntimeSpecLoader.Load(resolvedSpecPath, specJson);
        }
        catch (Exception ex)
        {
            WriteOptimizeResult(new WorkflowOptimizeResult(false, $"Failed to load workflow lab spec: {ex.Message}"), json);
            return 1;
        }

        var repoRoot = Environment.CurrentDirectory;
        var requests = NormalizeRequests(spec.Requests);
        var compositions = NormalizeCompositions(spec.Compositions);
        var profiles = NormalizeProfiles(spec.ModelProfiles, providerOverride, preferOverride);
        if (requests.Length == 0 || compositions.Length == 0 || profiles.Length == 0)
        {
            WriteOptimizeResult(new WorkflowOptimizeResult(
                false,
                "Workflow optimize spec must include at least one request, composition, and model profile."), json);
            return 1;
        }

        var objectiveText = ResolveObjectiveText(objective, objectiveFile);
        if (objectiveText is null && !string.IsNullOrWhiteSpace(objectiveFile))
        {
            WriteOptimizeResult(new WorkflowOptimizeResult(
                false,
                $"Failed to load objective file: {objectiveFile}",
                Objective: objective,
                ObjectiveFile: objectiveFile), json);
            return 1;
        }

        var benchmarkSet = NormalizeBenchmarkSet(benchmarkSetOverride, spec.Execution.BenchmarkSet);
        var persistHistory = persistHistoryOverride ?? spec.Execution.PersistHistory;
        var iterations = Math.Max(1, iterationsOverride ?? spec.Execution.Iterations);
        var warmupRuns = Math.Max(0, warmupRunsOverride ?? spec.Execution.WarmupRuns);
        var cooldownMs = Math.Max(0, cooldownMsOverride ?? spec.Execution.CooldownMs);
        var shuffleScenarios = shuffleScenariosOverride ?? spec.Execution.ShuffleScenarioOrder;
        var randomSeed = randomSeedOverride ?? spec.Execution.RandomSeed;
        var rng = randomSeed.HasValue ? new Random(randomSeed.Value) : null;
        var strategy = NormalizeSearchStrategy(searchStrategy);
        var minRunsForEarlyStop = Math.Max(1, earlyStopMinRuns ?? 2);
        var minSuccessForEarlyStop = Math.Clamp(earlyStopMinSuccessRate ?? 0.35, 0d, 1d);
        var measuredRunBudget = budgetRuns.HasValue ? Math.Max(1, budgetRuns.Value) : int.MaxValue;
        var sharedRequest = string.IsNullOrWhiteSpace(requestOverride) ? null : requestOverride.Trim();
        var optimizeRunId = BuildRunId();
        var specHash = ComputeSpecHash(JsonSerializer.Serialize(spec));
        var gitSha = ResolveGitSha();
        var providerSnapshot = BuildProviderSnapshot(profiles);

        var scenarioPlans = BuildScenarioPlans(requests, compositions, profiles, iterations);
        var groupedCandidates = scenarioPlans
            .GroupBy(
                x => $"{x.Request.Id}::{x.Composition.Id}::{x.Profile.Id}",
                StringComparer.OrdinalIgnoreCase)
            .Select(g => new OptimizeCandidatePlan(
                g.Key,
                g.First().Request,
                g.First().Composition,
                g.First().Profile,
                g.OrderBy(x => x.Iteration).ToArray()))
            .ToList();

        var objectiveKeywordSet = BuildObjectiveKeywordSet(objectiveText);
        var maxCandidateCount = Math.Max(1, maxCandidates);
        var synthesizedCandidates = BuildObjectiveSynthesizedCandidates(
            groupedCandidates,
            objectiveKeywordSet,
            maxCandidateCount);
        if (synthesizedCandidates.Count > 0)
        {
            groupedCandidates = groupedCandidates
                .Concat(synthesizedCandidates)
                .GroupBy(x => x.CandidateId, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
        }

        if (shuffleScenarios && groupedCandidates.Count > 1)
            ShuffleOptimizeCandidates(groupedCandidates, rng ?? new Random());

        groupedCandidates = SortCandidatesForSearchStrategy(groupedCandidates, strategy, objectiveKeywordSet);

        if (groupedCandidates.Count > maxCandidateCount)
            groupedCandidates = groupedCandidates.Take(maxCandidateCount).ToList();

        var preflightByProvider = new Dictionary<string, PreflightResult>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in profiles
                     .Select(x => x.Default.Provider)
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Select(x => x!.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            preflightByProvider[provider] = await _providerPreflight(provider, ct).ConfigureAwait(false);
        }

        var executionTargets = await ResolveExecutionTargetsAsync(
            includeMeshPeers,
            meshCapability,
            ct).ConfigureAwait(false);

        var measuredRunsUsed = 0;
        var targetStats = executionTargets.ToDictionary(
            x => x.Id,
            x => new TargetExecutionStats(),
            StringComparer.OrdinalIgnoreCase);
        var allocationTrace = new List<OptimizeAllocationTrace>();
        var adaptiveSynthesisBudget = Math.Max(1, maxCandidateCount / 3);
        var adaptiveSynthesisCount = 0;
        var adaptiveSynthesisCursor = 0;
        var candidates = new List<WorkflowOptimizeCandidate>();
        var persistedRows = new List<WorkflowLabStressHistoryRow>();
        var candidateStates = new List<OptimizeCandidateRuntimeState>();
        for (var candidateIndex = 0; candidateIndex < groupedCandidates.Count; candidateIndex++)
        {
            var candidate = groupedCandidates[candidateIndex];
            var profileProvider = candidate.Profile.Default.Provider?.Trim();
            var requiredModels = ResolveOllamaModelsForCandidate(candidate.Composition, candidate.Profile);
            var pullResult = autoPullModels
                ? await _ollamaModelPuller(requiredModels, ct).ConfigureAwait(false)
                : new ModelPullResult(
                    Ok: true,
                    Summary: "Model auto-pull disabled.",
                    Models: requiredModels,
                    PulledModels: Array.Empty<string>());
            var candidatePlans = strategy == "successive-halving"
                ? candidate.Plans.Take(Math.Max(1, (candidate.Plans.Count + 1) / 2)).ToArray()
                : candidate.Plans.ToArray();
            candidateStates.Add(new OptimizeCandidateRuntimeState(
                candidate,
                $"{optimizeRunId}-c{candidateIndex + 1:D2}",
                profileProvider,
                requiredModels,
                pullResult,
                candidatePlans));
        }

        while (measuredRunsUsed < measuredRunBudget)
        {
            var candidateState = SelectNextCandidateState(candidateStates, objectiveKeywordSet);
            if (candidateState is null)
                break;
            ct.ThrowIfCancellationRequested();
            var plan = candidateState.Plans[candidateState.NextPlanIndex];
            candidateState.NextPlanIndex++;
            var request = plan.Request;
            var composition = plan.Composition;
            var profile = plan.Profile;
            var iteration = plan.Iteration;
            var executionTarget = SelectExecutionTarget(executionTargets, targetStats);
            var scenarioId = BuildScenarioId(request.Id, composition.Id, profile.Id, iteration) +
                             $"::target-{NormalizeScenarioTargetSegment(executionTarget.Id)}";
            var runtime = BuildRuntimeSpec(composition, profile);
            var runtimeJson = JsonSerializer.Serialize(runtime);
            var runtimeExecutionRequest = BuildExecutionRequest(request, composition, profile, sharedRequest);

            for (var warmup = 0; warmup < warmupRuns; warmup++)
            {
                ct.ThrowIfCancellationRequested();
                if (!candidateState.PullResult.Ok && executionTarget.IsLocal)
                    break;
                if (executionTarget.IsLocal &&
                    !string.IsNullOrWhiteSpace(candidateState.ProfileProvider) &&
                    preflightByProvider.TryGetValue(candidateState.ProfileProvider, out var warmupPreflight) &&
                    !warmupPreflight.Ok)
                {
                    break;
                }

                try
                {
                    _ = await ExecuteScenarioForTargetAsync(
                        executionTarget,
                        runtimeExecutionRequest,
                        runtimeJson,
                        profile.Default.Provider,
                        verbose,
                        ct).ConfigureAwait(false);
                }
                catch
                {
                    // Warmups are intentionally ignored in persisted metrics.
                }
            }

            ct.ThrowIfCancellationRequested();
            var startedAt = DateTimeOffset.UtcNow;
            var cpuStart = Process.GetCurrentProcess().TotalProcessorTime;
            var sw = Stopwatch.StartNew();
            ScenarioExecutionResult scenario;
            if (!candidateState.PullResult.Ok && executionTarget.IsLocal)
            {
                scenario = new ScenarioExecutionResult(
                    Ok: true,
                    Summary: $"Skipped due to model pull failure: {candidateState.PullResult.Summary}",
                    ConflictCount: 0,
                    EscalationCount: 0,
                    FailureCategory: "skipped_infra",
                    Skipped: true);
            }
            else if (executionTarget.IsLocal &&
                     !string.IsNullOrWhiteSpace(candidateState.ProfileProvider) &&
                     preflightByProvider.TryGetValue(candidateState.ProfileProvider, out var preflight) &&
                     !preflight.Ok)
            {
                scenario = new ScenarioExecutionResult(
                    Ok: true,
                    Summary: $"Skipped due to provider preflight failure ({candidateState.ProfileProvider}): {preflight.Detail}",
                    ConflictCount: 0,
                    EscalationCount: 0,
                    FailureCategory: "skipped_infra",
                    Skipped: true);
            }
            else
            {
                try
                {
                    scenario = await ExecuteScenarioForTargetAsync(
                        executionTarget,
                        runtimeExecutionRequest,
                        runtimeJson,
                        profile.Default.Provider,
                        verbose,
                        ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    scenario = new ScenarioExecutionResult(
                        Ok: false,
                        Summary: $"Scenario executor failed: {ex.Message}",
                        ConflictCount: 0,
                        EscalationCount: 0,
                        FailureCategory: "executor_failure");
                }
            }

            var elapsedMs = sw.ElapsedMilliseconds;
            var telemetry = CaptureRuntimeTelemetry(startedAt, cpuStart);
            var score = ComputeScore(scenario.Ok, elapsedMs, composition, profile);
            var runSummary = $"{scenario.Summary} [target={executionTarget.Id}]";
            var runRecord = new WorkflowStressRunRecord(
                candidateState.CandidateRunId,
                gitSha,
                specHash,
                providerSnapshot,
                scenarioId,
                request.Id,
                composition.Id,
                profile.Id,
                iteration,
                scenario.Ok,
                composition.Roles.Count,
                scenario.ConflictCount,
                scenario.EscalationCount,
                elapsedMs,
                score,
                runSummary,
                scenario.FailureCategory,
                scenario.Skipped,
                startedAt,
                telemetry.CpuTimeDeltaMs,
                telemetry.WorkingSetMb,
                telemetry.PrivateMemoryMb,
                telemetry.ManagedMemoryMb,
                telemetry.ThreadCount,
                telemetry.HardwareProfile,
                benchmarkSet);
            candidateState.Runs.Add(runRecord);
            measuredRunsUsed++;
            UpdateTargetExecutionStats(targetStats, executionTarget.Id, runRecord.Success, runRecord.Skipped, runRecord.ElapsedMs);
            allocationTrace.Add(new OptimizeAllocationTrace(
                measuredRunsUsed,
                candidateState.Candidate.CandidateId,
                executionTarget.Id,
                runRecord.Success,
                runRecord.ElapsedMs,
                candidateState.Candidate.Synthesized ? "synthesized-candidate" : "base-candidate"));

            var historyRow = new WorkflowLabStressHistoryRow
            {
                RunId = runRecord.RunId,
                GitSha = runRecord.GitSha,
                SpecHash = runRecord.SpecHash,
                ProviderSnapshot = runRecord.ProviderSnapshot,
                ScenarioId = runRecord.ScenarioId,
                RequestId = runRecord.RequestId,
                CompositionId = runRecord.CompositionId,
                ModelProfileId = runRecord.ModelProfileId,
                Iteration = runRecord.Iteration,
                StartedAtUtc = runRecord.StartedAtUtc,
                ElapsedMs = runRecord.ElapsedMs,
                Success = runRecord.Success,
                AgentCount = runRecord.AgentCount,
                ConflictCount = runRecord.ConflictCount,
                EscalationCount = runRecord.EscalationCount,
                Score = runRecord.Score,
                Summary = runRecord.Summary,
                Skipped = runRecord.Skipped,
                CpuTimeDeltaMs = runRecord.CpuTimeDeltaMs,
                WorkingSetMb = runRecord.WorkingSetMb,
                PrivateMemoryMb = runRecord.PrivateMemoryMb,
                ManagedMemoryMb = runRecord.ManagedMemoryMb,
                ThreadCount = runRecord.ThreadCount,
                HardwareProfile = runRecord.HardwareProfile,
                FailureCategory = runRecord.FailureCategory,
                BenchmarkSet = runRecord.BenchmarkSet
            };
            if (persistHistory)
                WorkflowLabHistoryStore.Append(repoRoot, historyRow);
            persistedRows.Add(historyRow);

            if (cooldownMs > 0)
                await Task.Delay(TimeSpan.FromMilliseconds(cooldownMs), ct).ConfigureAwait(false);

            if (candidateState.Runs.Count >= minRunsForEarlyStop)
            {
                var successful = candidateState.Runs.Count(x => x.Success);
                var successRateNow = candidateState.Runs.Count == 0 ? 0d : (double)successful / candidateState.Runs.Count;
                if (successRateNow < minSuccessForEarlyStop)
                    candidateState.EarlyStopped = true;
            }

            if (adaptiveSynthesisCount < adaptiveSynthesisBudget &&
                measuredRunsUsed < measuredRunBudget &&
                candidateStates.Count < maxCandidateCount &&
                candidateState.Runs.Count == 1 &&
                candidateState.Runs[0].Success &&
                !candidateState.Candidate.Synthesized &&
                objectiveKeywordSet.Count > 0)
            {
                var adaptiveCandidate = BuildAdaptiveFollowUpCandidate(
                    candidateState.Candidate,
                    objectiveKeywordSet,
                    ++adaptiveSynthesisCursor);
                if (adaptiveCandidate is not null &&
                    !candidateStates.Any(x => string.Equals(x.Candidate.CandidateId, adaptiveCandidate.CandidateId, StringComparison.OrdinalIgnoreCase)))
                {
                    var adaptiveProfileProvider = adaptiveCandidate.Profile.Default.Provider?.Trim();
                    var adaptiveModels = ResolveOllamaModelsForCandidate(adaptiveCandidate.Composition, adaptiveCandidate.Profile);
                    var adaptivePullResult = autoPullModels
                        ? await _ollamaModelPuller(adaptiveModels, ct).ConfigureAwait(false)
                        : new ModelPullResult(
                            Ok: true,
                            Summary: "Model auto-pull disabled.",
                            Models: adaptiveModels,
                            PulledModels: Array.Empty<string>());
                    var adaptivePlans = strategy == "successive-halving"
                        ? adaptiveCandidate.Plans.Take(Math.Max(1, (adaptiveCandidate.Plans.Count + 1) / 2)).ToArray()
                        : adaptiveCandidate.Plans.ToArray();
                    candidateStates.Add(new OptimizeCandidateRuntimeState(
                        adaptiveCandidate,
                        $"{optimizeRunId}-c{candidateStates.Count + 1:D2}",
                        adaptiveProfileProvider,
                        adaptiveModels,
                        adaptivePullResult,
                        adaptivePlans));
                    adaptiveSynthesisCount++;
                }
            }
        }

        foreach (var candidateState in candidateStates.Where(x => x.Runs.Count > 0))
        {
            var successCount = candidateState.Runs.Count(x => x.Success);
            var failureCount = candidateState.Runs.Count(x => !x.Success && !x.Skipped);
            var skippedCount = candidateState.Runs.Count(x => x.Skipped);
            var successRate = candidateState.Runs.Count == 0 ? 0d : Math.Round((double)successCount / candidateState.Runs.Count, 4);
            var avgLatency = candidateState.Runs.Count == 0
                ? 0L
                : (long)Math.Round(candidateState.Runs.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average());
            var p95Latency = candidateState.Runs.Count == 0 ? 0L : ComputePercentile(candidateState.Runs.Select(x => x.ElapsedMs), 0.95);
            var avgScore = candidateState.Runs.Count == 0
                ? 0d
                : Math.Round(candidateState.Runs.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3);
            var avgCpuDelta = candidateState.Runs.Count == 0
                ? 0L
                : (long)Math.Round(candidateState.Runs.Select(x => (double)x.CpuTimeDeltaMs).DefaultIfEmpty(0d).Average());
            var p95WorkingSet = candidateState.Runs.Count == 0 ? 0L : ComputePercentile(candidateState.Runs.Select(x => x.WorkingSetMb), 0.95);
            var p95PrivateMemory = candidateState.Runs.Count == 0 ? 0L : ComputePercentile(candidateState.Runs.Select(x => x.PrivateMemoryMb), 0.95);
            var p95ManagedMemory = candidateState.Runs.Count == 0 ? 0L : ComputePercentile(candidateState.Runs.Select(x => x.ManagedMemoryMb), 0.95);
            var maxThreadCount = candidateState.Runs.Count == 0 ? 0 : candidateState.Runs.Max(x => x.ThreadCount);
            var hardwareProfile = candidateState.Runs
                .Select(x => x.HardwareProfile)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Key)
                .FirstOrDefault() ?? "unknown";
            var candidateObjectiveScore = ScoreCandidateForObjective(candidateState.Candidate, objectiveKeywordSet);
            candidates.Add(new WorkflowOptimizeCandidate(
                CandidateId: candidateState.Candidate.CandidateId,
                RunId: candidateState.CandidateRunId,
                RequestId: candidateState.Candidate.Request.Id,
                CompositionId: candidateState.Candidate.Composition.Id,
                ModelProfileId: candidateState.Candidate.Profile.Id,
                TotalRuns: candidateState.Runs.Count,
                Successes: successCount,
                Failures: failureCount,
                Skipped: skippedCount,
                SuccessRate: successRate,
                AverageLatencyMs: avgLatency,
                P95LatencyMs: p95Latency,
                AverageScore: avgScore,
                AverageCpuTimeDeltaMs: avgCpuDelta,
                P95WorkingSetMb: p95WorkingSet,
                P95PrivateMemoryMb: p95PrivateMemory,
                P95ManagedMemoryMb: p95ManagedMemory,
                MaxThreadCount: maxThreadCount,
                HardwareProfile: hardwareProfile,
                Models: candidateState.RequiredModels,
                AutoPullSummary: candidateState.PullResult.Summary,
                AutoPullOk: candidateState.PullResult.Ok,
                Synthesized: candidateState.Candidate.Synthesized,
                SynthesisRationale: candidateState.Candidate.SynthesisRationale,
                ObjectiveScore: candidateObjectiveScore));
        }

        if (candidates.Count == 0)
        {
            WriteOptimizeResult(new WorkflowOptimizeResult(false, "No optimize candidates were generated."), json);
            return 1;
        }

        var ranked = candidates
            .OrderByDescending(x => x.SuccessRate)
            .ThenByDescending(x => x.AverageScore)
            .ThenByDescending(x => x.ObjectiveScore)
            .ThenBy(x => x.P95LatencyMs)
            .ThenBy(x => x.AverageLatencyMs)
            .ToArray();
        var winner = ranked[0];
        var winnerConfidence = ComputePromotionConfidence(winner, minRunsForEarlyStop);
        const double promotionConfidenceThreshold = 0.6;
        var minimumPromotionSamples = Math.Max(2, minRunsForEarlyStop);
        var winnerHasMinimumSamples = winner.TotalRuns >= minimumPromotionSamples;
        var recommendations = BuildOptimizeRecommendations(ranked);
        if (!winnerHasMinimumSamples)
        {
            recommendations = recommendations
                .Concat(new[]
                {
                    new WorkflowOptimizeRecommendation(
                        Kind: "sample-size",
                        Action: "collect-more-samples",
                        CandidateId: winner.CandidateId,
                        Rationale:
                        $"Winner has {winner.TotalRuns} measured run(s); minimum {minimumPromotionSamples} run(s) required before promotion.")
                })
                .ToArray();
        }
        if (winnerConfidence < promotionConfidenceThreshold)
        {
            recommendations = recommendations
                .Concat(new[]
                {
                    new WorkflowOptimizeRecommendation(
                        Kind: "confidence",
                        Action: "collect-more-samples",
                        CandidateId: winner.CandidateId,
                        Rationale:
                        $"Winner confidence {winnerConfidence:F2} is below threshold {promotionConfidenceThreshold:F2}; collect more measured runs before promotion.")
                })
                .ToArray();
        }
        var synthesizedCandidateCount = ranked.Count(x => x.Synthesized);
        var targetAllocations = BuildTargetAllocations(allocationTrace);
        var candidateAllocations = BuildCandidateAllocations(allocationTrace, ranked);

        string? promotionSummary = null;
        string? promotedBaselineId = null;
        if (promoteWinner && persistHistory)
        {
            if (!winnerHasMinimumSamples)
            {
                promotionSummary =
                    $"Promotion skipped: winner has {winner.TotalRuns} measured run(s), requires at least {minimumPromotionSamples}.";
            }
            else if (winnerConfidence < promotionConfidenceThreshold)
            {
                promotionSummary =
                    $"Promotion skipped: winner confidence {winnerConfidence:F2} below threshold {promotionConfidenceThreshold:F2}.";
            }

            var policyLoad = LoadGatePolicy(policyFile);
            if (!string.IsNullOrWhiteSpace(promotionSummary))
            {
                // confidence guardrail already decided promotion outcome
            }
            else if (!policyLoad.Ok)
            {
                promotionSummary = $"Promotion skipped: {policyLoad.Error}";
            }
            else
            {
                var effectiveMinSuccessRateDelta = policyLoad.Policy?.MinSuccessRateDelta ?? -0.05;
                var effectiveMaxP95LatencyRegressionMs = policyLoad.Policy?.MaxP95LatencyRegressionMs ?? 250;
                var effectiveMaxAverageLatencyRegressionMs = policyLoad.Policy?.MaxAverageLatencyRegressionMs ?? 150;
                var effectiveMinAverageScoreDelta = policyLoad.Policy?.MinAverageScoreDelta ?? -5.0;
                var effectiveMaxRegressedScenarios = policyLoad.Policy?.MaxRegressedScenarios ?? 2;
                var activeBaseline = WorkflowBaselineStore.ReadActive(repoRoot, benchmarkSet);
                if (activeBaseline is not null)
                {
                    var history = WorkflowLabHistoryStore.ReadAll(repoRoot)
                        .Where(x => string.Equals(x.BenchmarkSet, benchmarkSet, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    var comparison = BuildComparison(history, winner.RunId, activeBaseline.RunId);
                    if (!comparison.Valid)
                    {
                        promotionSummary = $"Promotion skipped: failed to compare against active baseline {activeBaseline.RunId}.";
                    }
                    else
                    {
                        var failures = new List<string>();
                        if (comparison.SuccessRateDelta < effectiveMinSuccessRateDelta)
                            failures.Add("success-rate");
                        if (comparison.P95LatencyDeltaMs > effectiveMaxP95LatencyRegressionMs)
                            failures.Add("p95-latency");
                        if (comparison.AverageLatencyDeltaMs > effectiveMaxAverageLatencyRegressionMs)
                            failures.Add("avg-latency");
                        if (comparison.AverageScoreDelta < effectiveMinAverageScoreDelta)
                            failures.Add("avg-score");
                        if (comparison.RegressedScenarios > effectiveMaxRegressedScenarios)
                            failures.Add("regressed-scenarios");

                        if (failures.Count > 0)
                        {
                            promotionSummary =
                                $"Promotion skipped: winner failed policy gates versus baseline {activeBaseline.RunId} ({string.Join(", ", failures)}).";
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(promotionSummary))
                {
                    var winnerRows = persistedRows
                        .Where(x => string.Equals(x.RunId, winner.RunId, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    if (winnerRows.Length == 0)
                    {
                        promotionSummary = "Promotion skipped: winner rows missing from history.";
                    }
                    else
                    {
                        var latest = winnerRows
                            .OrderByDescending(x => x.StartedAtUtc)
                            .First();
                        var promoted = new WorkflowBaselineRecord
                        {
                            BaselineId = BuildBaselineId(benchmarkSet, winner.RunId),
                            BenchmarkSet = benchmarkSet,
                            RunId = winner.RunId,
                            GitSha = latest.GitSha ?? "unknown",
                            SpecHash = latest.SpecHash ?? "unknown",
                            ProviderSnapshot = latest.ProviderSnapshot ?? "unknown",
                            PromotedAtUtc = DateTimeOffset.UtcNow,
                            Active = true,
                            Notes = $"auto-promoted by workflow optimize ({optimizeRunId})",
                            Policy = policyLoad.Policy is null
                                ? null
                                : new WorkflowGatePolicySpec
                                {
                                    BenchmarkSet = policyLoad.Policy.BenchmarkSet,
                                    MinSuccessRateDelta = policyLoad.Policy.MinSuccessRateDelta,
                                    MaxP95LatencyRegressionMs = policyLoad.Policy.MaxP95LatencyRegressionMs,
                                    MaxAverageLatencyRegressionMs = policyLoad.Policy.MaxAverageLatencyRegressionMs,
                                    MinAverageScoreDelta = policyLoad.Policy.MinAverageScoreDelta,
                                    MaxRegressedScenarios = policyLoad.Policy.MaxRegressedScenarios
                                }
                        };
                        WorkflowBaselineStore.Promote(repoRoot, promoted);
                        promotedBaselineId = promoted.BaselineId;
                        promotionSummary = $"Promoted winner run-id {winner.RunId} as active baseline {promoted.BaselineId}.";
                    }
                }
            }
        }
        else if (promoteWinner && !persistHistory)
        {
            promotionSummary = "Promotion skipped: persist-history disabled.";
        }

        var reportPath = ResolveOptimizeReportPath(reportOutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        var reportContent = RenderOptimizationRecommendationContent(
            reportPath,
            optimizeRunId,
            benchmarkSet,
            ranked,
            winner,
            recommendations,
            promotionSummary,
            objectiveText,
            objectiveFile,
            strategy,
            measuredRunsUsed,
            measuredRunBudget,
            minRunsForEarlyStop,
            minSuccessForEarlyStop,
            synthesizedCandidateCount,
            winnerConfidence,
            promotionConfidenceThreshold,
            allocationTrace,
            targetAllocations,
            candidateAllocations);
        File.WriteAllText(reportPath, reportContent);

        var hasSuccess = ranked.Any(x => x.Successes > 0);
        var result = new WorkflowOptimizeResult(
            Ok: hasSuccess,
            Summary: hasSuccess
                ? $"Workflow optimize completed: evaluated {ranked.Length} candidate(s); winner={winner.CandidateId}."
                : $"Workflow optimize completed with no successful candidates across {ranked.Length} evaluated candidate(s).",
            SessionRunId: optimizeRunId,
            BenchmarkSet: benchmarkSet,
            RecommendationReportPath: reportPath,
            Candidates: ranked,
            Winner: winner,
            Recommendations: recommendations,
            PromotionSummary: promotionSummary,
            PromotedBaselineId: promotedBaselineId,
            Objective: objectiveText,
            ObjectiveFile: objectiveFile,
            SearchStrategy: strategy,
            BudgetRuns: measuredRunBudget == int.MaxValue ? null : measuredRunBudget,
            MeasuredRunsUsed: measuredRunsUsed,
            EarlyStopMinRuns: minRunsForEarlyStop,
            EarlyStopMinSuccessRate: minSuccessForEarlyStop,
            SynthesizedCandidateCount: synthesizedCandidateCount,
            AdaptiveSynthesizedCandidateCount: adaptiveSynthesisCount,
            WinnerConfidence: winnerConfidence,
            PromotionConfidenceThreshold: promotionConfidenceThreshold,
            AllocationTrace: allocationTrace,
            TargetAllocations: targetAllocations,
            CandidateAllocations: candidateAllocations);
        WriteOptimizeResult(result, json);
        return result.Ok ? 0 : 1;
    }

    private static IReadOnlyList<WorkflowOptimizeRecommendation> BuildOptimizeRecommendations(
        IReadOnlyList<WorkflowOptimizeCandidate> ranked)
    {
        var recommendations = new List<WorkflowOptimizeRecommendation>();
        if (ranked.Count == 0)
            return recommendations;

        var winner = ranked[0];
        recommendations.Add(new WorkflowOptimizeRecommendation(
            Kind: "winner",
            Action: "promote",
            CandidateId: winner.CandidateId,
            Rationale:
            $"Highest rank by success-rate/score/latency (success-rate {winner.SuccessRate:P1}, avg-score {winner.AverageScore:F2}, p95 {winner.P95LatencyMs} ms)."));

        var stable = ranked
            .Where(x => x.SuccessRate >= 0.8 && x.AutoPullOk)
            .OrderByDescending(x => x.SuccessRate)
            .ThenBy(x => x.P95LatencyMs)
            .ThenByDescending(x => x.AverageScore)
            .FirstOrDefault();
        if (stable is not null && !string.Equals(stable.CandidateId, winner.CandidateId, StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add(new WorkflowOptimizeRecommendation(
                Kind: "stable-alternative",
                Action: "consider",
                CandidateId: stable.CandidateId,
                Rationale: $"Alternative stable candidate with success-rate {stable.SuccessRate:P1} and p95 {stable.P95LatencyMs} ms."));
        }

        foreach (var pullFailed in ranked.Where(x => !x.AutoPullOk).Take(3))
        {
            recommendations.Add(new WorkflowOptimizeRecommendation(
                Kind: "infra-remediation",
                Action: "fix",
                CandidateId: pullFailed.CandidateId,
                Rationale: $"Model pull failed for candidate ({pullFailed.AutoPullSummary}); install or pull required models first."));
        }

        foreach (var weak in ranked.Where(x => x.Failures > 0).OrderByDescending(x => x.Failures).Take(3))
        {
            recommendations.Add(new WorkflowOptimizeRecommendation(
                Kind: "avoid",
                Action: "de-prioritize",
                CandidateId: weak.CandidateId,
                Rationale: $"Candidate observed {weak.Failures} measured failures over {weak.TotalRuns} runs."));
        }

        return recommendations;
    }

    private static double ComputePromotionConfidence(
        WorkflowOptimizeCandidate winner,
        int minRunsForEarlyStop)
    {
        var requiredRuns = Math.Max(1, minRunsForEarlyStop);
        var runCoverage = Math.Min(1d, winner.TotalRuns / (double)requiredRuns);
        var successWeight = Math.Clamp(winner.SuccessRate, 0d, 1d);
        var failurePenalty = winner.Failures <= 0 ? 1d : 1d / (1d + winner.Failures);
        var confidence = (0.55d * successWeight) + (0.30d * runCoverage) + (0.15d * failurePenalty);
        return Math.Round(Math.Clamp(confidence, 0d, 1d), 4);
    }

    private static IReadOnlyList<TargetAllocationStat> BuildTargetAllocations(
        IReadOnlyList<OptimizeAllocationTrace> allocationTrace)
    {
        return (allocationTrace ?? Array.Empty<OptimizeAllocationTrace>())
            .GroupBy(x => x.TargetId, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var runs = group.Count();
                var successes = group.Count(x => x.Success);
                var successRate = runs == 0 ? 0d : Math.Round((double)successes / runs, 4);
                var avgLatencyMs = runs == 0
                    ? 0L
                    : (long)Math.Round(group.Select(x => (double)x.LatencyMs).DefaultIfEmpty(0d).Average());
                return new TargetAllocationStat(
                    TargetId: group.Key,
                    Runs: runs,
                    Successes: successes,
                    SuccessRate: successRate,
                    AverageLatencyMs: avgLatencyMs);
            })
            .OrderByDescending(x => x.SuccessRate)
            .ThenBy(x => x.AverageLatencyMs)
            .ThenByDescending(x => x.Runs)
            .ThenBy(x => x.TargetId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<CandidateAllocationStat> BuildCandidateAllocations(
        IReadOnlyList<OptimizeAllocationTrace> allocationTrace,
        IReadOnlyList<WorkflowOptimizeCandidate> rankedCandidates)
    {
        var rankedLookup = (rankedCandidates ?? Array.Empty<WorkflowOptimizeCandidate>())
            .ToDictionary(x => x.CandidateId, x => x, StringComparer.OrdinalIgnoreCase);

        return (allocationTrace ?? Array.Empty<OptimizeAllocationTrace>())
            .GroupBy(x => x.CandidateId, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var runs = group.Count();
                var successes = group.Count(x => x.Success);
                var successRate = runs == 0 ? 0d : Math.Round((double)successes / runs, 4);
                var avgLatencyMs = runs == 0
                    ? 0L
                    : (long)Math.Round(group.Select(x => (double)x.LatencyMs).DefaultIfEmpty(0d).Average());
                rankedLookup.TryGetValue(group.Key, out var ranked);
                return new CandidateAllocationStat(
                    CandidateId: group.Key,
                    Runs: runs,
                    Successes: successes,
                    SuccessRate: successRate,
                    AverageLatencyMs: avgLatencyMs,
                    ObjectiveScore: ranked?.ObjectiveScore ?? 0,
                    Synthesized: ranked?.Synthesized ?? false);
            })
            .OrderByDescending(x => x.SuccessRate)
            .ThenByDescending(x => x.ObjectiveScore)
            .ThenBy(x => x.AverageLatencyMs)
            .ThenByDescending(x => x.Runs)
            .ThenBy(x => x.CandidateId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveOllamaModelsForCandidate(
        WorkflowLabCompositionSpec composition,
        WorkflowLabModelProfileSpec profile)
    {
        var models = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static void AddModel(HashSet<string> target, string? model)
        {
            if (string.IsNullOrWhiteSpace(model))
                return;
            target.Add(model.Trim());
        }

        if (string.Equals(profile.Default.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
            AddModel(models, profile.Default.Model);

        foreach (var runtime in profile.Agents.Values)
        {
            if (!string.Equals(runtime.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
                continue;
            AddModel(models, runtime.Model);
        }

        foreach (var role in composition.Roles)
        {
            AddModel(models, role.OllamaModel);
            if (profile.AgentModelHints.TryGetValue(role.AgentId, out var hint))
                AddModel(models, hint);
            if (profile.Agents.TryGetValue(role.AgentId, out var runtime) &&
                string.Equals(runtime.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
            {
                AddModel(models, runtime.Model);
            }
        }

        return models
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ResolveOptimizeReportPath(string? reportOutputPath)
    {
        if (!string.IsNullOrWhiteSpace(reportOutputPath))
            return Path.GetFullPath(reportOutputPath);

        return Path.Combine(
            Environment.CurrentDirectory,
            ".nexo",
            "workflow",
            "optimize_recommendation_report.md");
    }

    private static string RenderOptimizationRecommendationContent(
        string reportPath,
        string sessionRunId,
        string benchmarkSet,
        IReadOnlyList<WorkflowOptimizeCandidate> ranked,
        WorkflowOptimizeCandidate winner,
        IReadOnlyList<WorkflowOptimizeRecommendation> recommendations,
        string? promotionSummary,
        string? objective,
        string? objectiveFile,
        string searchStrategy,
        int measuredRunsUsed,
        int measuredRunBudget,
        int earlyStopMinRuns,
        double earlyStopMinSuccessRate,
        int synthesizedCandidateCount,
        double winnerConfidence,
        double promotionConfidenceThreshold,
        IReadOnlyList<OptimizeAllocationTrace> allocationTrace,
        IReadOnlyList<TargetAllocationStat> targetAllocations,
        IReadOnlyList<CandidateAllocationStat> candidateAllocations)
    {
        var extension = Path.GetExtension(reportPath).Trim().ToLowerInvariant();
        if (extension == ".json")
        {
            return JsonSerializer.Serialize(new
            {
                generatedAtUtc = DateTimeOffset.UtcNow,
                sessionRunId,
                benchmarkSet,
                winner,
                candidates = ranked,
                recommendations,
                promotionSummary,
                optimizeExecution = new
                {
                    objective,
                    objectiveFile,
                    searchStrategy,
                    measuredRunsUsed,
                    measuredRunBudget = measuredRunBudget == int.MaxValue ? (int?)null : measuredRunBudget,
                    earlyStopMinRuns,
                    earlyStopMinSuccessRate,
                    synthesizedCandidateCount,
                    winnerConfidence,
                    promotionConfidenceThreshold
                },
                hardwareTelemetry = new
                {
                    hardwareProfile = winner.HardwareProfile,
                    averageCpuTimeDeltaMs = winner.AverageCpuTimeDeltaMs,
                    p95WorkingSetMb = winner.P95WorkingSetMb,
                    p95PrivateMemoryMb = winner.P95PrivateMemoryMb,
                    p95ManagedMemoryMb = winner.P95ManagedMemoryMb,
                    maxThreadCount = winner.MaxThreadCount
                },
                allocation = new
                {
                    trace = allocationTrace,
                    byTarget = targetAllocations,
                    byCandidate = candidateAllocations
                }
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        if (extension == ".txt")
            return RenderOptimizationRecommendationText(
                sessionRunId,
                benchmarkSet,
                ranked,
                winner,
                recommendations,
                promotionSummary,
                objective,
                objectiveFile,
                searchStrategy,
                measuredRunsUsed,
                measuredRunBudget,
                earlyStopMinRuns,
                earlyStopMinSuccessRate,
                synthesizedCandidateCount,
                winnerConfidence,
                promotionConfidenceThreshold,
                allocationTrace,
                targetAllocations,
                candidateAllocations);

        return RenderOptimizationRecommendationMarkdown(
            sessionRunId,
            benchmarkSet,
            ranked,
            winner,
            recommendations,
            promotionSummary,
            objective,
            objectiveFile,
            searchStrategy,
            measuredRunsUsed,
            measuredRunBudget,
            earlyStopMinRuns,
            earlyStopMinSuccessRate,
            synthesizedCandidateCount,
            winnerConfidence,
            promotionConfidenceThreshold,
            allocationTrace,
            targetAllocations,
            candidateAllocations);
    }

    private static string RenderOptimizationRecommendationMarkdown(
        string sessionRunId,
        string benchmarkSet,
        IReadOnlyList<WorkflowOptimizeCandidate> ranked,
        WorkflowOptimizeCandidate winner,
        IReadOnlyList<WorkflowOptimizeRecommendation> recommendations,
        string? promotionSummary,
        string? objective,
        string? objectiveFile,
        string searchStrategy,
        int measuredRunsUsed,
        int measuredRunBudget,
        int earlyStopMinRuns,
        double earlyStopMinSuccessRate,
        int synthesizedCandidateCount,
        double winnerConfidence,
        double promotionConfidenceThreshold,
        IReadOnlyList<OptimizeAllocationTrace> allocationTrace,
        IReadOnlyList<TargetAllocationStat> targetAllocations,
        IReadOnlyList<CandidateAllocationStat> candidateAllocations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Workflow Optimize Recommendation Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTimeOffset.UtcNow:O}");
        sb.AppendLine($"Session run-id: {sessionRunId}");
        sb.AppendLine($"Benchmark set: {benchmarkSet}");
        if (!string.IsNullOrWhiteSpace(objective))
            sb.AppendLine($"Objective: {objective}");
        if (!string.IsNullOrWhiteSpace(objectiveFile))
            sb.AppendLine($"Objective file: {objectiveFile}");
        sb.AppendLine($"Search strategy: {searchStrategy}");
        sb.AppendLine($"Measured runs used: {measuredRunsUsed}{(measuredRunBudget == int.MaxValue ? string.Empty : "/" + measuredRunBudget)}");
        sb.AppendLine($"Early stop: min-runs={earlyStopMinRuns}, min-success-rate={earlyStopMinSuccessRate:P0}");
        sb.AppendLine($"Synthesized candidates: {synthesizedCandidateCount}");
        sb.AppendLine($"Winner confidence: {winnerConfidence:F2} (promotion threshold {promotionConfidenceThreshold:F2})");
        sb.AppendLine();
        sb.AppendLine("## Winner");
        sb.AppendLine($"- Candidate: `{winner.CandidateId}`");
        sb.AppendLine($"- Run-id: `{winner.RunId}`");
        sb.AppendLine($"- Objective score: {winner.ObjectiveScore}");
        sb.AppendLine($"- Synthesized: {(winner.Synthesized ? "yes" : "no")}");
        if (winner.Synthesized && !string.IsNullOrWhiteSpace(winner.SynthesisRationale))
            sb.AppendLine($"- Synthesis rationale: {winner.SynthesisRationale}");
        sb.AppendLine($"- Success rate: {winner.SuccessRate:P1}");
        sb.AppendLine($"- Avg score: {winner.AverageScore:F2}");
        sb.AppendLine($"- Avg latency: {winner.AverageLatencyMs} ms");
        sb.AppendLine($"- P95 latency: {winner.P95LatencyMs} ms");
        sb.AppendLine($"- Models: {(winner.Models.Count == 0 ? "none" : string.Join(", ", winner.Models))}");
        sb.AppendLine($"- Auto-pull: {(winner.AutoPullOk ? "ok" : "failed")} ({winner.AutoPullSummary})");
        sb.AppendLine($"- Avg CPU delta: {winner.AverageCpuTimeDeltaMs} ms");
        sb.AppendLine($"- P95 working set: {winner.P95WorkingSetMb} MB");
        sb.AppendLine($"- P95 private memory: {winner.P95PrivateMemoryMb} MB");
        sb.AppendLine($"- P95 managed memory: {winner.P95ManagedMemoryMb} MB");
        sb.AppendLine($"- Max threads: {winner.MaxThreadCount}");
        sb.AppendLine($"- Hardware profile: {winner.HardwareProfile}");
        sb.AppendLine();
        sb.AppendLine("## Ranked Candidates");
        foreach (var candidate in ranked)
        {
            sb.AppendLine(
                $"- `{candidate.CandidateId}` | run `{candidate.RunId}` | objective-score {candidate.ObjectiveScore} | synthesized {(candidate.Synthesized ? "yes" : "no")} | success {candidate.SuccessRate:P1} | score {candidate.AverageScore:F2} | avg {candidate.AverageLatencyMs} ms | p95 {candidate.P95LatencyMs} ms | cpu {candidate.AverageCpuTimeDeltaMs} ms | ws-p95 {candidate.P95WorkingSetMb} MB | pull {(candidate.AutoPullOk ? "ok" : "failed")}");
            if (candidate.Synthesized && !string.IsNullOrWhiteSpace(candidate.SynthesisRationale))
                sb.AppendLine($"  - rationale: {candidate.SynthesisRationale}");
        }

        sb.AppendLine();
        sb.AppendLine("## Allocation Summary");
        foreach (var target in targetAllocations)
        {
            sb.AppendLine($"- target `{target.TargetId}`: runs {target.Runs}, success-rate {target.SuccessRate:P1}, avg latency {target.AverageLatencyMs} ms");
        }
        foreach (var candidateAllocation in candidateAllocations.Take(8))
        {
            sb.AppendLine($"- candidate `{candidateAllocation.CandidateId}`: runs {candidateAllocation.Runs}, success-rate {candidateAllocation.SuccessRate:P1}, avg latency {candidateAllocation.AverageLatencyMs} ms");
        }
        sb.AppendLine();
        sb.AppendLine("## Allocation Trace");
        foreach (var entry in allocationTrace.Take(24))
        {
            sb.AppendLine($"- run#{entry.RunIndex} candidate={entry.CandidateId} target={entry.TargetId} ok={entry.Success} latency={entry.LatencyMs}ms reason={entry.Reason}");
        }
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        foreach (var recommendation in recommendations)
        {
            sb.AppendLine(
                $"- `{recommendation.Kind}` [{recommendation.Action}] {(string.IsNullOrWhiteSpace(recommendation.CandidateId) ? "" : recommendation.CandidateId + " — ")}{recommendation.Rationale}");
        }

        sb.AppendLine();
        sb.AppendLine("## Promotion");
        sb.AppendLine($"- {promotionSummary ?? "Promotion was not requested."}");
        return sb.ToString();
    }

    private static string RenderOptimizationRecommendationText(
        string sessionRunId,
        string benchmarkSet,
        IReadOnlyList<WorkflowOptimizeCandidate> ranked,
        WorkflowOptimizeCandidate winner,
        IReadOnlyList<WorkflowOptimizeRecommendation> recommendations,
        string? promotionSummary,
        string? objective,
        string? objectiveFile,
        string searchStrategy,
        int measuredRunsUsed,
        int measuredRunBudget,
        int earlyStopMinRuns,
        double earlyStopMinSuccessRate,
        int synthesizedCandidateCount,
        double winnerConfidence,
        double promotionConfidenceThreshold,
        IReadOnlyList<OptimizeAllocationTrace> allocationTrace,
        IReadOnlyList<TargetAllocationStat> targetAllocations,
        IReadOnlyList<CandidateAllocationStat> candidateAllocations)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Workflow Optimize Recommendation Report");
        sb.AppendLine($"Generated: {DateTimeOffset.UtcNow:O}");
        sb.AppendLine($"Session run-id: {sessionRunId}");
        sb.AppendLine($"Benchmark set: {benchmarkSet}");
        if (!string.IsNullOrWhiteSpace(objective))
            sb.AppendLine($"Objective: {objective}");
        if (!string.IsNullOrWhiteSpace(objectiveFile))
            sb.AppendLine($"Objective file: {objectiveFile}");
        sb.AppendLine($"Search strategy: {searchStrategy}");
        sb.AppendLine($"Measured runs used: {measuredRunsUsed}{(measuredRunBudget == int.MaxValue ? string.Empty : "/" + measuredRunBudget)}");
        sb.AppendLine($"Early stop: min-runs={earlyStopMinRuns}, min-success-rate={earlyStopMinSuccessRate:P0}");
        sb.AppendLine($"Synthesized candidates: {synthesizedCandidateCount}");
        sb.AppendLine($"Winner confidence: {winnerConfidence:F2} (promotion threshold {promotionConfidenceThreshold:F2})");
        sb.AppendLine($"Winner: {winner.CandidateId} ({winner.RunId}) success={winner.SuccessRate:P1}, score={winner.AverageScore:F2}, avg={winner.AverageLatencyMs}ms, p95={winner.P95LatencyMs}ms");
        sb.AppendLine($"Winner objective: score={winner.ObjectiveScore}, synthesized={(winner.Synthesized ? "yes" : "no")}, rationale={winner.SynthesisRationale ?? "n/a"}");
        sb.AppendLine($"Winner telemetry: cpu={winner.AverageCpuTimeDeltaMs}ms, ws-p95={winner.P95WorkingSetMb}MB, private-p95={winner.P95PrivateMemoryMb}MB, managed-p95={winner.P95ManagedMemoryMb}MB, max-threads={winner.MaxThreadCount}, profile={winner.HardwareProfile}");
        sb.AppendLine("Ranked candidates:");
        foreach (var candidate in ranked)
        {
            sb.AppendLine(
                $"- {candidate.CandidateId}: run={candidate.RunId}, objective-score={candidate.ObjectiveScore}, synthesized={(candidate.Synthesized ? "yes" : "no")}, success={candidate.SuccessRate:P1}, score={candidate.AverageScore:F2}, avg={candidate.AverageLatencyMs}ms, p95={candidate.P95LatencyMs}ms, cpu={candidate.AverageCpuTimeDeltaMs}ms, ws-p95={candidate.P95WorkingSetMb}MB, pull={(candidate.AutoPullOk ? "ok" : "failed")}");
            if (candidate.Synthesized && !string.IsNullOrWhiteSpace(candidate.SynthesisRationale))
                sb.AppendLine($"  rationale={candidate.SynthesisRationale}");
        }

        sb.AppendLine("Recommendations:");
        foreach (var recommendation in recommendations)
        {
            sb.AppendLine(
                $"- [{recommendation.Action}] {recommendation.Kind} {(string.IsNullOrWhiteSpace(recommendation.CandidateId) ? "" : recommendation.CandidateId + ": ")}{recommendation.Rationale}");
        }

        sb.AppendLine("Allocation by target:");
        foreach (var target in targetAllocations)
            sb.AppendLine($"- {target.TargetId}: runs={target.Runs}, success-rate={target.SuccessRate:P1}, avg-latency={target.AverageLatencyMs}ms");
        sb.AppendLine("Allocation by candidate:");
        foreach (var candidateAllocation in candidateAllocations.Take(8))
            sb.AppendLine($"- {candidateAllocation.CandidateId}: runs={candidateAllocation.Runs}, success-rate={candidateAllocation.SuccessRate:P1}, avg-latency={candidateAllocation.AverageLatencyMs}ms");
        sb.AppendLine("Allocation trace:");
        foreach (var trace in allocationTrace.Take(24))
            sb.AppendLine($"- run#{trace.RunIndex}: candidate={trace.CandidateId}, target={trace.TargetId}, ok={trace.Success}, latency={trace.LatencyMs}ms, reason={trace.Reason}");

        sb.AppendLine($"Promotion: {promotionSummary ?? "Promotion was not requested."}");
        return sb.ToString();
    }

    private static async Task<ModelPullResult> PullOllamaModelsAsync(
        IReadOnlyList<string> models,
        CancellationToken ct)
    {
        var requested = (models ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requested.Length == 0)
        {
            return new ModelPullResult(
                Ok: true,
                Summary: "No Ollama models required for this candidate.",
                Models: requested,
                PulledModels: Array.Empty<string>(),
                FailedModels: Array.Empty<string>());
        }

        var pulled = new List<string>();
        var failed = new List<string>();
        foreach (var model in requested)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo("ollama")
                    {
                        UseShellExecute = false,
                        RedirectStandardError = false,
                        RedirectStandardOutput = false,
                        CreateNoWindow = true
                    }
                };
                process.StartInfo.ArgumentList.Add("pull");
                process.StartInfo.ArgumentList.Add(model);
                if (!process.Start())
                {
                    failed.Add(model);
                    continue;
                }

                await process.WaitForExitAsync(ct).ConfigureAwait(false);
                if (process.ExitCode == 0)
                    pulled.Add(model);
                else
                    failed.Add(model);
            }
            catch
            {
                failed.Add(model);
            }
        }

        var ok = failed.Count == 0;
        var summary = ok
            ? $"Pulled {pulled.Count} Ollama model(s): {string.Join(", ", pulled)}."
            : $"Pulled {pulled.Count}/{requested.Length} Ollama model(s); failed: {string.Join(", ", failed)}.";
        return new ModelPullResult(
            Ok: ok,
            Summary: summary,
            Models: requested,
            PulledModels: pulled.ToArray(),
            FailedModels: failed.ToArray());
    }

    private static void ShuffleOptimizeCandidates(IReadOnlyList<OptimizeCandidatePlan> candidates, Random rng)
    {
        if (candidates is not List<OptimizeCandidatePlan> list)
            return;

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static string BuildExecutionRequest(
        WorkflowLabRequestSpec request,
        WorkflowLabCompositionSpec composition,
        WorkflowLabModelProfileSpec profile,
        string? requestOverride)
    {
        if (!string.IsNullOrWhiteSpace(requestOverride))
            return requestOverride;

        var roleLines = composition.Roles.Select(role =>
        {
            var chain = role.CommandChain.Count == 0 ? string.Empty : $" chain={string.Join(">", role.CommandChain)}";
            var reportsTo = string.IsNullOrWhiteSpace(role.ReportsToAgentId) ? string.Empty : $" reportsTo={role.ReportsToAgentId}";
            var cluster = string.IsNullOrWhiteSpace(role.ClusterId) ? string.Empty : $" cluster={role.ClusterId}";
            var roleModel = ResolveRoleModel(profile, role);
            var modelDirective = string.IsNullOrWhiteSpace(roleModel) ? string.Empty : $" model={roleModel}";
            return
                $"- agentId={role.AgentId} role={role.Role} domain={role.Domain}{cluster}{reportsTo}{chain}{modelDirective} goal={role.Goal}";
        });
        return
            $"{request.Prompt}\n\n" +
            $"Use this workflow composition:\n{string.Join('\n', roleLines)}\n" +
            "Treat the composition as mandatory orchestration constraints.";
    }

    private static async Task<ScenarioExecutionResult> ExecuteScenarioAsync(
        OrchestrateCommand orchestrate,
        string request,
        string runtimeSpecJson,
        string? provider,
        bool json,
        bool verbose,
        CancellationToken ct)
    {
        var result = await orchestrate.ExecuteStructuredAsync(
            request,
            runtimeSpecPath: null,
            runtimeSpecJson,
            preferModel: null,
            provider,
            barrierLevel: null,
            preferredRegion: null,
            cancellationToken: ct).ConfigureAwait(false);

        if (result.Ok)
        {
            return new ScenarioExecutionResult(
                Ok: true,
                Summary: "Orchestration run completed successfully.",
                ConflictCount: result.Conflicts ?? 0,
                EscalationCount: result.Escalations ?? 0,
                FailureCategory: "none");
        }

        var summary = string.IsNullOrWhiteSpace(result.ErrorCode)
            ? $"Orchestration failed: {result.Error ?? "unknown error"}"
            : $"Orchestration failed with errorCode={result.ErrorCode}: {result.Error ?? "no error details"}";
        var category = ClassifyFailureCategory(result.Error ?? string.Empty, result.ErrorCode);
        return new ScenarioExecutionResult(
            Ok: false,
            Summary: summary,
            ConflictCount: result.Conflicts ?? 0,
            EscalationCount: result.Escalations ?? 0,
            FailureCategory: category);
    }

    private async Task<ScenarioExecutionResult> ExecuteScenarioForTargetAsync(
        ExecutionTarget target,
        string request,
        string runtimeSpecJson,
        string? provider,
        bool verbose,
        CancellationToken ct)
    {
        if (target.IsLocal || string.IsNullOrWhiteSpace(target.Endpoint))
        {
            return await _scenarioExecutor(
                request,
                runtimeSpecJson,
                provider,
                true,
                verbose,
                ct).ConfigureAwait(false);
        }

        return await _meshScenarioExecutor(
            target.Endpoint,
            request,
            runtimeSpecJson,
            provider,
            true,
            verbose,
            ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ExecutionTarget>> ResolveExecutionTargetsAsync(
        bool includeMeshPeers,
        string? meshCapability,
        CancellationToken ct)
    {
        var targets = new List<ExecutionTarget> { ExecutionTarget.Local };
        if (!includeMeshPeers)
            return targets;

        IReadOnlyList<PeerInfo> discovered;
        try
        {
            discovered = await _meshPeerDiscovery(ct).ConfigureAwait(false) ?? Array.Empty<PeerInfo>();
        }
        catch
        {
            return targets;
        }

        if (discovered.Count == 0)
            return targets;

        var capabilityFilter = string.IsNullOrWhiteSpace(meshCapability) ? null : meshCapability.Trim();
        var seenEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var peer in discovered)
        {
            ct.ThrowIfCancellationRequested();
            if (peer is null)
                continue;
            var endpoint = peer.Endpoint?.Trim();
            if (string.IsNullOrWhiteSpace(endpoint))
                continue;
            if (!string.IsNullOrWhiteSpace(capabilityFilter) &&
                !peer.Capabilities.Any(x => string.Equals(x?.Trim(), capabilityFilter, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (!TryNormalizeMeshEndpoint(endpoint, out var normalizedEndpoint))
                continue;
            if (!seenEndpoints.Add(normalizedEndpoint))
                continue;

            var peerId = string.IsNullOrWhiteSpace(peer.PeerId)
                ? normalizedEndpoint
                : peer.PeerId.Trim();
            targets.Add(new ExecutionTarget(peerId, normalizedEndpoint, false));
        }

        return targets;
    }

    private static bool TryNormalizeMeshEndpoint(string endpoint, out string normalizedEndpoint)
    {
        normalizedEndpoint = string.Empty;
        if (string.IsNullOrWhiteSpace(endpoint))
            return false;
        if (!Uri.TryCreate(endpoint.Trim(), UriKind.Absolute, out var uri))
            return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        normalizedEndpoint = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return !string.IsNullOrWhiteSpace(normalizedEndpoint);
    }

    private static string NormalizeScenarioTargetSegment(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return "unknown";
        var buffer = new StringBuilder(targetId.Length);
        foreach (var c in targetId.Trim())
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_')
                buffer.Append(char.ToLowerInvariant(c));
            else if (c is '.' or ':')
                buffer.Append('-');
        }

        return buffer.Length == 0 ? "unknown" : buffer.ToString();
    }

    private sealed record ExecutionTarget(
        string Id,
        string? Endpoint,
        bool IsLocal)
    {
        public static readonly ExecutionTarget Local = new("local", null, true);
    }

    private sealed record MeshOrchestrateRequest(
        string Request,
        string RuntimeSpecJson,
        string? Provider,
        bool Verbose,
        bool Json);

    private sealed record MeshOrchestrateResponse(
        bool Success,
        string? Summary,
        object? Output,
        int? Conflicts = null,
        int? Escalations = null,
        string? ErrorCode = null);

    private static async Task<ScenarioExecutionResult> ExecuteScenarioOnMeshPeerAsync(
        string endpoint,
        string request,
        string runtimeSpecJson,
        string? provider,
        bool outputJson,
        bool verbose,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMeshEndpoint(endpoint, out var normalizedEndpoint))
        {
            return new ScenarioExecutionResult(
                Ok: false,
                Summary: $"Mesh peer endpoint is invalid: {endpoint}",
                ConflictCount: 0,
                EscalationCount: 0,
                FailureCategory: "infra_unavailable");
        }

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"{normalizedEndpoint}/"), Timeout = TimeSpan.FromMinutes(3) };
            var payload = new MeshOrchestrateRequest(
                Request: request,
                RuntimeSpecJson: runtimeSpecJson,
                Provider: provider,
                Verbose: verbose,
                Json: outputJson);
            using var response = await client.PostAsJsonAsync("api/orchestrate", payload, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new ScenarioExecutionResult(
                    Ok: false,
                    Summary: $"Mesh peer returned HTTP {(int)response.StatusCode} for {normalizedEndpoint}.",
                    ConflictCount: 0,
                    EscalationCount: 0,
                    FailureCategory: "infra_unavailable");
            }

            var orchestrate = await response.Content.ReadFromJsonAsync<MeshOrchestrateResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (orchestrate is null)
            {
                return new ScenarioExecutionResult(
                    Ok: false,
                    Summary: $"Mesh peer response missing payload for {normalizedEndpoint}.",
                    ConflictCount: 0,
                    EscalationCount: 0,
                    FailureCategory: "orchestration_failure");
            }

            if (orchestrate.Success)
            {
                return new ScenarioExecutionResult(
                    Ok: true,
                    Summary: string.IsNullOrWhiteSpace(orchestrate.Summary)
                        ? $"Mesh peer run succeeded on {normalizedEndpoint}."
                        : orchestrate.Summary!,
                    ConflictCount: orchestrate.Conflicts ?? 0,
                    EscalationCount: orchestrate.Escalations ?? 0,
                    FailureCategory: "none");
            }

            var failureSummary = string.IsNullOrWhiteSpace(orchestrate.Summary)
                ? $"Mesh peer run failed on {normalizedEndpoint}."
                : $"Mesh peer run failed on {normalizedEndpoint}: {orchestrate.Summary}";
            return new ScenarioExecutionResult(
                Ok: false,
                Summary: failureSummary,
                ConflictCount: orchestrate.Conflicts ?? 0,
                EscalationCount: orchestrate.Escalations ?? 0,
                FailureCategory: ClassifyFailureCategory(failureSummary, orchestrate.ErrorCode));
        }
        catch (Exception ex)
        {
            var summary = $"Mesh peer request failed for {normalizedEndpoint}: {ex.Message}";
            return new ScenarioExecutionResult(
                Ok: false,
                Summary: summary,
                ConflictCount: 0,
                EscalationCount: 0,
                FailureCategory: ClassifyFailureCategory(summary));
        }
    }

    private static string ClassifyFailureCategory(string output, string? parsedErrorCode = null)
    {
        if (!string.IsNullOrWhiteSpace(parsedErrorCode))
        {
            if (parsedErrorCode.Contains("BARRIER", StringComparison.OrdinalIgnoreCase))
                return "runtime_context_failure";
            if (parsedErrorCode.Contains("ENDPOINT", StringComparison.OrdinalIgnoreCase))
                return "infra_unavailable";
        }

        var text = output ?? string.Empty;
        if (text.Contains("BarrierContext has already been initialized", StringComparison.OrdinalIgnoreCase))
            return "runtime_context_failure";
        if (text.Contains("At least one barrier level must be defined", StringComparison.OrdinalIgnoreCase))
            return "runtime_context_failure";
        if (text.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Could not list Ollama models", StringComparison.OrdinalIgnoreCase))
            return "infra_unavailable";
        if (text.Contains("Orchestrate response missing data payload", StringComparison.OrdinalIgnoreCase))
            return "orchestration_failure";
        return "model_execution_failure";
    }

    private static OrchestrationRuntimeSpec BuildRuntimeSpec(
        WorkflowLabCompositionSpec composition,
        WorkflowLabModelProfileSpec profile)
    {
        var defaultModel = profile.Default;
        var byAgent = new Dictionary<string, ModelRuntimeSpec>(profile.Agents, StringComparer.OrdinalIgnoreCase);
        foreach (var role in composition.Roles)
        {
            if (string.IsNullOrWhiteSpace(role.AgentId))
                continue;
            if (byAgent.ContainsKey(role.AgentId))
                continue;

            var provider = string.IsNullOrWhiteSpace(role.Provider) ? defaultModel.Provider : role.Provider;
            var prefer = string.IsNullOrWhiteSpace(role.Prefer) ? defaultModel.Prefer : role.Prefer!;
            byAgent[role.AgentId] = new ModelRuntimeSpec
            {
                Prefer = string.IsNullOrWhiteSpace(prefer) ? "auto" : prefer,
                Provider = provider
            };
        }

        return new OrchestrationRuntimeSpec
        {
            Model = defaultModel,
            Domains = new Dictionary<string, ModelRuntimeSpec>(profile.Domains, StringComparer.OrdinalIgnoreCase),
            Agents = byAgent
        };
    }

    private static WorkflowLabRequestSpec[] NormalizeRequests(IReadOnlyList<WorkflowLabRequestSpec> requests)
    {
        return (requests ?? Array.Empty<WorkflowLabRequestSpec>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .Where(x => !string.IsNullOrWhiteSpace(x.Prompt))
            .Select(x => x with { Id = x.Id.Trim(), Prompt = x.Prompt.Trim() })
            .ToArray();
    }

    private static WorkflowLabCompositionSpec[] NormalizeCompositions(IReadOnlyList<WorkflowLabCompositionSpec> compositions)
    {
        var output = new List<WorkflowLabCompositionSpec>();
        foreach (var composition in compositions ?? Array.Empty<WorkflowLabCompositionSpec>())
        {
            if (string.IsNullOrWhiteSpace(composition.Id))
                continue;

            var normalizedRoles = (composition.Roles ?? Array.Empty<WorkflowLabAgentRoleSpec>())
                .Where(role => !string.IsNullOrWhiteSpace(role.AgentId))
                .Where(role => !string.IsNullOrWhiteSpace(role.Goal))
                .Select(role => role with
                {
                    AgentId = role.AgentId.Trim(),
                    Role = string.IsNullOrWhiteSpace(role.Role) ? "builder" : role.Role.Trim(),
                    Domain = string.IsNullOrWhiteSpace(role.Domain) ? "general" : role.Domain.Trim(),
                    Goal = role.Goal.Trim(),
                    CommandChain = (role.CommandChain ?? Array.Empty<string>())
                        .Where(entry => !string.IsNullOrWhiteSpace(entry))
                        .Select(entry => entry.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray()
                })
                .ToArray();
            if (normalizedRoles.Length == 0)
                continue;

            output.Add(composition with
            {
                Id = composition.Id.Trim(),
                Description = composition.Description?.Trim() ?? string.Empty,
                Roles = normalizedRoles
            });
        }

        return output.ToArray();
    }

    private static WorkflowLabModelProfileSpec[] NormalizeProfiles(
        IReadOnlyList<WorkflowLabModelProfileSpec> profiles,
        string? providerOverride,
        string? preferOverride)
    {
        var provider = string.IsNullOrWhiteSpace(providerOverride) ? null : providerOverride.Trim();
        var prefer = string.IsNullOrWhiteSpace(preferOverride) ? null : preferOverride.Trim();
        var output = new List<WorkflowLabModelProfileSpec>();
        foreach (var profile in profiles ?? Array.Empty<WorkflowLabModelProfileSpec>())
        {
            if (string.IsNullOrWhiteSpace(profile.Id))
                continue;

            var defaultRuntime = ApplyOverrides(profile.Default, provider, prefer);
            var domains = profile.Domains.ToDictionary(
                kvp => kvp.Key,
                kvp => ApplyOverrides(kvp.Value, provider, prefer),
                StringComparer.OrdinalIgnoreCase);
            var agents = profile.Agents.ToDictionary(
                kvp => kvp.Key,
                kvp => ApplyOverrides(kvp.Value, provider, prefer),
                StringComparer.OrdinalIgnoreCase);

            output.Add(profile with
            {
                Id = profile.Id.Trim(),
                Description = profile.Description?.Trim() ?? string.Empty,
                Default = defaultRuntime,
                Domains = domains,
                Agents = agents
            });
        }

        return output.ToArray();
    }

    private static ModelRuntimeSpec ApplyOverrides(ModelRuntimeSpec runtime, string? provider, string? prefer)
    {
        var updated = runtime with { Prefer = string.IsNullOrWhiteSpace(runtime.Prefer) ? "auto" : runtime.Prefer };
        if (!string.IsNullOrWhiteSpace(provider))
            updated = updated with { Provider = provider };
        if (!string.IsNullOrWhiteSpace(prefer))
            updated = updated with { Prefer = prefer };
        return updated;
    }

    private static string ResolveDefaultSpecPath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath);
        return Path.Combine(Environment.CurrentDirectory, ".nexo", "workflow", "workflow_lab.runtime.json");
    }

    private static string BuildRunId()
        => DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];

    private static string ResolveGitSha()
    {
        try
        {
            var head = Path.Combine(Environment.CurrentDirectory, ".git", "HEAD");
            if (!File.Exists(head))
                return "unknown";
            var headRef = File.ReadAllText(head).Trim();
            if (!headRef.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
                return headRef.Length >= 12 ? headRef[..12] : headRef;
            var refPath = headRef["ref:".Length..].Trim();
            var fullRefPath = Path.Combine(Environment.CurrentDirectory, ".git", refPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullRefPath))
                return "unknown";
            var sha = File.ReadAllText(fullRefPath).Trim();
            return sha.Length >= 12 ? sha[..12] : sha;
        }
        catch
        {
            return "unknown";
        }
    }

    private static string ComputeSpecHash(string raw)
    {
        var text = raw ?? string.Empty;
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private static string BuildProviderSnapshot(IReadOnlyList<WorkflowLabModelProfileSpec> profiles)
    {
        var providers = profiles
            .Select(x => x.Default.Provider)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return providers.Length == 0 ? "none" : string.Join(",", providers);
    }

    private static Task<PreflightResult> PreflightProviderAsync(
        string provider,
        CancellationToken ct,
        IProviderFactory? providerFactory = null)
    {
        var normalized = string.IsNullOrWhiteSpace(provider) ? "unset" : provider.Trim().ToLowerInvariant();
        if (normalized is "unset" or "offline" or "mock-json")
            return Task.FromResult(new PreflightResult(true, normalized, "Provider does not require runtime connectivity check."));

        if (providerFactory is null)
            return Task.FromResult(new PreflightResult(false, normalized, "Provider factory unavailable for preflight."));

        try
        {
            var available = providerFactory.IsProviderAvailable(normalized);
            return Task.FromResult(available
                ? new PreflightResult(true, normalized, "Provider available.")
                : new PreflightResult(false, normalized, "Provider unavailable."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new PreflightResult(false, normalized, ex.Message));
        }
    }

    private static string BuildScenarioId(string requestId, string compositionId, string profileId, int iteration)
        => $"{requestId}::{compositionId}::{profileId}::iter-{iteration}";

    private static string NormalizeBenchmarkSet(string? benchmarkSetOverride, string defaultValue)
    {
        var value = string.IsNullOrWhiteSpace(benchmarkSetOverride) ? defaultValue : benchmarkSetOverride;
        var normalized = (value ?? "workflow-lab").Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "workflow-lab" : normalized;
    }

    private static GatePolicyLoadResult LoadGatePolicy(string? policyFile)
    {
        if (string.IsNullOrWhiteSpace(policyFile))
            return new GatePolicyLoadResult(true, null, null);

        try
        {
            var fullPath = Path.GetFullPath(policyFile);
            if (!File.Exists(fullPath))
                return new GatePolicyLoadResult(false, null, $"Policy file not found: {fullPath}");

            var content = File.ReadAllText(fullPath);
            var policy = JsonSerializer.Deserialize<WorkflowGatePolicy>(content);
            if (policy is null)
                return new GatePolicyLoadResult(false, null, $"Policy file is empty or invalid: {fullPath}");

            if (policy.MaxRegressedScenarios < 0)
                return new GatePolicyLoadResult(false, null, $"Policy maxRegressedScenarios must be >= 0: {fullPath}");
            if (policy.MaxP95LatencyRegressionMs < 0)
                return new GatePolicyLoadResult(false, null, $"Policy maxP95LatencyRegressionMs must be >= 0: {fullPath}");
            if (policy.MaxAverageLatencyRegressionMs < 0)
                return new GatePolicyLoadResult(false, null, $"Policy maxAverageLatencyRegressionMs must be >= 0: {fullPath}");

            return new GatePolicyLoadResult(true, policy, null);
        }
        catch (Exception ex)
        {
            return new GatePolicyLoadResult(false, null, $"Failed to parse policy file: {ex.Message}");
        }
    }

    private static string BuildBaselineId(string benchmarkSet, string runId)
    {
        var prefix = NormalizeBenchmarkSet(benchmarkSet, "workflow-lab").Replace(' ', '-');
        var suffix = string.IsNullOrWhiteSpace(runId) ? Guid.NewGuid().ToString("N")[..8] : runId.Trim();
        return $"{prefix}-{suffix}";
    }

    private static RuntimeTelemetry CaptureRuntimeTelemetry(DateTimeOffset startedAtUtc, TimeSpan cpuStart)
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            var _ = startedAtUtc;
            var cpuDeltaMs = Math.Max(
                0L,
                (long)Math.Round((process.TotalProcessorTime - cpuStart).TotalMilliseconds));
            var workingSetMb = BytesToMb(process.WorkingSet64);
            var privateMemoryMb = BytesToMb(process.PrivateMemorySize64);
            var managedMemoryMb = BytesToMb(GC.GetTotalMemory(forceFullCollection: false));
            var threadCount = process.Threads.Count;
            var hardwareProfile = ResolveHardwareProfile(process);
            return new RuntimeTelemetry(
                CpuTimeDeltaMs: cpuDeltaMs,
                WorkingSetMb: workingSetMb,
                PrivateMemoryMb: privateMemoryMb,
                ManagedMemoryMb: managedMemoryMb,
                ThreadCount: threadCount,
                HardwareProfile: hardwareProfile);
        }
        catch
        {
            return new RuntimeTelemetry(
                CpuTimeDeltaMs: 0,
                WorkingSetMb: 0,
                PrivateMemoryMb: 0,
                ManagedMemoryMb: 0,
                ThreadCount: 0,
                HardwareProfile: "unknown");
        }
    }

    private static string ResolveHardwareProfile(Process process)
    {
        var cpu = Environment.ProcessorCount;
        var memoryMb = BytesToMb(process.WorkingSet64);
        return $"cpu:{cpu}|ws:{memoryMb}mb";
    }

    private static long BytesToMb(long bytes)
    {
        if (bytes <= 0)
            return 0;
        return (long)Math.Round(bytes / (1024d * 1024d));
    }

    private static WorkflowBenchmarkReport BuildBenchmarkReport(IReadOnlyList<WorkflowLabStressHistoryRow> rows)
    {
        var items = rows ?? Array.Empty<WorkflowLabStressHistoryRow>();
        var measuredRuns = items.Where(x => !x.Skipped).ToArray();
        var totalRuns = measuredRuns.Length;
        var successRuns = measuredRuns.Count(x => x.Success);
        var failedRuns = totalRuns - successRuns;
        var skippedRuns = items.Count(x => x.Skipped);
        var successRate = totalRuns == 0 ? 0d : Math.Round((double)successRuns / totalRuns, 4);
        var avgElapsed = totalRuns == 0 ? 0L : (long)Math.Round(items.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average());
        var p95Elapsed = ComputePercentile(items.Select(x => x.ElapsedMs), 0.95);
        var avgScore = totalRuns == 0 ? 0d : Math.Round(items.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3);
        var avgConflicts = totalRuns == 0 ? 0d : Math.Round(items.Select(x => (double)x.ConflictCount).DefaultIfEmpty(0d).Average(), 3);
        var avgEscalations = totalRuns == 0 ? 0d : Math.Round(items.Select(x => (double)x.EscalationCount).DefaultIfEmpty(0d).Average(), 3);
        var avgCpuDelta = totalRuns == 0 ? 0L : (long)Math.Round(items.Select(x => (double)x.CpuTimeDeltaMs).DefaultIfEmpty(0d).Average());
        var p95WorkingSet = ComputePercentile(items.Select(x => x.WorkingSetMb), 0.95);
        var p95PrivateMemory = ComputePercentile(items.Select(x => x.PrivateMemoryMb), 0.95);
        var p95ManagedMemory = ComputePercentile(items.Select(x => x.ManagedMemoryMb), 0.95);
        var maxThreadCount = items.Count == 0 ? 0 : items.Max(x => x.ThreadCount);
        var hardwareProfile = items
            .Select(x => x.HardwareProfile)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Key)
            .FirstOrDefault() ?? "unknown";
        var failuresByCategory = measuredRuns
            .Where(x => !x.Success)
            .GroupBy(
                x => string.IsNullOrWhiteSpace(x.FailureCategory) || string.Equals(x.FailureCategory, "none", StringComparison.OrdinalIgnoreCase)
                    ? ClassifyFailureCategory(x.Summary ?? string.Empty)
                    : x.FailureCategory!,
                StringComparer.OrdinalIgnoreCase)
            .Select(g => new WorkflowFailureCategoryStat(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var scenarioStats = measuredRuns
            .GroupBy(x => $"{x.RequestId}::{x.CompositionId}::{x.ModelProfileId}", StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var data = g.ToArray();
                var groupSuccess = data.Count(x => x.Success);
                return new WorkflowScenarioBenchmark(
                    ScenarioGroupId: g.Key,
                    Runs: data.Length,
                    Successes: groupSuccess,
                    Failures: data.Length - groupSuccess,
                    SuccessRate: data.Length == 0 ? 0d : Math.Round((double)groupSuccess / data.Length, 4),
                    AverageElapsedMs: (long)Math.Round(data.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average()),
                    P95ElapsedMs: ComputePercentile(data.Select(x => x.ElapsedMs), 0.95),
                    AverageScore: Math.Round(data.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3),
                    AverageConflicts: Math.Round(data.Select(x => (double)x.ConflictCount).DefaultIfEmpty(0d).Average(), 3),
                    AverageEscalations: Math.Round(data.Select(x => (double)x.EscalationCount).DefaultIfEmpty(0d).Average(), 3))
                {
                    LastFailureSummary = data
                        .Where(x => !x.Success && !string.IsNullOrWhiteSpace(x.Summary))
                        .OrderByDescending(x => x.StartedAtUtc)
                        .Select(x => x.Summary)
                        .FirstOrDefault()
                };
            })
            .OrderByDescending(x => x.SuccessRate)
            .ThenByDescending(x => x.AverageScore)
            .ThenBy(x => x.AverageElapsedMs)
            .ToArray();

        var topScenarios = scenarioStats.Take(5).ToArray();
        var bottlenecks = scenarioStats
            .Where(x => x.Failures > 0 || x.P95ElapsedMs > avgElapsed * 1.5)
            .OrderByDescending(x => x.Failures)
            .ThenByDescending(x => x.P95ElapsedMs)
            .Take(5)
            .ToArray();

        var runIds = items
            .Select(x => x.RunId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var recommendations = CreateRecommendations(
            scenarioStats,
            failuresByCategory,
            successRate,
            avgElapsed,
            topScenarios);

        var latest = items.OrderByDescending(x => x.StartedAtUtc).FirstOrDefault();

        return new WorkflowBenchmarkReport(
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            TotalRuns: totalRuns,
            SuccessRuns: successRuns,
            FailedRuns: failedRuns,
            SuccessRate: successRate,
            AverageElapsedMs: avgElapsed,
            P95ElapsedMs: p95Elapsed,
            AverageScore: avgScore,
            AverageConflicts: avgConflicts,
            AverageEscalations: avgEscalations,
            AverageCpuTimeDeltaMs: avgCpuDelta,
            P95WorkingSetMb: p95WorkingSet,
            P95PrivateMemoryMb: p95PrivateMemory,
            P95ManagedMemoryMb: p95ManagedMemory,
            MaxThreadCount: maxThreadCount,
            HardwareProfile: hardwareProfile,
            TopScenarios: topScenarios,
            Bottlenecks: bottlenecks,
            FailureCategories: failuresByCategory,
            RunIds: runIds,
            LatestRunId: latest?.RunId,
            GitSha: latest?.GitSha,
            SpecHash: latest?.SpecHash,
            ProviderSnapshot: latest?.ProviderSnapshot,
            Recommendations: recommendations);
    }

    private static IReadOnlyList<WorkflowRecommendation> CreateRecommendations(
        IReadOnlyList<WorkflowScenarioBenchmark> scenarioStats,
        IReadOnlyList<WorkflowFailureCategoryStat> failuresByCategory,
        double successRate,
        long avgElapsedMs,
        IReadOnlyList<WorkflowScenarioBenchmark> topScenarios)
    {
        var output = new List<WorkflowRecommendation>();
        var stable = scenarioStats
            .Where(x => x.SuccessRate >= 0.8)
            .OrderByDescending(x => x.SuccessRate)
            .ThenBy(x => x.P95ElapsedMs)
            .ThenByDescending(x => x.AverageScore)
            .FirstOrDefault();

        if (stable != null)
        {
            output.Add(new WorkflowRecommendation(
                "best_stable_config",
                "use",
                stable.ScenarioGroupId,
                $"Stable success-rate {stable.SuccessRate:P1} with p95 {stable.P95ElapsedMs} ms."));
        }
        else if (topScenarios.Count > 0)
        {
            var candidate = topScenarios[0];
            output.Add(new WorkflowRecommendation(
                "candidate_best_config",
                "investigate",
                candidate.ScenarioGroupId,
                $"No stable config yet; top candidate success-rate {candidate.SuccessRate:P1}, p95 {candidate.P95ElapsedMs} ms."));
        }

        var infraFailures = failuresByCategory
            .FirstOrDefault(x => string.Equals(x.Category, "infra_unavailable", StringComparison.OrdinalIgnoreCase));
        if (infraFailures != null && infraFailures.Count > 0)
        {
            output.Add(new WorkflowRecommendation(
                "infra_dependency",
                "fix",
                null,
                $"Detected {infraFailures.Count} infra_unavailable failures; stabilize provider/runtime dependencies before comparing models."));
        }

        var badConfigs = scenarioStats
            .Where(x => x.Failures >= Math.Max(2, (int)Math.Ceiling(x.Runs * 0.7)))
            .OrderByDescending(x => x.Failures)
            .ThenByDescending(x => x.P95ElapsedMs)
            .Take(3)
            .ToArray();
        foreach (var bad in badConfigs)
        {
            output.Add(new WorkflowRecommendation(
                "do_not_use_config",
                "avoid",
                bad.ScenarioGroupId,
                $"High failure rate ({bad.Failures}/{bad.Runs}) and p95 {bad.P95ElapsedMs} ms."));
        }

        output.Add(new WorkflowRecommendation(
            "global_baseline",
            "monitor",
            null,
            $"Current benchmark baseline success-rate {successRate:P1}, average latency {avgElapsedMs} ms."));

        return output;
    }

    private static long ComputePercentile(IEnumerable<long> source, double percentile)
    {
        var ordered = source.OrderBy(x => x).ToArray();
        if (ordered.Length == 0)
            return 0;
        var clamped = Math.Clamp(percentile, 0d, 1d);
        var index = (int)Math.Ceiling((ordered.Length - 1) * clamped);
        return ordered[index];
    }

    private static IReadOnlyList<ScenarioPlan> BuildScenarioPlans(
        IReadOnlyList<WorkflowLabRequestSpec> requests,
        IReadOnlyList<WorkflowLabCompositionSpec> compositions,
        IReadOnlyList<WorkflowLabModelProfileSpec> profiles,
        int iterations)
    {
        var plans = new List<ScenarioPlan>();
        foreach (var request in requests)
        {
            foreach (var composition in compositions)
            {
                foreach (var profile in profiles)
                {
                    for (var i = 1; i <= iterations; i++)
                    {
                        plans.Add(new ScenarioPlan(request, composition, profile, i));
                    }
                }
            }
        }

        return plans;
    }

    private static void ShuffleScenarioPlans(IReadOnlyList<ScenarioPlan> plans, Random rng)
    {
        for (var i = plans.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            if (plans is List<ScenarioPlan> list)
            {
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    private static WorkflowRunComparison BuildComparison(
        IReadOnlyList<WorkflowLabStressHistoryRow> rows,
        string? runId,
        string? baselineRunId)
    {
        if (string.IsNullOrWhiteSpace(runId) && string.IsNullOrWhiteSpace(baselineRunId))
        {
            return new WorkflowRunComparison(true, "No run comparison requested.");
        }

        if (!string.IsNullOrWhiteSpace(runId) && string.IsNullOrWhiteSpace(baselineRunId))
        {
            return new WorkflowRunComparison(
                true,
                "No baseline run-id provided; comparison skipped.",
                RunId: runId.Trim());
        }

        if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(baselineRunId))
        {
            return new WorkflowRunComparison(false, "Both run-id and baseline-run-id are required for comparison.");
        }

        var candidateId = runId.Trim();
        var baselineId = baselineRunId.Trim();
        var candidateRows = rows
            .Where(x => string.Equals(x.RunId, candidateId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var baselineRows = rows
            .Where(x => string.Equals(x.RunId, baselineId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (candidateRows.Length == 0)
            return new WorkflowRunComparison(false, $"No history found for run-id '{candidateId}'.");
        if (baselineRows.Length == 0)
            return new WorkflowRunComparison(false, $"No history found for baseline-run-id '{baselineId}'.");

        var candidateMeasuredRows = candidateRows.Where(x => !x.Skipped).ToArray();
        var baselineMeasuredRows = baselineRows.Where(x => !x.Skipped).ToArray();
        if (candidateMeasuredRows.Length == 0)
            return new WorkflowRunComparison(false, $"No non-skipped history found for run-id '{candidateId}'.");
        if (baselineMeasuredRows.Length == 0)
            return new WorkflowRunComparison(false, $"No non-skipped history found for baseline-run-id '{baselineId}'.");

        var candidateSuccessRate = Math.Round((double)candidateMeasuredRows.Count(x => x.Success) / candidateMeasuredRows.Length, 4);
        var baselineSuccessRate = Math.Round((double)baselineMeasuredRows.Count(x => x.Success) / baselineMeasuredRows.Length, 4);
        var candidateAvgLatency = (long)Math.Round(candidateMeasuredRows.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average());
        var baselineAvgLatency = (long)Math.Round(baselineMeasuredRows.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average());
        var candidateP95 = ComputePercentile(candidateMeasuredRows.Select(x => x.ElapsedMs), 0.95);
        var baselineP95 = ComputePercentile(baselineMeasuredRows.Select(x => x.ElapsedMs), 0.95);
        var candidateScore = Math.Round(candidateMeasuredRows.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3);
        var baselineScore = Math.Round(baselineMeasuredRows.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3);

        var candidateByScenario = candidateMeasuredRows
            .GroupBy(x => $"{x.RequestId}::{x.CompositionId}::{x.ModelProfileId}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    SuccessRate = Math.Round((double)g.Count(x => x.Success) / Math.Max(1, g.Count()), 4),
                    AvgLatency = (long)Math.Round(g.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average()),
                    AvgScore = Math.Round(g.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3)
                },
                StringComparer.OrdinalIgnoreCase);

        var baselineByScenario = baselineMeasuredRows
            .GroupBy(x => $"{x.RequestId}::{x.CompositionId}::{x.ModelProfileId}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    SuccessRate = Math.Round((double)g.Count(x => x.Success) / Math.Max(1, g.Count()), 4),
                    AvgLatency = (long)Math.Round(g.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average()),
                    AvgScore = Math.Round(g.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3)
                },
                StringComparer.OrdinalIgnoreCase);

        var scenarioDeltas = candidateByScenario.Keys
            .Intersect(baselineByScenario.Keys, StringComparer.OrdinalIgnoreCase)
            .Select(key =>
            {
                var candidate = candidateByScenario[key];
                var baseline = baselineByScenario[key];
                return new WorkflowScenarioDelta(
                    key,
                    candidate.SuccessRate - baseline.SuccessRate,
                    candidate.AvgLatency - baseline.AvgLatency,
                    Math.Round(candidate.AvgScore - baseline.AvgScore, 3));
            })
            .OrderBy(x => x.SuccessRateDelta)
            .ThenByDescending(x => x.AverageLatencyDeltaMs)
            .ToArray();

        var regressedScenarios = scenarioDeltas.Count(x =>
            x.SuccessRateDelta < 0 ||
            x.AverageLatencyDeltaMs > 0 ||
            x.AverageScoreDelta < 0);

        return new WorkflowRunComparison(
            true,
            $"Compared run {candidateId} against baseline {baselineId}.",
            candidateId,
            baselineId,
            candidateMeasuredRows.Length,
            baselineMeasuredRows.Length,
            candidateSuccessRate,
            baselineSuccessRate,
            candidateSuccessRate - baselineSuccessRate,
            candidateAvgLatency,
            baselineAvgLatency,
            candidateAvgLatency - baselineAvgLatency,
            candidateP95,
            baselineP95,
            candidateP95 - baselineP95,
            candidateScore,
            baselineScore,
            Math.Round(candidateScore - baselineScore, 3),
            regressedScenarios,
            scenarioDeltas);
    }

    private static string RenderReportContent(WorkflowReportResult result, bool preferJson, string outputPath)
    {
        var extension = Path.GetExtension(outputPath).Trim().ToLowerInvariant();
        if (extension == ".md")
        {
            return RenderReportMarkdown(result);
        }

        if (extension == ".txt")
        {
            return RenderReportText(result);
        }

        if (extension == ".json")
        {
            return JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                report = result.Report
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        if (preferJson)
        {
            return JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                report = result.Report
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        return RenderReportText(result);
    }

    private static string RenderReportMarkdown(WorkflowReportResult result)
    {
        var report = result.Report;
        var sb = new StringBuilder();
        sb.AppendLine("# Workflow Stress Benchmark Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {report.GeneratedAtUtc:O}");
        if (!string.IsNullOrWhiteSpace(report.LatestRunId))
            sb.AppendLine($"Run ID: {report.LatestRunId}");
        if (!string.IsNullOrWhiteSpace(report.GitSha))
            sb.AppendLine($"Git SHA: {report.GitSha}");
        if (!string.IsNullOrWhiteSpace(report.SpecHash))
            sb.AppendLine($"Spec hash: {report.SpecHash}");
        if (!string.IsNullOrWhiteSpace(report.ProviderSnapshot))
            sb.AppendLine($"Provider snapshot: {report.ProviderSnapshot}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Total runs: {report.TotalRuns}");
        sb.AppendLine($"- Success runs: {report.SuccessRuns}");
        sb.AppendLine($"- Failed runs: {report.FailedRuns}");
        sb.AppendLine($"- Success rate: {report.SuccessRate:P1}");
        sb.AppendLine($"- Avg latency: {report.AverageElapsedMs} ms");
        sb.AppendLine($"- P95 latency: {report.P95ElapsedMs} ms");
        sb.AppendLine($"- Avg score: {report.AverageScore:F2}");
        sb.AppendLine($"- Avg conflicts: {report.AverageConflicts:F2}");
        sb.AppendLine($"- Avg escalations: {report.AverageEscalations:F2}");
        sb.AppendLine();
        sb.AppendLine("## Hardware Telemetry");
        sb.AppendLine($"- Hardware profile: {report.HardwareProfile}");
        sb.AppendLine($"- Avg CPU time delta: {report.AverageCpuTimeDeltaMs} ms");
        sb.AppendLine($"- P95 working set: {report.P95WorkingSetMb} MB");
        sb.AppendLine($"- P95 private memory: {report.P95PrivateMemoryMb} MB");
        sb.AppendLine($"- P95 managed memory: {report.P95ManagedMemoryMb} MB");
        sb.AppendLine($"- Max thread count: {report.MaxThreadCount}");
        sb.AppendLine();
        sb.AppendLine("## Failure Categories");
        foreach (var category in report.FailureCategories)
        {
            sb.AppendLine($"- `{category.Category}`: {category.Count}");
        }
        sb.AppendLine();
        sb.AppendLine("## Top Scenarios");
        foreach (var scenario in report.TopScenarios)
        {
            sb.AppendLine($"- `{scenario.ScenarioGroupId}` | success {scenario.Successes}/{scenario.Runs} ({scenario.SuccessRate:P1}) | score {scenario.AverageScore:F2} | p95 {scenario.P95ElapsedMs} ms");
        }
        sb.AppendLine();
        sb.AppendLine("## Bottlenecks");
        foreach (var bottleneck in report.Bottlenecks)
        {
            sb.AppendLine($"- `{bottleneck.ScenarioGroupId}` | failures {bottleneck.Failures} | p95 {bottleneck.P95ElapsedMs} ms | last failure: {bottleneck.LastFailureSummary ?? "n/a"}");
        }
        sb.AppendLine();
        sb.AppendLine("## Recommendations");
        foreach (var rec in report.Recommendations)
        {
            sb.AppendLine($"- `{rec.Kind}` [{rec.Action}] {(string.IsNullOrWhiteSpace(rec.Target) ? "" : rec.Target + " — ")}{rec.Rationale}");
        }
        if (result.Comparison is { Valid: true, RunId: not null, BaselineRunId: not null })
        {
            sb.AppendLine();
            sb.AppendLine("## Comparison");
            sb.AppendLine($"- Candidate run: `{result.Comparison.RunId}`");
            sb.AppendLine($"- Baseline run: `{result.Comparison.BaselineRunId}`");
            sb.AppendLine($"- Success-rate delta: {result.Comparison.SuccessRateDelta:+0.0000;-0.0000;0.0000}");
            sb.AppendLine($"- Avg latency delta: {result.Comparison.AverageLatencyDeltaMs} ms");
            sb.AppendLine($"- P95 latency delta: {result.Comparison.P95LatencyDeltaMs} ms");
            sb.AppendLine($"- Avg score delta: {result.Comparison.AverageScoreDelta:+0.000;-0.000;0.000}");
            sb.AppendLine($"- Regressed scenarios: {result.Comparison.RegressedScenarios}");
        }
        return sb.ToString();
    }

    private static string RenderReportText(WorkflowReportResult result)
    {
        var report = result.Report;
        var sb = new StringBuilder();
        sb.AppendLine("Workflow Stress Benchmark Report");
        sb.AppendLine($"Generated: {report.GeneratedAtUtc:O}");
        if (!string.IsNullOrWhiteSpace(report.LatestRunId))
            sb.AppendLine($"Run ID: {report.LatestRunId}");
        if (!string.IsNullOrWhiteSpace(report.GitSha))
            sb.AppendLine($"Git SHA: {report.GitSha}");
        if (!string.IsNullOrWhiteSpace(report.SpecHash))
            sb.AppendLine($"Spec hash: {report.SpecHash}");
        if (!string.IsNullOrWhiteSpace(report.ProviderSnapshot))
            sb.AppendLine($"Provider snapshot: {report.ProviderSnapshot}");
        sb.AppendLine($"Total runs: {report.TotalRuns}");
        sb.AppendLine($"Success runs: {report.SuccessRuns}");
        sb.AppendLine($"Failed runs: {report.FailedRuns}");
        sb.AppendLine($"Success rate: {report.SuccessRate:P1}");
        sb.AppendLine($"Average latency: {report.AverageElapsedMs} ms");
        sb.AppendLine($"P95 latency: {report.P95ElapsedMs} ms");
        sb.AppendLine($"Average score: {report.AverageScore:F2}");
        sb.AppendLine($"Average conflicts: {report.AverageConflicts:F2}");
        sb.AppendLine($"Average escalations: {report.AverageEscalations:F2}");
        sb.AppendLine($"Average CPU time delta: {report.AverageCpuTimeDeltaMs} ms");
        sb.AppendLine($"P95 working-set memory: {report.P95WorkingSetMb} MB");
        sb.AppendLine($"P95 private memory: {report.P95PrivateMemoryMb} MB");
        sb.AppendLine($"P95 managed memory: {report.P95ManagedMemoryMb} MB");
        sb.AppendLine($"Max thread count: {report.MaxThreadCount}");
        sb.AppendLine($"Hardware profile: {report.HardwareProfile}");
        sb.AppendLine();
        sb.AppendLine("Hardware telemetry:");
        sb.AppendLine($"- Hardware profile: {report.HardwareProfile}");
        sb.AppendLine($"- Average CPU time delta: {report.AverageCpuTimeDeltaMs} ms");
        sb.AppendLine($"- P95 working set: {report.P95WorkingSetMb} MB");
        sb.AppendLine($"- P95 private memory: {report.P95PrivateMemoryMb} MB");
        sb.AppendLine($"- P95 managed memory: {report.P95ManagedMemoryMb} MB");
        sb.AppendLine($"- Max thread count: {report.MaxThreadCount}");
        sb.AppendLine();
        sb.AppendLine("Failure categories:");
        foreach (var category in report.FailureCategories)
        {
            sb.AppendLine($"- {category.Category}: {category.Count}");
        }
        sb.AppendLine();
        sb.AppendLine("Top scenarios:");
        foreach (var scenario in report.TopScenarios)
        {
            sb.AppendLine($"- {scenario.ScenarioGroupId}: success {scenario.Successes}/{scenario.Runs}, score {scenario.AverageScore:F2}, p95 {scenario.P95ElapsedMs} ms");
        }
        sb.AppendLine();
        sb.AppendLine("Bottlenecks:");
        foreach (var bottleneck in report.Bottlenecks)
        {
            sb.AppendLine($"- {bottleneck.ScenarioGroupId}: failures {bottleneck.Failures}, p95 {bottleneck.P95ElapsedMs} ms, last failure={bottleneck.LastFailureSummary ?? "n/a"}");
        }
        sb.AppendLine();
        sb.AppendLine("Recommendations:");
        foreach (var rec in report.Recommendations)
        {
            var target = string.IsNullOrWhiteSpace(rec.Target) ? string.Empty : $"{rec.Target}: ";
            sb.AppendLine($"- [{rec.Action}] {rec.Kind} {target}{rec.Rationale}");
        }
        if (result.Comparison is { Valid: true, RunId: not null, BaselineRunId: not null })
        {
            sb.AppendLine();
            sb.AppendLine("Comparison:");
            sb.AppendLine($"- Candidate run: {result.Comparison.RunId}");
            sb.AppendLine($"- Baseline run: {result.Comparison.BaselineRunId}");
            sb.AppendLine($"- Success-rate delta: {result.Comparison.SuccessRateDelta:+0.0000;-0.0000;0.0000}");
            sb.AppendLine($"- Avg latency delta: {result.Comparison.AverageLatencyDeltaMs} ms");
            sb.AppendLine($"- P95 latency delta: {result.Comparison.P95LatencyDeltaMs} ms");
            sb.AppendLine($"- Avg score delta: {result.Comparison.AverageScoreDelta:+0.000;-0.000;0.000}");
            sb.AppendLine($"- Regressed scenarios: {result.Comparison.RegressedScenarios}");
        }
        return sb.ToString();
    }

    private static string? ResolveRoleModel(WorkflowLabModelProfileSpec profile, WorkflowLabAgentRoleSpec role)
    {
        if (!string.IsNullOrWhiteSpace(role.OllamaModel))
            return role.OllamaModel.Trim();
        if (profile.AgentModelHints.TryGetValue(role.AgentId, out var hint) && !string.IsNullOrWhiteSpace(hint))
            return hint.Trim();
        return null;
    }

    private static double ComputeScore(
        bool success,
        long elapsedMs,
        WorkflowLabCompositionSpec composition,
        WorkflowLabModelProfileSpec profile)
    {
        var successScore = success ? 100d : 0d;
        var latencyPenalty = Math.Min(60d, elapsedMs / 200d);
        var complexityBonus = Math.Min(20d, composition.Roles.Count * 2d);
        var diversityBonus = Math.Min(10d, profile.Agents.Count + profile.Domains.Count + profile.AgentModelHints.Count);
        var score = successScore - latencyPenalty + complexityBonus + diversityBonus;
        return Math.Round(score, 3);
    }

    private static string? ResolveObjectiveText(string? objective, string? objectiveFile)
    {
        if (!string.IsNullOrWhiteSpace(objective))
            return objective.Trim();
        if (string.IsNullOrWhiteSpace(objectiveFile))
            return null;
        try
        {
            var path = Path.GetFullPath(objectiveFile.Trim());
            if (!File.Exists(path))
                return null;
            var content = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static HashSet<string> BuildObjectiveKeywordSet(string? objective)
    {
        var output = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(objective))
            return output;

        var separators = new[] { ' ', '\t', '\r', '\n', ',', '.', ';', ':', '|', '-', '_', '/', '\\', '(', ')', '[', ']', '{', '}', '"' };
        foreach (var token in objective.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.Length < 3)
                continue;
            output.Add(token.ToLowerInvariant());
        }
        return output;
    }

    private static List<OptimizeCandidatePlan> BuildObjectiveSynthesizedCandidates(
        IReadOnlyList<OptimizeCandidatePlan> baseCandidates,
        HashSet<string> objectiveKeywords,
        int maxCandidates)
    {
        var synthesized = new List<OptimizeCandidatePlan>();
        if (baseCandidates.Count == 0 || objectiveKeywords.Count == 0 || !ShouldSynthesizeCandidates(objectiveKeywords))
            return synthesized;

        var seedCount = Math.Min(baseCandidates.Count, Math.Max(1, Math.Min(3, maxCandidates / 2)));
        var seeds = baseCandidates
            .Select(candidate => new
            {
                Candidate = candidate,
                ObjectiveScore = ScoreCandidateForObjective(candidate, objectiveKeywords)
            })
            .OrderByDescending(x => x.ObjectiveScore)
            .ThenByDescending(x => x.Candidate.Composition.Roles.Count)
            .ThenBy(x => x.Candidate.CandidateId, StringComparer.OrdinalIgnoreCase)
            .Take(seedCount)
            .Select(x => x.Candidate)
            .ToArray();

        for (var i = 0; i < seeds.Length; i++)
        {
            var seed = seeds[i];
            var synthesizedRequest = seed.Request with
            {
                Id = $"{seed.Request.Id}-synth-{i + 1}",
                Prompt = BuildSynthesizedPrompt(seed.Request.Prompt, objectiveKeywords)
            };
            var synthesizedProfile = SynthesizeProfileForObjective(seed.Profile, seed.Composition, objectiveKeywords, i);
            var candidateId = $"{seed.CandidateId}::synth-{i + 1}";
            var rationale = BuildSynthesisRationale(seed, objectiveKeywords, synthesizedProfile);
            var plans = seed.Plans
                .Select(plan => new ScenarioPlan(
                    synthesizedRequest,
                    seed.Composition,
                    synthesizedProfile,
                    plan.Iteration))
                .ToArray();
            synthesized.Add(new OptimizeCandidatePlan(
                CandidateId: candidateId,
                Request: synthesizedRequest,
                Composition: seed.Composition,
                Profile: synthesizedProfile,
                Plans: plans,
                Synthesized: true,
                SynthesisRationale: rationale));
        }

        return synthesized;
    }

    private static bool ShouldSynthesizeCandidates(HashSet<string> objectiveKeywords)
    {
        return objectiveKeywords.Contains("latency") ||
               objectiveKeywords.Contains("fast") ||
               objectiveKeywords.Contains("speed") ||
               objectiveKeywords.Contains("throughput") ||
               objectiveKeywords.Contains("quality") ||
               objectiveKeywords.Contains("accuracy") ||
               objectiveKeywords.Contains("reasoning") ||
               objectiveKeywords.Contains("thorough");
    }

    private static string BuildSynthesizedPrompt(string prompt, HashSet<string> objectiveKeywords)
    {
        if (objectiveKeywords.Count == 0)
            return prompt;
        var directives = objectiveKeywords
            .Take(4)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (directives.Length == 0)
            return prompt;
        return $"{prompt}\n\nOptimization objective focus: prioritize {string.Join(", ", directives)}.";
    }

    private static WorkflowLabModelProfileSpec SynthesizeProfileForObjective(
        WorkflowLabModelProfileSpec profile,
        WorkflowLabCompositionSpec composition,
        HashSet<string> objectiveKeywords,
        int seedIndex)
    {
        var defaultRuntime = profile.Default;
        var defaultHint = ResolveObjectiveDefaultModelHint(objectiveKeywords, seedIndex);
        if (!string.IsNullOrWhiteSpace(defaultHint) &&
            string.Equals(defaultRuntime.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
        {
            defaultRuntime = defaultRuntime with { Model = defaultHint };
        }

        var agents = profile.Agents.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var hints = profile.AgentModelHints.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var role in composition.Roles)
        {
            var roleHint = ResolveObjectiveRoleModelHint(objectiveKeywords, role, seedIndex);
            if (string.IsNullOrWhiteSpace(roleHint))
                continue;
            hints[role.AgentId] = roleHint;

            if (agents.TryGetValue(role.AgentId, out var runtime))
            {
                var resolvedProvider = string.IsNullOrWhiteSpace(runtime.Provider) ? defaultRuntime.Provider : runtime.Provider;
                agents[role.AgentId] = runtime with { Provider = resolvedProvider, Model = roleHint };
            }
            else if (string.Equals(defaultRuntime.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
            {
                agents[role.AgentId] = defaultRuntime with { Model = roleHint };
            }
        }

        return profile with
        {
            Id = $"{profile.Id}-synth-{seedIndex + 1}",
            Description = string.IsNullOrWhiteSpace(profile.Description)
                ? "Objective-synthesized profile."
                : $"{profile.Description} [objective-synthesized]",
            Default = defaultRuntime,
            Agents = agents,
            AgentModelHints = hints
        };
    }

    private static string? ResolveObjectiveDefaultModelHint(HashSet<string> objectiveKeywords, int seedIndex)
    {
        var lowLatency = objectiveKeywords.Contains("latency") ||
                         objectiveKeywords.Contains("fast") ||
                         objectiveKeywords.Contains("speed") ||
                         objectiveKeywords.Contains("throughput");
        if (lowLatency)
            return seedIndex % 2 == 0 ? "qwen2.5:7b" : "mistral:7b";

        var highReasoning = objectiveKeywords.Contains("quality") ||
                            objectiveKeywords.Contains("accuracy") ||
                            objectiveKeywords.Contains("reasoning") ||
                            objectiveKeywords.Contains("thorough");
        if (highReasoning)
            return seedIndex % 2 == 0 ? "llama3.1" : "qwen2.5:7b";

        return null;
    }

    private static string? ResolveObjectiveRoleModelHint(
        HashSet<string> objectiveKeywords,
        WorkflowLabAgentRoleSpec role,
        int seedIndex)
    {
        var roleText = $"{role.Role} {role.Domain} {role.Goal}".ToLowerInvariant();
        var lowLatency = objectiveKeywords.Contains("latency") ||
                         objectiveKeywords.Contains("fast") ||
                         objectiveKeywords.Contains("speed") ||
                         objectiveKeywords.Contains("throughput");
        if (lowLatency)
        {
            if (roleText.Contains("planner") || roleText.Contains("coordinat"))
                return "qwen2.5:7b";
            if (roleText.Contains("qa") || roleText.Contains("review"))
                return "mistral:7b";
            return seedIndex % 2 == 0 ? "qwen2.5:7b" : "llama3.1";
        }

        var qualityFocus = objectiveKeywords.Contains("quality") ||
                           objectiveKeywords.Contains("accuracy") ||
                           objectiveKeywords.Contains("reasoning") ||
                           objectiveKeywords.Contains("thorough");
        if (qualityFocus)
        {
            if (roleText.Contains("builder") || roleText.Contains("engineer"))
                return "codellama:13b";
            if (roleText.Contains("qa") || roleText.Contains("review"))
                return "mistral:7b";
            return "llama3.1";
        }

        return null;
    }

    private static string BuildSynthesisRationale(
        OptimizeCandidatePlan seed,
        HashSet<string> objectiveKeywords,
        WorkflowLabModelProfileSpec synthesizedProfile)
    {
        var keywords = objectiveKeywords
            .Take(4)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var keywordText = keywords.Length == 0 ? "none" : string.Join(", ", keywords);
        return $"Derived from {seed.CandidateId}; objective keywords [{keywordText}] applied to request prompt and profile {synthesizedProfile.Id}.";
    }

    private static string NormalizeSearchStrategy(string? searchStrategy)
    {
        var value = string.IsNullOrWhiteSpace(searchStrategy)
            ? "successive-halving"
            : searchStrategy.Trim().ToLowerInvariant();
        return value switch
        {
            "objective-first" => "objective-first",
            "exhaustive" => "exhaustive",
            _ => "successive-halving"
        };
    }

    private static List<OptimizeCandidatePlan> SortCandidatesForSearchStrategy(
        IReadOnlyList<OptimizeCandidatePlan> candidates,
        string strategy,
        HashSet<string> objectiveKeywords)
    {
        var list = candidates.ToList();
        if (list.Count <= 1 || string.Equals(strategy, "exhaustive", StringComparison.OrdinalIgnoreCase))
            return list;

        var ranked = list
            .Select(candidate => new
            {
                Candidate = candidate,
                ObjectiveScore = ScoreCandidateForObjective(candidate, objectiveKeywords)
            })
            .OrderByDescending(x => x.ObjectiveScore)
            .ThenByDescending(x => x.Candidate.Composition.Roles.Count)
            .ThenBy(x => x.Candidate.CandidateId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Candidate)
            .ToList();

        if (string.Equals(strategy, "objective-first", StringComparison.OrdinalIgnoreCase))
            return ranked;

        var halved = Math.Max(1, (ranked.Count + 1) / 2);
        return ranked.Take(halved).Concat(ranked.Skip(halved)).ToList();
    }

    private static int ScoreCandidateForObjective(OptimizeCandidatePlan candidate, HashSet<string> objectiveKeywords)
    {
        if (objectiveKeywords.Count == 0)
            return 0;

        var score = 0;
        score += CountMatches(candidate.Request.Id, objectiveKeywords) * 3;
        score += CountMatches(candidate.Request.Prompt, objectiveKeywords) * 3;
        score += CountMatches(candidate.Composition.Id, objectiveKeywords) * 2;
        score += CountMatches(candidate.Composition.Description, objectiveKeywords) * 2;
        score += CountMatches(candidate.Profile.Id, objectiveKeywords) * 2;

        foreach (var role in candidate.Composition.Roles)
        {
            score += CountMatches(role.AgentId, objectiveKeywords);
            score += CountMatches(role.Role, objectiveKeywords);
            score += CountMatches(role.Domain, objectiveKeywords);
            score += CountMatches(role.Goal, objectiveKeywords) * 2;
            score += CountMatches(role.OllamaModel, objectiveKeywords);
        }

        return score;
    }

    private static int CountMatches(string? text, HashSet<string> objectiveKeywords)
    {
        if (string.IsNullOrWhiteSpace(text) || objectiveKeywords.Count == 0)
            return 0;

        var lowered = text.ToLowerInvariant();
        var matches = 0;
        foreach (var token in objectiveKeywords)
        {
            if (lowered.Contains(token, StringComparison.OrdinalIgnoreCase))
                matches++;
        }

        return matches;
    }

    private static OptimizeCandidateRuntimeState? SelectNextCandidateState(
        IReadOnlyList<OptimizeCandidateRuntimeState> states,
        HashSet<string> objectiveKeywords)
    {
        return states
            .Where(x => x.NextPlanIndex < x.Plans.Count)
            .Where(x => !x.EarlyStopped)
            .Select(x => new
            {
                State = x,
                SuccessRate = x.Runs.Count == 0 ? 0d : (double)x.Runs.Count(r => r.Success) / x.Runs.Count,
                ObjectiveScore = ScoreCandidateForObjective(x.Candidate, objectiveKeywords),
                FailurePressure = x.Runs.Count(r => !r.Success && !r.Skipped)
            })
            .OrderByDescending(x => x.SuccessRate)
            .ThenByDescending(x => x.ObjectiveScore)
            .ThenBy(x => x.FailurePressure)
            .ThenBy(x => x.State.Runs.Count)
            .ThenBy(x => x.State.Candidate.CandidateId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.State)
            .FirstOrDefault();
    }

    private static ExecutionTarget SelectExecutionTarget(
        IReadOnlyList<ExecutionTarget> targets,
        IReadOnlyDictionary<string, TargetExecutionStats> targetStats)
    {
        if (targets.Count == 1)
            return targets[0];

        return targets
            .Select(target =>
            {
                targetStats.TryGetValue(target.Id, out var stats);
                var successRate = stats is null || stats.Runs == 0
                    ? 1d
                    : (double)stats.Successes / Math.Max(1, stats.Runs);
                var avgLatency = stats is null || stats.Runs == 0
                    ? 0d
                    : stats.TotalLatencyMs / (double)Math.Max(1, stats.Runs);
                return new
                {
                    Target = target,
                    Runs = stats?.Runs ?? 0,
                    SuccessRate = successRate,
                    AvgLatency = avgLatency
                };
            })
            .OrderByDescending(x => x.SuccessRate)
            .ThenBy(x => x.AvgLatency)
            .ThenBy(x => x.Runs)
            .ThenBy(x => x.Target.Id, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Target)
            .First();
    }

    private static void UpdateTargetExecutionStats(
        IDictionary<string, TargetExecutionStats> targetStats,
        string targetId,
        bool success,
        bool skipped,
        long latencyMs)
    {
        if (!targetStats.TryGetValue(targetId, out var stats))
        {
            stats = new TargetExecutionStats();
            targetStats[targetId] = stats;
        }

        stats.Runs++;
        if (success || skipped)
            stats.Successes++;
        stats.TotalLatencyMs += Math.Max(0, latencyMs);
    }

    private static OptimizeCandidatePlan? BuildAdaptiveFollowUpCandidate(
        OptimizeCandidatePlan seed,
        HashSet<string> objectiveKeywords,
        int cursor)
    {
        if (objectiveKeywords.Count == 0)
            return null;

        var profile = SynthesizeProfileForObjective(seed.Profile, seed.Composition, objectiveKeywords, cursor + 3);
        var request = seed.Request with
        {
            Id = $"{seed.Request.Id}-adaptive-{cursor}",
            Prompt = $"{BuildSynthesizedPrompt(seed.Request.Prompt, objectiveKeywords)}\n\nAdaptive follow-up attempt {cursor}."
        };
        var plans = seed.Plans
            .Select(plan => new ScenarioPlan(
                request,
                seed.Composition,
                profile,
                plan.Iteration))
            .ToArray();
        var candidateId = $"{seed.CandidateId}::adaptive-{cursor}";
        var rationale = $"Adaptive follow-up derived from {seed.CandidateId} after early successful signal.";
        return new OptimizeCandidatePlan(
            CandidateId: candidateId,
            Request: request,
            Composition: seed.Composition,
            Profile: profile,
            Plans: plans,
            Synthesized: true,
            SynthesisRationale: rationale);
    }

    private static void WriteScaffoldResult(WorkflowScaffoldResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                outputPath = result.OutputPath
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow scaffold: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.OutputPath))
            Console.WriteLine($"  output={result.OutputPath}");
    }

    private static void WriteStressResult(WorkflowStressResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                runId = result.RunId,
                benchmarkSet = result.BenchmarkSet,
                persistHistory = result.PersistHistory,
                runs = result.Runs,
                aggregates = result.Aggregates,
                best = result.Best
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow stress: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.BenchmarkSet))
            Console.WriteLine($"  run-id={result.RunId ?? "n/a"}, benchmark-set={result.BenchmarkSet} (persist-history={result.PersistHistory})");
        foreach (var aggregate in (result.Aggregates ?? Array.Empty<WorkflowStressAggregate>()).Take(5))
        {
            Console.WriteLine(
                $"  {aggregate.ScenarioGroupId}: success={aggregate.Successes}/{aggregate.TotalRuns}, avg-score={aggregate.AverageScore:F2}, avg-elapsed={aggregate.AverageElapsedMs}ms");
        }
        if (result.Best != null)
        {
            Console.WriteLine($"  best={result.Best.ScenarioGroupId} score={result.Best.AverageScore:F2}");
        }
    }

    private static void WriteHistoryResult(WorkflowHistoryResult result, bool json)
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

    private static void WriteReportResult(WorkflowReportResult result, bool json)
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
            Console.WriteLine(RenderComparisonText(result.Comparison, "  "));
        }
    }

    private static void WriteOptimizeResult(WorkflowOptimizeResult result, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = result.Ok,
                summary = result.Summary,
                sessionRunId = result.SessionRunId,
                benchmarkSet = result.BenchmarkSet,
                recommendationReportPath = result.RecommendationReportPath,
                winner = result.Winner,
                recommendations = result.Recommendations,
                promotionSummary = result.PromotionSummary,
                promotedBaselineId = result.PromotedBaselineId,
                objective = result.Objective,
                objectiveFile = result.ObjectiveFile,
                searchStrategy = result.SearchStrategy,
                budgetRuns = result.BudgetRuns,
                measuredRunsUsed = result.MeasuredRunsUsed,
                earlyStopMinRuns = result.EarlyStopMinRuns,
                earlyStopMinSuccessRate = result.EarlyStopMinSuccessRate,
                synthesizedCandidateCount = result.SynthesizedCandidateCount,
                adaptiveSynthesizedCandidateCount = result.AdaptiveSynthesizedCandidateCount,
                winnerConfidence = result.WinnerConfidence,
                promotionConfidenceThreshold = result.PromotionConfidenceThreshold,
                allocationTrace = result.AllocationTrace,
                targetAllocations = result.TargetAllocations,
                candidateAllocations = result.CandidateAllocations,
                candidates = result.Candidates
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"workflow optimize: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.SessionRunId))
            Console.WriteLine($"  session-run-id={result.SessionRunId}");
        if (!string.IsNullOrWhiteSpace(result.BenchmarkSet))
            Console.WriteLine($"  benchmark-set={result.BenchmarkSet}");
        if (!string.IsNullOrWhiteSpace(result.RecommendationReportPath))
            Console.WriteLine($"  recommendation-report={result.RecommendationReportPath}");
        if (!string.IsNullOrWhiteSpace(result.Objective))
            Console.WriteLine($"  objective={result.Objective}");
        if (!string.IsNullOrWhiteSpace(result.ObjectiveFile))
            Console.WriteLine($"  objective-file={result.ObjectiveFile}");
        if (!string.IsNullOrWhiteSpace(result.SearchStrategy))
            Console.WriteLine($"  search-strategy={result.SearchStrategy}");
        if (result.MeasuredRunsUsed.HasValue)
            Console.WriteLine($"  measured-runs-used={result.MeasuredRunsUsed}{(result.BudgetRuns.HasValue ? "/" + result.BudgetRuns.Value : string.Empty)}");
        if (result.EarlyStopMinRuns.HasValue || result.EarlyStopMinSuccessRate.HasValue)
            Console.WriteLine($"  early-stop=min-runs:{result.EarlyStopMinRuns ?? 0}, min-success-rate:{(result.EarlyStopMinSuccessRate ?? 0d):P0}");
        if (result.SynthesizedCandidateCount.HasValue)
            Console.WriteLine($"  synthesized-candidates={result.SynthesizedCandidateCount}");
        if (result.AdaptiveSynthesizedCandidateCount.HasValue)
            Console.WriteLine($"  adaptive-synthesized-candidates={result.AdaptiveSynthesizedCandidateCount}");
        if (result.WinnerConfidence.HasValue)
            Console.WriteLine($"  winner-confidence={result.WinnerConfidence:F2}");
        if (result.PromotionConfidenceThreshold.HasValue)
            Console.WriteLine($"  promotion-confidence-threshold={result.PromotionConfidenceThreshold:F2}");
        if (result.AllocationTrace is { Count: > 0 })
            Console.WriteLine($"  allocation-trace-entries={result.AllocationTrace.Count}");
        if (result.Winner is not null)
        {
            Console.WriteLine(
                $"  winner={result.Winner.CandidateId} (run-id={result.Winner.RunId}, success-rate={result.Winner.SuccessRate:P1}, avg-score={result.Winner.AverageScore:F2}, p95={result.Winner.P95LatencyMs}ms)");
        }
        if (result.Recommendations is { Count: > 0 })
        {
            Console.WriteLine("  recommendations:");
            foreach (var recommendation in result.Recommendations)
                Console.WriteLine($"    - [{recommendation.Action}] {recommendation.Kind}: {recommendation.Rationale}");
        }
        if (!string.IsNullOrWhiteSpace(result.PromotionSummary))
            Console.WriteLine($"  promotion={result.PromotionSummary}");
        if (!string.IsNullOrWhiteSpace(result.PromotedBaselineId))
            Console.WriteLine($"  promoted-baseline-id={result.PromotedBaselineId}");
    }

    private static void WriteGateResult(WorkflowGateResult result, bool json)
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
            Console.WriteLine(RenderComparisonText(result.Comparison, "  "));
        }
    }


    private static void WriteBaselinePromoteResult(WorkflowBaselinePromoteResult result, bool json)
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

    private static void WriteBaselineListResult(WorkflowBaselineListResult result, bool json)
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

    private static void WriteBaselineShowResult(WorkflowBaselineShowResult result, bool json)
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
            if (!string.IsNullOrWhiteSpace(result.Baseline.Notes))
                Console.WriteLine($"  notes={result.Baseline.Notes}");
            if (result.Baseline.Policy is not null)
                Console.WriteLine($"  policy={JsonSerializer.Serialize(result.Baseline.Policy)}");
        }
    }

    private static string RenderComparisonText(WorkflowRunComparison comparison, string indent)
    {
        var prefix = string.IsNullOrEmpty(indent) ? string.Empty : indent;
        var lines = new[]
        {
            $"{prefix}comparison={comparison.RunId} vs {comparison.BaselineRunId}",
            $"{prefix}success-rate-delta={comparison.SuccessRateDelta:+0.0000;-0.0000;0.0000}",
            $"{prefix}avg-latency-delta={comparison.AverageLatencyDeltaMs}ms",
            $"{prefix}p95-latency-delta={comparison.P95LatencyDeltaMs}ms",
            $"{prefix}avg-score-delta={comparison.AverageScoreDelta:+0.000;-0.000;0.000}",
            $"{prefix}regressed-scenarios={comparison.RegressedScenarios}"
        };
        return string.Join(Environment.NewLine, lines);
    }

    private sealed record WorkflowScaffoldResult(bool Ok, string Summary, string? OutputPath = null);

    private sealed record WorkflowStressRunRecord(
        string RunId,
        string GitSha,
        string SpecHash,
        string ProviderSnapshot,
        string ScenarioId,
        string RequestId,
        string CompositionId,
        string ModelProfileId,
        int Iteration,
        bool Success,
        int AgentCount,
        int ConflictCount,
        int EscalationCount,
        long ElapsedMs,
        double Score,
        string Summary,
        string FailureCategory,
        bool Skipped,
        DateTimeOffset StartedAtUtc,
        long CpuTimeDeltaMs,
        long WorkingSetMb,
        long PrivateMemoryMb,
        long ManagedMemoryMb,
        int ThreadCount,
        string HardwareProfile,
        string BenchmarkSet);

    private sealed record WorkflowStressAggregate(
        string ScenarioGroupId,
        string RequestId,
        string CompositionId,
        string ModelProfileId,
        int TotalRuns,
        int Successes,
        int Failures,
        long AverageElapsedMs,
        double AverageScore);

    private sealed record WorkflowStressResult(
        bool Ok,
        string Summary,
        IReadOnlyList<WorkflowStressRunRecord>? Runs = null,
        IReadOnlyList<WorkflowStressAggregate>? Aggregates = null,
        WorkflowStressAggregate? Best = null,
        string? RunId = null,
        string? BenchmarkSet = null,
        bool? PersistHistory = null);

    private sealed record WorkflowOptimizeCandidate(
        string CandidateId,
        string RunId,
        string RequestId,
        string CompositionId,
        string ModelProfileId,
        int TotalRuns,
        int Successes,
        int Failures,
        int Skipped,
        double SuccessRate,
        long AverageLatencyMs,
        long P95LatencyMs,
        double AverageScore,
        long AverageCpuTimeDeltaMs,
        long P95WorkingSetMb,
        long P95PrivateMemoryMb,
        long P95ManagedMemoryMb,
        long MaxThreadCount,
        string HardwareProfile,
        IReadOnlyList<string> Models,
        string AutoPullSummary,
        bool AutoPullOk,
        bool Synthesized = false,
        string? SynthesisRationale = null,
        int ObjectiveScore = 0);

    private sealed record WorkflowOptimizeRecommendation(
        string Kind,
        string Action,
        string CandidateId,
        string Rationale);

    private sealed record WorkflowOptimizeResult(
        bool Ok,
        string Summary,
        string? SessionRunId = null,
        string? BenchmarkSet = null,
        string? RecommendationReportPath = null,
        IReadOnlyList<WorkflowOptimizeCandidate>? Candidates = null,
        WorkflowOptimizeCandidate? Winner = null,
        IReadOnlyList<WorkflowOptimizeRecommendation>? Recommendations = null,
        string? PromotionSummary = null,
        string? PromotedBaselineId = null,
        string? Objective = null,
        string? ObjectiveFile = null,
        string? SearchStrategy = null,
        int? BudgetRuns = null,
        int? MeasuredRunsUsed = null,
        int? EarlyStopMinRuns = null,
        double? EarlyStopMinSuccessRate = null,
        int? SynthesizedCandidateCount = null,
        int? AdaptiveSynthesizedCandidateCount = null,
        double? WinnerConfidence = null,
        double? PromotionConfidenceThreshold = null,
        IReadOnlyList<OptimizeAllocationTrace>? AllocationTrace = null,
        IReadOnlyList<TargetAllocationStat>? TargetAllocations = null,
        IReadOnlyList<CandidateAllocationStat>? CandidateAllocations = null);

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

    private sealed record WorkflowScenarioBenchmark(
        string ScenarioGroupId,
        int Runs,
        int Successes,
        int Failures,
        double SuccessRate,
        long AverageElapsedMs,
        long P95ElapsedMs,
        double AverageScore,
        double AverageConflicts,
        double AverageEscalations)
    {
        public string? LastFailureSummary { get; init; }
    }

    private sealed record WorkflowFailureCategoryStat(
        string Category,
        int Count);

    private sealed record WorkflowBenchmarkReport(
        DateTimeOffset GeneratedAtUtc,
        int TotalRuns,
        int SuccessRuns,
        int FailedRuns,
        double SuccessRate,
        long AverageElapsedMs,
        long P95ElapsedMs,
        double AverageScore,
        double AverageConflicts,
        double AverageEscalations,
        long AverageCpuTimeDeltaMs,
        long P95WorkingSetMb,
        long P95PrivateMemoryMb,
        long P95ManagedMemoryMb,
        long MaxThreadCount,
        string HardwareProfile,
        IReadOnlyList<WorkflowScenarioBenchmark> TopScenarios,
        IReadOnlyList<WorkflowScenarioBenchmark> Bottlenecks,
        IReadOnlyList<WorkflowFailureCategoryStat> FailureCategories,
        IReadOnlyList<string> RunIds,
        string? LatestRunId,
        string? GitSha,
        string? SpecHash,
        string? ProviderSnapshot,
        IReadOnlyList<WorkflowRecommendation> Recommendations);

    private sealed record WorkflowRecommendation(
        string Kind,
        string Action,
        string? Target,
        string Rationale);

    private sealed record WorkflowReportResult(
        bool Ok,
        string Summary,
        WorkflowBenchmarkReport Report,
        string? OutputPath = null,
        WorkflowRunComparison? Comparison = null);

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

    private sealed record WorkflowGateResult(
        bool Ok,
        bool Passed,
        string Summary,
        IReadOnlyList<string>? Failures = null,
        WorkflowRunComparison? Comparison = null);

    private sealed record WorkflowRunComparison(
        bool Valid,
        string Summary,
        string? RunId = null,
        string? BaselineRunId = null,
        int CandidateRunCount = 0,
        int BaselineRunCount = 0,
        double CandidateSuccessRate = 0d,
        double BaselineSuccessRate = 0d,
        double SuccessRateDelta = 0d,
        long CandidateAverageLatencyMs = 0,
        long BaselineAverageLatencyMs = 0,
        long AverageLatencyDeltaMs = 0,
        long CandidateP95LatencyMs = 0,
        long BaselineP95LatencyMs = 0,
        long P95LatencyDeltaMs = 0,
        double CandidateAverageScore = 0d,
        double BaselineAverageScore = 0d,
        double AverageScoreDelta = 0d,
        int RegressedScenarios = 0,
        IReadOnlyList<WorkflowScenarioDelta>? ScenarioDeltas = null);

    private sealed record WorkflowScenarioDelta(
        string ScenarioGroupId,
        double SuccessRateDelta,
        long AverageLatencyDeltaMs,
        double AverageScoreDelta);

    private sealed record WorkflowGatePolicy
    {
        public string? Name { get; init; }
        public string? BenchmarkSet { get; init; }
        public double? MinSuccessRateDelta { get; init; }
        public long? MaxP95LatencyRegressionMs { get; init; }
        public long? MaxAverageLatencyRegressionMs { get; init; }
        public double? MinAverageScoreDelta { get; init; }
        public int? MaxRegressedScenarios { get; init; }
    }

    private sealed record GatePolicyLoadResult(
        bool Ok,
        WorkflowGatePolicy? Policy,
        string? Error);

    private sealed record OptimizeCandidatePlan(
        string CandidateId,
        WorkflowLabRequestSpec Request,
        WorkflowLabCompositionSpec Composition,
        WorkflowLabModelProfileSpec Profile,
        IReadOnlyList<ScenarioPlan> Plans,
        bool Synthesized = false,
        string? SynthesisRationale = null);

    private sealed record ScenarioPlan(
        WorkflowLabRequestSpec Request,
        WorkflowLabCompositionSpec Composition,
        WorkflowLabModelProfileSpec Profile,
        int Iteration);

    private sealed class OptimizeCandidateRuntimeState
    {
        public OptimizeCandidateRuntimeState(
            OptimizeCandidatePlan candidate,
            string candidateRunId,
            string? profileProvider,
            IReadOnlyList<string> requiredModels,
            ModelPullResult pullResult,
            IReadOnlyList<ScenarioPlan> plans)
        {
            Candidate = candidate;
            CandidateRunId = candidateRunId;
            ProfileProvider = profileProvider;
            RequiredModels = requiredModels;
            PullResult = pullResult;
            Plans = plans;
        }

        public OptimizeCandidatePlan Candidate { get; }
        public string CandidateRunId { get; }
        public string? ProfileProvider { get; }
        public IReadOnlyList<string> RequiredModels { get; }
        public ModelPullResult PullResult { get; }
        public IReadOnlyList<ScenarioPlan> Plans { get; }
        public int NextPlanIndex { get; set; }
        public bool EarlyStopped { get; set; }
        public List<WorkflowStressRunRecord> Runs { get; } = new();
    }

    private sealed class TargetExecutionStats
    {
        public int Runs { get; set; }
        public int Successes { get; set; }
        public long TotalLatencyMs { get; set; }
    }

    private sealed record OptimizeAllocationTrace(
        int RunIndex,
        string CandidateId,
        string TargetId,
        bool Success,
        long LatencyMs,
        string Reason);

    private sealed record TargetAllocationStat(
        string TargetId,
        int Runs,
        int Successes,
        double SuccessRate,
        long AverageLatencyMs);

    private sealed record CandidateAllocationStat(
        string CandidateId,
        int Runs,
        int Successes,
        double SuccessRate,
        long AverageLatencyMs,
        int ObjectiveScore,
        bool Synthesized);

    private sealed record RuntimeTelemetry(
        long CpuTimeDeltaMs,
        long WorkingSetMb,
        long PrivateMemoryMb,
        long ManagedMemoryMb,
        int ThreadCount,
        string HardwareProfile);

    internal sealed record ModelPullResult(
        bool Ok,
        string Summary,
        IReadOnlyList<string> Models,
        IReadOnlyList<string> PulledModels,
        IReadOnlyList<string>? FailedModels = null);

    internal sealed record ScenarioExecutionResult(
        bool Ok,
        string Summary,
        int ConflictCount,
        int EscalationCount,
        bool Skipped = false,
        string FailureCategory = "none");

    private sealed record PreflightResult(
        bool Ok,
        string Provider,
        string Detail);
}
