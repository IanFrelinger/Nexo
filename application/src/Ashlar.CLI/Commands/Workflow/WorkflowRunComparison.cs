namespace Ashlar.CLI.Commands.Workflow;

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
