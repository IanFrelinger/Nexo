using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Ashlar.CLI.Runtime;
using Ashlar.Infrastructure.HostProcess;

namespace Ashlar.CLI.Commands.Runtime;
/// <summary>Handles execute requests.</summary>
internal sealed partial class ExecuteHandler(
    Func<string, string, string, string, string, string?, string?, int?, bool, int, RuntimePlanContext> buildPlanContext)
{
    /// <summary>Executes the command handler and returns a process exit code.</summary>
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
        if (!RuntimeCommandUtilities.TryValidateNonNegativeCount(maxRemediationAttempts))
        {
            RuntimeOutputWriter.WriteResult(
                new RuntimeExecuteResult(false, RuntimeCommandUtilities.InvalidMaxRemediationAttemptsMessage, FailureStage: "input"),
                json);
            return 1;
        }

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

        var result = await TimedProcess.RunAsync(psi, TimedProcess.DaemonProbeTimeout, ct).ConfigureAwait(false);
        if (result.TimedOut)
        {
            var timeoutNote = $"Probe timed out after {TimedProcess.DaemonProbeTimeout.TotalSeconds:0}s.";
            var stderr = string.IsNullOrWhiteSpace(result.StdErr)
                ? timeoutNote
                : timeoutNote + " " + result.StdErr.Trim();
            return new RuntimeSubprocessResult(TimedProcess.TimeoutExitCode, result.StdOut, stderr);
        }

        return new RuntimeSubprocessResult(result.ExitCode, result.StdOut, result.StdErr);
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

        var run = await TimedProcess.RunAsync(psi, TimedProcess.OperatorCommandTimeout, ct)
            .ConfigureAwait(false);
        return new RuntimeSubprocessResult(run.ExitCode, run.StdOut, run.StdErr);
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

        return ("dotnet", new[] { "run", "--project", "application/src/Ashlar.CLI", "--" });
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
