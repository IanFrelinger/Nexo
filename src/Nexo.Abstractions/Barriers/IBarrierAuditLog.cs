namespace Nexo.Abstractions.Barriers;

public interface IBarrierAuditLog
{
    ValueTask RecordAsync(
        BarrierAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
