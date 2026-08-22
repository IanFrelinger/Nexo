namespace Ashlar.CLI.Commands.Workflow;

internal sealed record WorkflowRecommendation(
    string Kind,
    string Action,
    string? Target,
    string Rationale);
