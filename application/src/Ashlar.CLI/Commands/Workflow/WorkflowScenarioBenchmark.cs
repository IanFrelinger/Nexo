namespace Ashlar.CLI.Commands.Workflow;

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
    /// <summary>Last failure summary.</summary>
    public string? LastFailureSummary { get; init; }
}
