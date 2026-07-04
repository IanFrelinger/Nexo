using Nexo.Core.Domain.Workflows;

namespace Nexo.Core.Application.Workflows;

/// <summary>Emitted immediately before a workflow node begins execution.</summary>
/// <param name="CorrelationId">Workflow run correlation id.</param>
/// <param name="Node">Node about to execute.</param>
public record NodeStartedEvent(string CorrelationId, WorkflowNode Node)
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);
