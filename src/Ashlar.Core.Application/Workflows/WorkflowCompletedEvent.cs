namespace Ashlar.Core.Application.Workflows;

/// <summary>Emitted when workflow execution completes successfully.</summary>
/// <param name="CorrelationId">Workflow run correlation id.</param>
/// <param name="Result">Final workflow result payload.</param>
public record WorkflowCompletedEvent(string CorrelationId, WorkflowResult Result)
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);
