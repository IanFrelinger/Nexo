namespace Ashlar.CLI.Commands.Workflow;

internal sealed record WorkflowStressAggregate(
    string ScenarioGroupId,
    string RequestId,
    string CompositionId,
    string ModelProfileId,
    int Runs,
    int Successes,
    int Failures,
    long AverageElapsedMs,
    double AverageScore);
