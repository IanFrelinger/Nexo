namespace Nexo.CLI.Commands;

internal sealed record WorkflowGatePolicy
{
    public string? Name { get; init; }
    public string? BenchmarkSet { get; init; }
    public double? MinSuccessRateDelta { get; init; }
    public long? MaxP95LatencyRegressionMs { get; init; }
    public long? MaxAverageLatencyRegressionMs { get; init; }
    public double? MinAverageScoreDelta { get; init; }
    public int? MaxRegressedScenarios { get; init; }
}

internal sealed record GatePolicyLoadResult(
    bool Ok,
    WorkflowGatePolicy? Policy,
    string? Error);

internal sealed record WorkflowScenarioBenchmark(
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

internal sealed record WorkflowFailureCategoryStat(
    string Category,
    int Count);

internal sealed record WorkflowBenchmarkReport(
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

internal sealed record WorkflowRecommendation(
    string Kind,
    string Action,
    string? Target,
    string Rationale);

internal sealed record WorkflowReportResult(
    bool Ok,
    string Summary,
    WorkflowBenchmarkReport Report,
    string? OutputPath = null,
    WorkflowRunComparison? Comparison = null);

internal sealed record WorkflowRunComparison(
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

internal sealed record WorkflowScenarioDelta(
    string ScenarioGroupId,
    double SuccessRateDelta,
    long AverageLatencyDeltaMs,
    double AverageScoreDelta);
