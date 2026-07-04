namespace Nexo.CLI.Commands.Workflow;

internal sealed record GatePolicyLoadResult(
    bool Ok,
    WorkflowGatePolicy? Policy,
    string? Error);
