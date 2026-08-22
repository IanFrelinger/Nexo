using Ashlar.Core.Domain.Workflows;

namespace Ashlar.Core.Application.Workflows;

/// <summary>Emitted after a workflow node finishes execution.</summary>
/// <param name="CorrelationId">Workflow run correlation id.</param>
/// <param name="Node">Node that completed.</param>
/// <param name="Result">Node execution result.</param>
public record NodeCompletedEvent(string CorrelationId, WorkflowNode Node, NodeResult Result)
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);
