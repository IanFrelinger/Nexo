namespace Nexo.Core.Application.Workflows;

/// <summary>Base event streamed by <see cref="WorkflowExecutor"/> during execution.</summary>
/// <param name="CorrelationId">Workflow run correlation id.</param>
/// <param name="Timestamp">UTC timestamp when the event was emitted.</param>
public abstract record WorkflowExecutionEvent(string CorrelationId, DateTimeOffset Timestamp);
