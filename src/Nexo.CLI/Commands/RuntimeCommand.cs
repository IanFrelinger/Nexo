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
        var qaPolicyOpt = new Option<string>("--qa-policy", () => "auto", "QA policy: auto | demo | prod | research.");
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
        var qaPolicyOpt = new Option<string>("--qa-policy", () => "auto", "QA policy: auto | demo | prod | research.");
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
                ctx.ParseResult.GetValueForOption(jsonOpt),
                ctx.GetCancellationToken()).ConfigureAwait(false);
        });
        AddCommand(evaluateCmd);
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
            ct).ConfigureAwait(false);
        WriteResult(result, json);
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
            WritePlanResult(new RuntimePlanResult(false, "Goal is required."), json);
            return Task.FromResult(1);
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WritePlanResult(new RuntimePlanResult(false, $"Repo root not found: {fullRepoRoot}"), json);
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
            WritePlanResult(new RuntimePlanResult(
                true,
                "Plan computed successfully.",
                context.Plan,
                context.WorkflowSpec,
                context.AdaptivePolicyReason), json);
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            WritePlanResult(new RuntimePlanResult(false, $"Failed to compute plan: {ex.Message}"), json);
            return Task.FromResult(1);
        }
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
        bool json,
        CancellationToken ct)
    {
        var goals = ResolveGoals(goalsJson, goalsFile);
        if (goals.Length == 0)
        {
            WriteEvaluateResult(new RuntimeEvaluateResult(false, "No goals provided for evaluation."), json);
            return 1;
        }

        var policies = ResolvePolicies(policiesCsv);
        if (policies.Length == 0)
        {
            WriteEvaluateResult(new RuntimeEvaluateResult(false, "No valid policies provided for evaluation."), json);
            return 1;
        }

        var fullRepoRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(repoRoot) ? Environment.CurrentDirectory : repoRoot);
        if (!Directory.Exists(fullRepoRoot))
        {
            WriteEvaluateResult(new RuntimeEvaluateResult(false, $"Repo root not found: {fullRepoRoot}"), json);
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
        WriteEvaluateResult(result, json);
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

        var enrichedGoal = AdaptiveRuntimePlanResolver.EnrichGoal(goal, context.Manifest, context.Plan);

        var bootstrapBefore = await BootstrapRuntime.AssessDemoAsync(context.Plan.BootstrapProfile, includeOptional: false, ct).ConfigureAwait(false);
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
            bootstrapAfter = await BootstrapRuntime.AssessDemoAsync(context.Plan.BootstrapProfile, includeOptional: false, ct).ConfigureAwait(false);
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

        return ("dotnet", new[] { "run", "--project", "src/Nexo.CLI", "--" });
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

    private static void WriteResult(RuntimeExecuteResult result, bool json)
    {
        if (json)
        {
            var payload = new
            {
                runId = result.RunId,
                startedAtUtc = result.StartedAtUtc,
                elapsedMs = result.ElapsedMs,
                failureStage = result.FailureStage,
                ok = result.Ok,
                summary = result.Summary,
                requestedQaPolicy = result.RequestedQaPolicy,
                resolvedQaPolicy = result.ResolvedQaPolicy,
                adaptivePolicyReason = result.AdaptivePolicyReason,
                plan = result.Plan == null
                    ? null
                    : new
                    {
                        bootstrapProfile = result.Plan.BootstrapProfile,
                        qaPolicy = result.Plan.QaPolicyProfile,
                        focus = result.Plan.Focus,
                        maxIterations = result.Plan.MaxIterations,
                        stopOnFirstPass = result.Plan.StopOnFirstPass,
                        runFunctionalQa = result.Plan.RunFunctionalQa,
                        runAestheticQa = result.Plan.RunAestheticQa,
                        runVisualQa = result.Plan.RunVisualQa,
                        visualFallback = result.Plan.VisualQaFallbackPolicy,
                        reasons = result.Plan.Reasons
                    },
                bootstrap = result.BootstrapAfter == null
                    ? null
                    : new
                    {
                        applied = result.BootstrapApplied,
                        applyExit = result.BootstrapApplyExit,
                        ok = result.BootstrapOk,
                        missingRequired = result.BootstrapAfter.MissingRequired.Select(m => m.Id).ToArray()
                    },
                gates = new
                {
                    preflightRan = result.PreflightRan,
                    preflightOk = result.PreflightOk,
                    selfExtendRan = result.SelfExtendRan,
                    selfExtendOk = result.SelfExtendOk
                },
                preflight = result.PreflightPayload,
                execution = result.SelfExtendPayload
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"runtime execute: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.FailureStage) && !string.Equals(result.FailureStage, "none", StringComparison.OrdinalIgnoreCase))
            Console.WriteLine($"  failure-stage={result.FailureStage}");
        if (result.Plan != null)
        {
            Console.WriteLine($"  profile={result.Plan.BootstrapProfile}, policy={result.Plan.QaPolicyProfile}, focus={result.Plan.Focus}");
            Console.WriteLine($"  visual-policy={result.Plan.VisualQaFallbackPolicy}, run-visual={result.Plan.RunVisualQa}");
        }
        if (result.BootstrapAfter != null)
            Console.WriteLine($"  bootstrap missing-required={result.BootstrapAfter.MissingRequired.Count()}");
    }

    private static void WritePlanResult(RuntimePlanResult result, bool json)
    {
        if (json)
        {
            var payload = new
            {
                ok = result.Ok,
                summary = result.Summary,
                adaptivePolicyReason = result.AdaptivePolicyReason,
                plan = result.Plan == null
                    ? null
                    : new
                    {
                        bootstrapProfile = result.Plan.BootstrapProfile,
                        qaPolicy = result.Plan.QaPolicyProfile,
                        focus = result.Plan.Focus,
                        maxIterations = result.Plan.MaxIterations,
                        stopOnFirstPass = result.Plan.StopOnFirstPass,
                        runFunctionalQa = result.Plan.RunFunctionalQa,
                        runAestheticQa = result.Plan.RunAestheticQa,
                        runVisualQa = result.Plan.RunVisualQa,
                        visualFallback = result.Plan.VisualQaFallbackPolicy,
                        reasons = result.Plan.Reasons
                    },
                workflow = result.WorkflowSpec?.Workflow
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"runtime plan: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        if (result.Plan != null)
        {
            Console.WriteLine($"  profile={result.Plan.BootstrapProfile}");
            Console.WriteLine($"  qa-policy={result.Plan.QaPolicyProfile}");
            Console.WriteLine($"  focus={result.Plan.Focus}");
            Console.WriteLine($"  visual-policy={result.Plan.VisualQaFallbackPolicy}, run-visual={result.Plan.RunVisualQa}");
        }
        if (!string.IsNullOrWhiteSpace(result.AdaptivePolicyReason))
            Console.WriteLine($"  adaptive-policy={result.AdaptivePolicyReason}");
    }

    private static void WriteEvaluateResult(RuntimeEvaluateResult result, bool json)
    {
        if (json)
        {
            var payload = new
            {
                ok = result.Ok,
                summary = result.Summary,
                scenarios = result.Scenarios,
                policies = result.PolicySummaries
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        Console.WriteLine($"runtime evaluate: {(result.Ok ? "ok" : "failed")}");
        Console.WriteLine(result.Summary);
        foreach (var policy in result.PolicySummaries ?? Array.Empty<RuntimeEvaluatePolicySummary>())
            Console.WriteLine($"  {policy.Policy}: {policy.Passed}/{policy.Total} passed, avg={policy.AverageElapsedMs}ms");
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
            .Where(p => p is "demo" or "prod" or "research" or "auto")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeQaPolicy(string? qaPolicy)
    {
        var normalized = (qaPolicy ?? "auto").Trim().ToLowerInvariant();
        return normalized switch
        {
            "demo" => "demo",
            "prod" => "prod",
            "research" => "research",
            _ => "auto"
        };
    }

    private sealed record RuntimeSubprocessResult(int ExitCode, string StdOut, string StdErr);

    private sealed record RuntimeExecuteResult(
        bool Ok,
        string Summary,
        AdaptiveRuntimeExecutionPlan? Plan = null,
        BootstrapAssessment? BootstrapBefore = null,
        BootstrapAssessment? BootstrapAfter = null,
        bool BootstrapApplied = false,
        int? BootstrapApplyExit = null,
        JsonElement? PreflightPayload = null,
        RuntimeSubprocessResult? PreflightRun = null,
        JsonElement? SelfExtendPayload = null,
        RuntimeSubprocessResult? SelfExtendRun = null,
        string? RequestedQaPolicy = null,
        string? ResolvedQaPolicy = null,
        string? AdaptivePolicyReason = null,
        string? RepoRoot = null,
        string? FailureStage = null,
        bool? BootstrapOk = null,
        bool? PreflightRan = null,
        bool? PreflightOk = null,
        bool? SelfExtendRan = null,
        bool? SelfExtendOk = null,
        string? RunId = null,
        DateTimeOffset? StartedAtUtc = null,
        long? ElapsedMs = null,
        string? GoalFingerprint = null,
        string? GoalPreview = null);

    private sealed record RuntimePlanResult(
        bool Ok,
        string Summary,
        AdaptiveRuntimeExecutionPlan? Plan = null,
        SelfExtendWorkflowRuntimeSpec? WorkflowSpec = null,
        string? AdaptivePolicyReason = null);

    private sealed record RuntimePlanContext(
        string Goal,
        AdaptiveRuntimeManifest Manifest,
        AdaptiveRuntimeExecutionPlan Plan,
        SelfExtendWorkflowRuntimeSpec WorkflowSpec,
        string RequestedQaPolicy,
        string? AdaptivePolicyReason);

    private sealed record RuntimeEvaluateScenarioResult(
        string Goal,
        string Policy,
        bool Ok,
        long? ElapsedMs,
        string? FailureStage,
        string Summary);

    private sealed record RuntimeEvaluatePolicySummary(
        string Policy,
        int Total,
        int Passed,
        int Failed,
        long AverageElapsedMs);

    private sealed record RuntimeEvaluateResult(
        bool Ok,
        string Summary,
        IReadOnlyList<RuntimeEvaluateScenarioResult>? Scenarios = null,
        IReadOnlyList<RuntimeEvaluatePolicySummary>? PolicySummaries = null);
}
