using System.Text.Json;

namespace Nexo.CLI.Commands.Workflow;
/// <summary>Handles optimize requests.</summary>
internal sealed partial class OptimizeHandler
{
    private static List<WorkflowOptimizeCandidate> BuildCandidateResults(
        IReadOnlyList<OptimizeCandidateRuntimeState> candidateStates,
        HashSet<string> objectiveKeywordSet)
    {
        var candidates = new List<WorkflowOptimizeCandidate>();
        foreach (var candidateState in candidateStates.Where(x => x.Runs.Count > 0))
        {
            var successCount = candidateState.Runs.Count(x => x.Success);
            var failureCount = candidateState.Runs.Count(x => !x.Success && !x.Skipped);
            var skippedCount = candidateState.Runs.Count(x => x.Skipped);
            var successRate = candidateState.Runs.Count == 0 ? 0d : Math.Round((double)successCount / candidateState.Runs.Count, 4);
            var avgLatency = candidateState.Runs.Count == 0
                ? 0L
                : (long)Math.Round(candidateState.Runs.Select(x => (double)x.ElapsedMs).DefaultIfEmpty(0d).Average());
            var p95Latency = candidateState.Runs.Count == 0 ? 0L : WorkflowCommandUtilities.ComputePercentile(candidateState.Runs.Select(x => x.ElapsedMs), 0.95);
            var avgScore = candidateState.Runs.Count == 0
                ? 0d
                : Math.Round(candidateState.Runs.Select(x => x.Score).DefaultIfEmpty(0d).Average(), 3);
            var avgCpuDelta = candidateState.Runs.Count == 0
                ? 0L
                : (long)Math.Round(candidateState.Runs.Select(x => (double)x.CpuTimeDeltaMs).DefaultIfEmpty(0d).Average());
            var p95WorkingSet = candidateState.Runs.Count == 0 ? 0L : WorkflowCommandUtilities.ComputePercentile(candidateState.Runs.Select(x => x.WorkingSetMb), 0.95);
            var p95PrivateMemory = candidateState.Runs.Count == 0 ? 0L : WorkflowCommandUtilities.ComputePercentile(candidateState.Runs.Select(x => x.PrivateMemoryMb), 0.95);
            var p95ManagedMemory = candidateState.Runs.Count == 0 ? 0L : WorkflowCommandUtilities.ComputePercentile(candidateState.Runs.Select(x => x.ManagedMemoryMb), 0.95);
            var maxThreadCount = candidateState.Runs.Count == 0 ? 0 : candidateState.Runs.Max(x => x.ThreadCount);
            var hardwareProfile = candidateState.Runs
                .Select(x => x.HardwareProfile)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Key)
                .FirstOrDefault() ?? "unknown";
            var candidateObjectiveScore = WorkflowCommandUtilities.ScoreCandidateForObjective(candidateState.Candidate, objectiveKeywordSet);
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


        return candidates;
    }


    private static async Task<List<OptimizeCandidateRuntimeState>> BuildCandidateStatesAsync(
        IReadOnlyList<OptimizeCandidatePlan> groupedCandidates,
        string optimizeRunId,
        string strategy,
        bool autoPullModels,
        Func<IReadOnlyList<string>, CancellationToken, Task<WorkflowCommand.ModelPullResult>> ollamaModelPuller,
        CancellationToken ct)
    {
        var candidateStates = new List<OptimizeCandidateRuntimeState>();
        for (var candidateIndex = 0; candidateIndex < groupedCandidates.Count; candidateIndex++)
        {
            var candidate = groupedCandidates[candidateIndex];
            var profileProvider = candidate.Profile.Default.Provider?.Trim();
            var requiredModels = WorkflowOptimizeReportRenderer.ResolveOllamaModelsForCandidate(candidate.Composition, candidate.Profile);
            var pullResult = autoPullModels
                ? await ollamaModelPuller(requiredModels, ct).ConfigureAwait(false)
                : new WorkflowCommand.ModelPullResult(
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

        return candidateStates;
    }

    private static IReadOnlyList<WorkflowOptimizeRecommendation> BuildRecommendations(
        IReadOnlyList<WorkflowOptimizeCandidate> ranked,
        WorkflowOptimizeCandidate winner,
        bool winnerHasMinimumSamples,
        int minimumPromotionSamples,
        double winnerConfidence,
        double promotionConfidenceThreshold)
    {
        var recommendations = WorkflowOptimizeReportRenderer.BuildOptimizeRecommendations(ranked);
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

        return recommendations;
    }

    private static void WriteResult(WorkflowOptimizeResult result, bool json)
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
}
