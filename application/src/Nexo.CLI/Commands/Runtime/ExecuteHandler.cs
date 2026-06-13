using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Nexo.CLI.Runtime;

namespace Nexo.CLI.Commands;

internal sealed class ExecuteHandler(
    Func<string, string, string, string, string, string?, string?, int?, bool, int, RuntimePlanContext> buildPlanContext)
{
    public async Task<int> ExecuteAsync(
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


    internal async Task<RuntimeExecuteResult> ExecuteCoreAsync(
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
                            BenchmarkSet = RuntimeCommandUtilities.NormalizeBenchmarkSet(benchmarkSet),
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
            context = buildPlanContext(
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


}
