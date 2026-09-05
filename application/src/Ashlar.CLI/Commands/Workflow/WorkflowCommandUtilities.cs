using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ashlar.CLI.Commands;
using Ashlar.CLI.Runtime;
using Ashlar.Orchestration.Models;
using Process = System.Diagnostics.Process;

namespace Ashlar.CLI.Commands.Workflow;
/// <summary>Workflow command utilities.</summary>
internal static class WorkflowCommandUtilities
{
    internal static string ClassifyFailureCategory(string output, string? parsedErrorCode = null)
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


    internal static OrchestrationRuntimeSpec BuildRuntimeSpec(
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


    internal static WorkflowLabRequestSpec[] NormalizeRequests(IReadOnlyList<WorkflowLabRequestSpec> requests)
    {
        return (requests ?? Array.Empty<WorkflowLabRequestSpec>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .Where(x => !string.IsNullOrWhiteSpace(x.Prompt))
            .Select(x => x with { Id = x.Id.Trim(), Prompt = x.Prompt.Trim() })
            .ToArray();
    }


    internal static WorkflowLabCompositionSpec[] NormalizeCompositions(IReadOnlyList<WorkflowLabCompositionSpec> compositions)
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


    internal static WorkflowLabModelProfileSpec[] NormalizeProfiles(
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


    internal static bool TryValidateWorkflowLabPrefers(WorkflowLabRuntimeSpec spec, out string error)
    {
        error = OrchestrateCommand.InvalidRuntimeSpecPreferMessage;
        if (spec is null)
            return false;

        foreach (var profile in spec.ModelProfiles ?? Array.Empty<WorkflowLabModelProfileSpec>())
        {
            if (!OrchestrateCommand.TryNormalizePreferModel(profile.Default?.Prefer, out _))
                return false;

            if (profile.Domains != null)
            {
                foreach (var entry in profile.Domains.Values)
                {
                    if (!OrchestrateCommand.TryNormalizePreferModel(entry.Prefer, out _))
                        return false;
                }
            }

            if (profile.Agents != null)
            {
                foreach (var entry in profile.Agents.Values)
                {
                    if (!OrchestrateCommand.TryNormalizePreferModel(entry.Prefer, out _))
                        return false;
                }
            }
        }

        foreach (var composition in spec.Compositions ?? Array.Empty<WorkflowLabCompositionSpec>())
        {
            foreach (var role in composition.Roles ?? Array.Empty<WorkflowLabAgentRoleSpec>())
            {
                if (!string.IsNullOrWhiteSpace(role.Prefer) &&
                    !OrchestrateCommand.TryNormalizePreferModel(role.Prefer, out _))
                    return false;
            }
        }

        error = string.Empty;
        return true;
    }


    internal static ModelRuntimeSpec ApplyOverrides(ModelRuntimeSpec runtime, string? provider, string? prefer)
    {
        var preferToApply = !string.IsNullOrWhiteSpace(prefer) ? prefer : runtime.Prefer;
        if (!OrchestrateCommand.TryNormalizePreferModel(preferToApply, out var normalizedPrefer))
        {
            throw new ArgumentException(
                !string.IsNullOrWhiteSpace(prefer)
                    ? OrchestrateCommand.InvalidPreferMessage
                    : OrchestrateCommand.InvalidRuntimeSpecPreferMessage);
        }

        var updated = runtime with { Prefer = normalizedPrefer };
        if (!string.IsNullOrWhiteSpace(provider))
            updated = updated with { Provider = provider };
        return updated;
    }


    internal static string ResolveDefaultSpecPath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return Path.GetFullPath(explicitPath);
        return Path.Combine(Environment.CurrentDirectory, ".ashlar", "workflow", "workflow_lab.runtime.json");
    }


    internal static string BuildRunId()
        => DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];


    internal static string ResolveGitSha()
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


    internal static string ComputeSpecHash(string raw)
    {
        var text = raw ?? string.Empty;
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }


    internal static string BuildProviderSnapshot(IReadOnlyList<WorkflowLabModelProfileSpec> profiles)
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


    internal static string BuildScenarioId(string requestId, string compositionId, string profileId, int iteration)
        => $"{requestId}::{compositionId}::{profileId}::iter-{iteration}";


    internal static string NormalizeBenchmarkSet(string? benchmarkSetOverride, string defaultValue)
    {
        var value = string.IsNullOrWhiteSpace(benchmarkSetOverride) ? defaultValue : benchmarkSetOverride;
        var normalized = (value ?? "workflow-lab").Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalized) ? "workflow-lab" : normalized;
    }


    internal static GatePolicyLoadResult LoadGatePolicy(string? policyFile)
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


    internal static string BuildBaselineId(string benchmarkSet, string runId)
    {
        var prefix = NormalizeBenchmarkSet(benchmarkSet, "workflow-lab").Replace(' ', '-');
        var suffix = string.IsNullOrWhiteSpace(runId) ? Guid.NewGuid().ToString("N")[..8] : runId.Trim();
        return $"{prefix}-{suffix}";
    }


    internal static RuntimeTelemetry CaptureRuntimeTelemetry(DateTimeOffset startedAtUtc, TimeSpan cpuStart)
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


    internal static string ResolveHardwareProfile(Process process)
    {
        var cpu = Environment.ProcessorCount;
        var memoryMb = BytesToMb(process.WorkingSet64);
        return $"cpu:{cpu}|ws:{memoryMb}mb";
    }


    internal static long BytesToMb(long bytes)
    {
        if (bytes <= 0)
            return 0;
        return (long)Math.Round(bytes / (1024d * 1024d));
    }


    internal static WorkflowBenchmarkReport BuildBenchmarkReport(IReadOnlyList<WorkflowLabStressHistoryRow> rows)
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


    internal static IReadOnlyList<WorkflowRecommendation> CreateRecommendations(
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


    internal static long ComputePercentile(IEnumerable<long> source, double percentile)
    {
        var ordered = source.OrderBy(x => x).ToArray();
        if (ordered.Length == 0)
            return 0;
        var clamped = Math.Clamp(percentile, 0d, 1d);
        var index = (int)Math.Ceiling((ordered.Length - 1) * clamped);
        return ordered[index];
    }


    internal static IReadOnlyList<ScenarioPlan> BuildScenarioPlans(
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


    internal static void ShuffleScenarioPlans(IReadOnlyList<ScenarioPlan> plans, Random rng)
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


    internal static WorkflowRunComparison BuildComparison(
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


    internal static string RenderReportContent(WorkflowReportResult result, bool preferJson, string outputPath)
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


    internal static string RenderReportMarkdown(WorkflowReportResult result)
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


    internal static string RenderReportText(WorkflowReportResult result)
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


    internal static double ComputeScore(
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


    internal static string? ResolveObjectiveText(string? objective, string? objectiveFile)
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


    internal static HashSet<string> BuildObjectiveKeywordSet(string? objective)
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


    internal static List<OptimizeCandidatePlan> BuildObjectiveSynthesizedCandidates(
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


    internal static bool ShouldSynthesizeCandidates(HashSet<string> objectiveKeywords)
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


    internal static string BuildSynthesizedPrompt(string prompt, HashSet<string> objectiveKeywords)
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


    internal static WorkflowLabModelProfileSpec SynthesizeProfileForObjective(
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


    internal static string? ResolveObjectiveDefaultModelHint(HashSet<string> objectiveKeywords, int seedIndex)
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


    internal static string? ResolveObjectiveRoleModelHint(
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


    internal static string BuildSynthesisRationale(
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


    internal const string InvalidSearchStrategyMessage =
        "Invalid --search-strategy. Use successive-halving, objective-first, or exhaustive.";

    internal const string InvalidIterationsMessage = "Invalid --iterations. Use a positive integer.";

    internal const string InvalidLimitMessage = "Invalid --limit. Use a positive integer.";

    internal static bool TryNormalizeSearchStrategy(string? searchStrategy, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(searchStrategy))
        {
            normalized = "successive-halving";
            return true;
        }

        normalized = searchStrategy.Trim().ToLowerInvariant();
        return normalized is "successive-halving" or "objective-first" or "exhaustive";
    }

    internal static string NormalizeSearchStrategy(string? searchStrategy)
    {
        if (!TryNormalizeSearchStrategy(searchStrategy, out var normalized))
            throw new ArgumentException(InvalidSearchStrategyMessage);
        return normalized;
    }


    internal static List<OptimizeCandidatePlan> SortCandidatesForSearchStrategy(
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


    internal static int ScoreCandidateForObjective(OptimizeCandidatePlan candidate, HashSet<string> objectiveKeywords)
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


    internal static int CountMatches(string? text, HashSet<string> objectiveKeywords)
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


    internal static OptimizeCandidateRuntimeState? SelectNextCandidateState(
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


    internal static ExecutionTarget SelectExecutionTarget(
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


    internal static void UpdateTargetExecutionStats(
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


    internal static OptimizeCandidatePlan? BuildAdaptiveFollowUpCandidate(
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


    internal static string RenderComparisonText(WorkflowRunComparison comparison, string indent)
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


}
