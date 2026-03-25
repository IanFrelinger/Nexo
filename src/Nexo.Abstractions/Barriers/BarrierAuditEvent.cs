namespace Nexo.Abstractions.Barriers;

public sealed record BarrierAuditEvent(
    string EventType,
    string BarrierLevel,
    string AuthoritySource,
    string AgentName,
    string CorrelationId,
    string SpanId,
    DateTimeOffset OccurredAt,
    string? Detail = null);
