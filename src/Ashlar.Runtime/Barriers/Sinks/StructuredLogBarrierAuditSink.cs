using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Barriers;

namespace Ashlar.Runtime.Barriers.Sinks;

/// <summary>Structured logging sink for barrier audit events with per-event log level overrides.</summary>
public sealed class StructuredLogBarrierAuditSink : IBarrierAuditSink
{
    private readonly ILogger<StructuredLogBarrierAuditSink> _logger;
    private readonly IReadOnlyDictionary<string, LogLevel> _eventLevelOverrides;

    public StructuredLogBarrierAuditSink(
        ILogger<StructuredLogBarrierAuditSink> logger,
        StructuredLogBarrierAuditSinkOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        _eventLevelOverrides = new Dictionary<string, LogLevel>(
            options.EventLevelOverrides,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Writes a barrier audit event to structured application logs.</summary>
    public ValueTask WriteAsync(
        BarrierAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        if (auditEvent is null)
        {
            return default;
        }

        try
        {
            var level = ResolveLevel(auditEvent.EventType);
            _logger.Log(
                level,
                "[BARRIER_AUDIT] {EventType} agent={AgentName} level={BarrierLevel} source={AuthoritySource} correlationId={CorrelationId} spanId={SpanId} detail={Detail} occurredAt={OccurredAt}",
                auditEvent.EventType,
                auditEvent.AgentName,
                auditEvent.BarrierLevel,
                auditEvent.AuthoritySource,
                auditEvent.CorrelationId,
                auditEvent.SpanId,
                auditEvent.Detail,
                auditEvent.OccurredAt);
        }
        catch
        {
            // Sink contract: never throw into the caller path.
        }

        return default;
    }

    private LogLevel ResolveLevel(string eventType)
    {
        if (_eventLevelOverrides.TryGetValue(eventType, out var overridden))
        {
            return overridden;
        }

        if (string.Equals(eventType, BarrierAuditEventType.ContextInitialized, StringComparison.OrdinalIgnoreCase))
            return LogLevel.Information;
        if (string.Equals(eventType, BarrierAuditEventType.IdentityResolved, StringComparison.OrdinalIgnoreCase))
            return LogLevel.Information;
        if (string.Equals(eventType, BarrierAuditEventType.AgentInvoked, StringComparison.OrdinalIgnoreCase))
            return LogLevel.Information;
        if (string.Equals(eventType, BarrierAuditEventType.DefaultApplied, StringComparison.OrdinalIgnoreCase))
            return LogLevel.Warning;
        if (string.Equals(eventType, BarrierAuditEventType.PropagatedOverWire, StringComparison.OrdinalIgnoreCase))
            return LogLevel.Debug;
        if (string.Equals(eventType, BarrierAuditEventType.ValidationFailed, StringComparison.OrdinalIgnoreCase))
            return LogLevel.Warning;
        if (string.Equals(eventType, BarrierAuditEventType.CeilingExceeded, StringComparison.OrdinalIgnoreCase))
            return LogLevel.Error;
        if (string.Equals(eventType, BarrierAuditEventType.ElevationAttempted, StringComparison.OrdinalIgnoreCase))
            return LogLevel.Error;
        if (string.Equals(eventType, BarrierAuditEventType.ElevationDenied, StringComparison.OrdinalIgnoreCase))
            return LogLevel.Error;

        return LogLevel.Warning;
    }
}
