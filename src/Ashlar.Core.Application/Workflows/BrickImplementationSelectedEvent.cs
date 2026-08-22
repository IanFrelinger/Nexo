using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Application.Workflows;

/// <summary>Emitted when a brick node selects an implementation from its fallback chain.</summary>
/// <param name="CorrelationId">Workflow run correlation id.</param>
/// <param name="NodeId">Brick node id.</param>
/// <param name="BrickName">Human-readable brick name.</param>
/// <param name="Implementation">Selected implementation type.</param>
public record BrickImplementationSelectedEvent(
    string CorrelationId,
    string NodeId,
    string BrickName,
    ImplementationType Implementation)
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);
