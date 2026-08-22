namespace Ashlar.Abstractions.Barriers;

/// <summary>
/// Optional sink extension point for barrier audit events.
/// </summary>
public interface IBarrierAuditSink
{
    /// <summary>
    /// Write a single audit event to this sink's destination.
    /// Implementations must never throw; failures should be handled internally.
    /// </summary>
    ValueTask WriteAsync(
        BarrierAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
