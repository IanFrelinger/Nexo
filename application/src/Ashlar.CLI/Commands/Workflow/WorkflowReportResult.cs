namespace Ashlar.CLI.Commands.Workflow;

internal sealed record WorkflowReportResult(
    bool Ok,
    string Summary,
    WorkflowBenchmarkReport Report,
    string? OutputPath = null,
    WorkflowRunComparison? Comparison = null);
