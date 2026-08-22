using Ashlar.Core.Domain.Execution.Events;

namespace Ashlar.Core.Application.Workflows;

/// <summary>Forwards a behavior-level <see cref="ExecutionEvent"/> from an agent node.</summary>
/// <param name="CorrelationId">Workflow run correlation id.</param>
/// <param name="NodeId">Agent node id emitting the nested event.</param>
/// <param name="BrickEvent">Underlying behavior execution event.</param>
public record NodeBrickEvent(string CorrelationId, string NodeId, ExecutionEvent BrickEvent)
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);
