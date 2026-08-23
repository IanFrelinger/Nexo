using FluentAssertions;
using Ashlar.CLI.Commands;
using Ashlar.CLI.Commands.Workflow;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands.Workflow;

public sealed class WorkflowOptimizeReportRendererTests
{
    [Fact]
    public void RenderOptimizationRecommendationMarkdown_includes_winner_and_recommendations()
    {
        var winner = Candidate("candidate-a", successRate: 1.0, averageScore: 92);
        var recommendation = new WorkflowOptimizeRecommendation(
            "winner",
            "promote",
            winner.CandidateId,
            "Best candidate.");

        var markdown = WorkflowOptimizeReportRenderer.RenderOptimizationRecommendationMarkdown(
            sessionRunId: "opt-123",
            benchmarkSet: "default",
            ranked: [winner],
            winner: winner,
            recommendations: [recommendation],
            promotionSummary: "Promoted.",
            objective: "minimize latency",
            objectiveFile: null,
            searchStrategy: "successive-halving",
            measuredRunsUsed: 3,
            measuredRunBudget: 5,
            earlyStopMinRuns: 2,
            earlyStopMinSuccessRate: 0.8,
            synthesizedCandidateCount: 0,
            winnerConfidence: 0.92,
            promotionConfidenceThreshold: 0.6,
            allocationTrace: [new OptimizeAllocationTrace(1, winner.CandidateId, "local", true, 42, "base-candidate")],
            targetAllocations: [new TargetAllocationStat("local", 1, 1, 1.0, 42)],
            candidateAllocations: [new CandidateAllocationStat(winner.CandidateId, 1, 1, 1.0, 42, 3, false)]);

        markdown.Should().Contain("# Workflow Optimize Recommendation Report");
        markdown.Should().Contain("`candidate-a`");
        markdown.Should().Contain("Best candidate.");
        markdown.Should().Contain("Promotion");
    }

    [Fact]
    public void BuildOptimizeRecommendations_reports_winner_and_failed_model_pull()
    {
        var winner = Candidate("candidate-a", successRate: 1.0, averageScore: 90);
        var pullFailed = Candidate("candidate-b", successRate: 0.5, averageScore: 80, autoPullOk: false, failures: 1);

        var recommendations = WorkflowOptimizeReportRenderer.BuildOptimizeRecommendations([winner, pullFailed]);

        recommendations.Should().Contain(x => x.Kind == "winner" && x.Action == "promote" && x.CandidateId == "candidate-a");
        recommendations.Should().Contain(x => x.Kind == "infra-remediation" && x.CandidateId == "candidate-b");
        recommendations.Should().Contain(x => x.Kind == "avoid" && x.CandidateId == "candidate-b");
    }

    [Fact]
    public void ResolveOptimizeReportPath_uses_explicit_path_or_default_workflow_path()
    {
        var explicitPath = Path.Combine(Path.GetTempPath(), "ashlar-report.json");

        WorkflowOptimizeReportRenderer.ResolveOptimizeReportPath(explicitPath)
            .Should()
            .Be(Path.GetFullPath(explicitPath));

        WorkflowOptimizeReportRenderer.ResolveOptimizeReportPath(null)
            .Should()
            .EndWith(Path.Combine(".ashlar", "workflow", "optimize_recommendation_report.md"));
    }

    [Fact]
    public void TryNormalizeMeshEndpoint_accepts_http_authority_and_rejects_non_http()
    {
        WorkflowOptimizeReportRenderer.TryNormalizeMeshEndpoint(
                "https://peer.example.test:8443/api/mesh?x=1",
                out var normalized)
            .Should()
            .BeTrue();
        normalized.Should().Be("https://peer.example.test:8443");

        WorkflowOptimizeReportRenderer.TryNormalizeMeshEndpoint("file:///tmp/socket", out normalized)
            .Should()
            .BeFalse();
        normalized.Should().BeEmpty();
    }

    private static WorkflowOptimizeCandidate Candidate(
        string id,
        double successRate,
        double averageScore,
        bool autoPullOk = true,
        int failures = 0)
    {
        return new WorkflowOptimizeCandidate(
            CandidateId: id,
            RunId: $"{id}-run",
            RequestId: "request",
            CompositionId: "composition",
            ModelProfileId: "profile",
            TotalRuns: 3,
            Successes: failures == 0 ? 3 : 2,
            Failures: failures,
            Skipped: 0,
            SuccessRate: successRate,
            AverageLatencyMs: 42,
            P95LatencyMs: 64,
            AverageScore: averageScore,
            AverageCpuTimeDeltaMs: 5,
            P95WorkingSetMb: 128,
            P95PrivateMemoryMb: 96,
            P95ManagedMemoryMb: 32,
            MaxThreadCount: 8,
            HardwareProfile: "test",
            Models: ["llama3.1"],
            AutoPullSummary: autoPullOk ? "ok" : "failed",
            AutoPullOk: autoPullOk,
            Synthesized: false,
            SynthesisRationale: null,
            ObjectiveScore: 3);
    }
}
