using Nexo.Abstractions.Barriers;

namespace Nexo.Runtime.Barriers.Sinks;

/// <summary>No-op audit sink that discards events (used when auditing is disabled).</summary>
public sealed class NoOpBarrierAuditSink : IBarrierAuditSink
{
    /// <summary>Discards the audit event without writing.</summary>
    public ValueTask WriteAsync(
        BarrierAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
        => default;
}
