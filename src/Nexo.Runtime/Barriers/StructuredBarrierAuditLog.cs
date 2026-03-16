using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;

namespace Nexo.Runtime.Barriers;

/// <summary>
/// Structured logger-backed barrier audit log with optional sink fan-out.
/// </summary>
public sealed class StructuredBarrierAuditLog : IBarrierAuditLog
{
    private readonly ILogger<StructuredBarrierAuditLog> _logger;
    private readonly IReadOnlyList<IBarrierAuditSink> _sinks;

    public StructuredBarrierAuditLog(
        ILogger<StructuredBarrierAuditLog> logger,
        IEnumerable<IBarrierAuditSink>? sinks = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sinks = sinks is null ? Array.Empty<IBarrierAuditSink>() : sinks.ToList();
    }

    public async ValueTask RecordAsync(
        BarrierAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        var level = string.Equals(auditEvent.EventType, BarrierAuditEventType.AgentInvoked, StringComparison.OrdinalIgnoreCase)
            ? LogLevel.Information
            : LogLevel.Warning;

        _logger.Log(
            level,
            "BarrierAudit EventType={EventType} BarrierLevel={BarrierLevel} AuthoritySource={AuthoritySource} AgentName={AgentName} CorrelationId={CorrelationId} SpanId={SpanId} OccurredAt={OccurredAt} Detail={Detail}",
            auditEvent.EventType,
            auditEvent.BarrierLevel,
            auditEvent.AuthoritySource,
            auditEvent.AgentName,
            auditEvent.CorrelationId,
            auditEvent.SpanId,
            auditEvent.OccurredAt,
            auditEvent.Detail);

        foreach (var sink in _sinks)
        {
            await sink.WriteAsync(auditEvent, cancellationToken);
        }
    }
}
