using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;

namespace Nexo.Infrastructure.Metrics;

/// <summary>
/// IMetricsCollector implementation that reports to OpenTelemetry via System.Diagnostics.Metrics.
/// Use with AddNexoOpenTelemetry() for production observability.
/// </summary>
public sealed class OpenTelemetryMetricsCollector : IMetricsCollector
{
    /// <summary>Meter name for OpenTelemetry registration. Use with AddMeter().</summary>
    public const string MeterName = "Nexo";
    private readonly Meter _meter;
    private readonly Histogram<double> _executionTimeHistogram;
    private readonly Counter<long> _operationCounter;
    private readonly ILogger<OpenTelemetryMetricsCollector>? _logger;

    public OpenTelemetryMetricsCollector(ILogger<OpenTelemetryMetricsCollector>? logger = null)
    {
        _meter = new Meter(MeterName, "1.0.0");
        _executionTimeHistogram = _meter.CreateHistogram<double>(
            "nexo.operation.duration",
            unit: "ms",
            description: "Duration of Nexo operations in milliseconds");
        _operationCounter = _meter.CreateCounter<long>(
            "nexo.operation.count",
            unit: null,
            description: "Nexo operation counter");
        _logger = logger;
    }

    /// <inheritdoc />
    public void RecordExecutionTime(string operationName, TimeSpan duration)
    {
        var tags = new KeyValuePair<string, object?>("operation", operationName);
        _executionTimeHistogram.Record(duration.TotalMilliseconds, tags);
        _logger?.LogDebug("Recorded execution time: {Operation} = {Duration}ms", operationName, duration.TotalMilliseconds);
    }

    /// <inheritdoc />
    public void IncrementCounter(string counterName, int value = 1)
    {
        var tags = new KeyValuePair<string, object?>("counter", counterName);
        _operationCounter.Add(value, tags);
        _logger?.LogDebug("Incremented counter: {Counter} += {Value}", counterName, value);
    }

    /// <inheritdoc />
    public Task<MetricsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MetricsSnapshot
        {
            ExecutionTimes = new Dictionary<string, TimeSpan>(),
            Counters = new Dictionary<string, long>(),
            CollectedAt = DateTime.UtcNow
        });
    }
}
