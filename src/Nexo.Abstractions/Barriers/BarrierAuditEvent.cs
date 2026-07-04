namespace Nexo.Abstractions.Barriers;

/// <summary>
/// Structured barrier audit record emitted by the runtime when context is initialized,
/// identity is resolved, agents are invoked, or elevation/validation outcomes occur.
/// </summary>
/// <param name="EventType">Canonical event type; see <see cref="BarrierAuditEventType"/>.</param>
/// <param name="BarrierLevel">Active or attempted barrier level at the time of the event.</param>
/// <param name="AuthoritySource">Origin of the authority that established the barrier level.</param>
/// <param name="AgentName">Agent involved in the event, when applicable.</param>
/// <param name="CorrelationId">Request correlation id for distributed tracing.</param>
/// <param name="SpanId">Span id within the correlation scope, when available.</param>
/// <param name="OccurredAt">UTC timestamp when the event was observed.</param>
/// <param name="Detail">Optional free-form detail for operators and audit sinks.</param>
public sealed record BarrierAuditEvent(
    string EventType,
    string BarrierLevel,
    string AuthoritySource,
    string AgentName,
    string CorrelationId,
    string SpanId,
    DateTimeOffset OccurredAt,
    string? Detail = null);
