using Microsoft.Extensions.Logging;

namespace Nexo.Runtime.Barriers.Sinks;

/// <summary>Configuration for structured-log barrier audit sink log level overrides.</summary>
public sealed class StructuredLogBarrierAuditSinkOptions
{
    /// <summary>
    /// Override log level per event type. Key = BarrierAuditEventType constant.
    /// Unspecified event types use sink defaults.
    /// </summary>
    public IDictionary<string, LogLevel> EventLevelOverrides { get; init; }
        = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);
}
