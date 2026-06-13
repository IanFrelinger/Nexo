using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;
using Nexo.Runtime.Barriers.Sinks;

namespace Nexo.Runtime.Barriers;

/// <summary>
/// Structured logger-backed barrier audit log with optional sink fan-out.
/// </summary>
internal sealed class StructuredBarrierAuditLog : IBarrierAuditLog
{
    private readonly ILogger<StructuredBarrierAuditLog> _logger;
    private readonly IReadOnlyList<IBarrierAuditSink> _sinks;

    public StructuredBarrierAuditLog(
        ILogger<StructuredBarrierAuditLog> logger,
        IEnumerable<IBarrierAuditSink>? sinks = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sinks = sinks is null ? Array.Empty<IBarrierAuditSink>() : sinks.ToList();

        if (_sinks.Count == 0 || _sinks.All(static sink => sink is NoOpBarrierAuditSink))
        {
            _logger.LogWarning("No IBarrierAuditSink registered. Barrier audit events will be discarded.");
        }
    }

    public async ValueTask RecordAsync(
        BarrierAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        if (auditEvent is null)
            throw new ArgumentNullException(nameof(auditEvent));

        foreach (var sink in _sinks)
        {
            try
            {
                await sink.WriteAsync(auditEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Barrier audit sink failure. Sink={SinkName} Message={SinkMessage}",
                    sink.GetType().Name,
                    ex.Message);
            }
        }
    }
}
