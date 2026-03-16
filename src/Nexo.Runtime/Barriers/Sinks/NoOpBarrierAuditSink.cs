using Nexo.Abstractions.Barriers;

namespace Nexo.Runtime.Barriers.Sinks;

public sealed class NoOpBarrierAuditSink : IBarrierAuditSink
{
    public ValueTask WriteAsync(
        BarrierAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
        => default;
}
