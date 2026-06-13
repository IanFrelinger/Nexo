using System.Text;
using System.Text.Json;
using Nexo.CLI.Runtime;
using Nexo.Orchestration.Models;

namespace Nexo.CLI.Commands;

internal sealed record WorkflowOptimizeCandidate(
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

internal sealed record WorkflowOptimizeRecommendation(
    string Kind,
    string Action,
    string CandidateId,
    string Rationale);

internal sealed record OptimizeCandidatePlan(
    string CandidateId,
    WorkflowLabRequestSpec Request,
    WorkflowLabCompositionSpec Composition,
    WorkflowLabModelProfileSpec Profile,
    IReadOnlyList<ScenarioPlan> Plans,
    bool Synthesized = false,
    string? SynthesisRationale = null);

internal sealed record ScenarioPlan(
    WorkflowLabRequestSpec Request,
    WorkflowLabCompositionSpec Composition,
    WorkflowLabModelProfileSpec Profile,
    int Iteration);

internal sealed record OptimizeAllocationTrace(
    int RunIndex,
    string CandidateId,
    string TargetId,
    bool Success,
    long LatencyMs,
    string Reason);

internal sealed record TargetAllocationStat(
    string TargetId,
    int Runs,
    int Successes,
    double SuccessRate,
    long AverageLatencyMs);

internal sealed record CandidateAllocationStat(
    string CandidateId,
    int Runs,
    int Successes,
    double SuccessRate,
    long AverageLatencyMs,
    int ObjectiveScore,
    bool Synthesized);

internal static class WorkflowOptimizeReportRenderer
{
    internal static IReadOnlyList<WorkflowOptimizeRecommendation> BuildOptimizeRecommendations(
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


    internal static double ComputePromotionConfidence(
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


    internal static IReadOnlyList<TargetAllocationStat> BuildTargetAllocations(
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


    internal static IReadOnlyList<CandidateAllocationStat> BuildCandidateAllocations(
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


    internal static IReadOnlyList<string> ResolveOllamaModelsForCandidate(
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


    internal static string ResolveOptimizeReportPath(string? reportOutputPath)
    {
        if (!string.IsNullOrWhiteSpace(reportOutputPath))
            return Path.GetFullPath(reportOutputPath);

        return Path.Combine(
            Environment.CurrentDirectory,
            ".nexo",
            "workflow",
            "optimize_recommendation_report.md");
    }


    internal static string RenderOptimizationRecommendationContent(
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


    internal static string RenderOptimizationRecommendationMarkdown(
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


    internal static string RenderOptimizationRecommendationText(
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


    internal static void ShuffleOptimizeCandidates(IReadOnlyList<OptimizeCandidatePlan> candidates, Random rng)
    {
        if (candidates is not List<OptimizeCandidatePlan> list)
            return;

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }


    internal static string BuildExecutionRequest(
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


    internal static bool TryNormalizeMeshEndpoint(string endpoint, out string normalizedEndpoint)
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


    internal static string NormalizeScenarioTargetSegment(string targetId)
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

    private static string? ResolveRoleModel(WorkflowLabModelProfileSpec profile, WorkflowLabAgentRoleSpec role)
    {
        if (!string.IsNullOrWhiteSpace(role.OllamaModel))
            return role.OllamaModel.Trim();
        if (profile.AgentModelHints.TryGetValue(role.AgentId, out var hint) && !string.IsNullOrWhiteSpace(hint))
            return hint.Trim();
        return null;
    }

}
