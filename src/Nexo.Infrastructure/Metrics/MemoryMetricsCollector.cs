using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using System.Collections.Concurrent;

namespace Nexo.Infrastructure.Metrics;

/// <summary>
/// In-memory metrics collector implementation.
/// </summary>
public class MemoryMetricsCollector : IMetricsCollector
{
    private readonly ConcurrentDictionary<string, TimeSpan> _executionTimes = new();
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ILogger<MemoryMetricsCollector> _logger;

    public MemoryMetricsCollector(ILogger<MemoryMetricsCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordExecutionTime(string operationName, TimeSpan duration)
    {
        _executionTimes.AddOrUpdate(
            operationName,
            duration,
            (key, oldValue) => oldValue + duration);

        _logger.LogDebug(
            "Recorded execution time: {Operation} = {Duration}ms",
            operationName,
            duration.TotalMilliseconds);
    }

    public void IncrementCounter(string counterName, int value = 1)
    {
        _counters.AddOrUpdate(
            counterName,
            value,
            (key, oldValue) => oldValue + value);

        _logger.LogDebug(
            "Incremented counter: {Counter} = {Value}",
            counterName,
            _counters[counterName]);
    }

    public Task<MetricsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = new MetricsSnapshot
        {
            ExecutionTimes = new Dictionary<string, TimeSpan>(_executionTimes),
            Counters = new Dictionary<string, long>(_counters)
        };

        return Task.FromResult(snapshot);
    }
}

