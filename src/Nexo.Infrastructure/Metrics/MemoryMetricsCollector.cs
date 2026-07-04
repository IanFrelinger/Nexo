using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using System.Collections.Concurrent;

namespace Nexo.Infrastructure.Metrics;

/// <summary>
/// In-memory metrics collector implementation.
/// 
/// Responsibilities:
/// - Records execution times for operations
/// - Tracks counter metrics
/// - Provides metrics snapshots
/// - Thread-safe implementation using concurrent collections
/// 
/// Implements IMetricsCollector for use throughout the application.
/// Used by orchestration and application layers for metrics collection.
/// </summary>
public class MemoryMetricsCollector : IMetricsCollector
{
    private readonly ConcurrentDictionary<string, TimeSpan> _executionTimes = new();
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ILogger<MemoryMetricsCollector> _logger;

    /// <summary>Initializes a new memory metrics collector.</summary>
    public MemoryMetricsCollector(ILogger<MemoryMetricsCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Record execution time.</summary>
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

    /// <summary>Increment counter.</summary>
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

    /// <summary>Get snapshot asynchronously.</summary>
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

