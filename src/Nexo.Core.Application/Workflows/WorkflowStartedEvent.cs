namespace Nexo.Core.Application.Workflows;

/// <summary>Emitted when workflow execution begins.</summary>
/// <param name="CorrelationId">Workflow run correlation id.</param>
/// <param name="WorkflowName">Display name of the workflow definition.</param>
public record WorkflowStartedEvent(string CorrelationId, string WorkflowName)
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);
