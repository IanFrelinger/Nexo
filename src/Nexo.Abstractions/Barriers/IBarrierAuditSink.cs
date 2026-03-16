namespace Nexo.Abstractions.Barriers;

/// <summary>
/// Optional sink extension point for barrier audit events.
/// </summary>
public interface IBarrierAuditSink
{
    ValueTask WriteAsync(
        BarrierAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
