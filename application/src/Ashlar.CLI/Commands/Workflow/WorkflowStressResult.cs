namespace Ashlar.CLI.Commands.Workflow;

internal sealed record WorkflowStressResult(
    bool Ok,
    string Summary,
    IReadOnlyList<WorkflowStressRunRecord>? Runs = null,
    IReadOnlyList<WorkflowStressAggregate>? Aggregates = null,
    WorkflowStressAggregate? Best = null,
    string? RunId = null,
    string? BenchmarkSet = null,
    bool? PersistHistory = null);
