namespace Nexo.Core.Application.Workflows;

/// <summary>Emitted after topological sorting produces a runnable execution plan.</summary>
/// <param name="CorrelationId">Workflow run correlation id.</param>
/// <param name="Plan">Resolved node execution order and parallel groups.</param>
public record ExecutionPlanCreatedEvent(string CorrelationId, ExecutionPlan Plan)
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);
