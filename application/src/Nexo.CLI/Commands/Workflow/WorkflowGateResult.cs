namespace Nexo.CLI.Commands.Workflow;

internal sealed record WorkflowGateResult(
    bool Ok,
    bool Passed,
    string Summary,
    IReadOnlyList<string>? Failures = null,
    WorkflowRunComparison? Comparison = null);
