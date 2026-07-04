namespace Nexo.CLI.Commands.Workflow;

internal sealed record WorkflowScenarioDelta(
    string ScenarioGroupId,
    double SuccessRateDelta,
    long AverageLatencyDeltaMs,
    double AverageScoreDelta);
