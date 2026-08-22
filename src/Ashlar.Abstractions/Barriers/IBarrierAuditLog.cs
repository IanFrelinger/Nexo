namespace Ashlar.Abstractions.Barriers;

/// <summary>
/// Append-only audit sink for barrier lifecycle and identity events.
/// Implementations persist events for compliance review, operator dashboards,
/// and post-incident tracing without blocking the caller's critical path.
/// </summary>
public interface IBarrierAuditLog
{
    /// <summary>
    /// Records a single barrier audit event.
    /// Implementations should be non-throwing under normal operation and tolerate
    /// downstream store failures according to host policy (fail-open vs fail-closed).
    /// </summary>
    /// <param name="auditEvent">Structured audit payload to persist.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative shutdown.</param>
    ValueTask RecordAsync(
        BarrierAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
