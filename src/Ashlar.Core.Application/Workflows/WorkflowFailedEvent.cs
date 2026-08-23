namespace Ashlar.Core.Application.Workflows;

/// <summary>Emitted when workflow execution fails with an unhandled exception.</summary>
/// <param name="CorrelationId">Workflow run correlation id.</param>
/// <param name="Exception">Failure exception.</param>
public record WorkflowFailedEvent(string CorrelationId, Exception Exception)
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);
