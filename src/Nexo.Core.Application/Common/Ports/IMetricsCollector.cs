namespace Nexo.Core.Application.Common.Ports;

/// <summary>
/// Port for collecting execution metrics and telemetry.
/// 
/// Defines the contract for metrics collection:
/// - Record execution times
/// - Increment counters
/// - Get metrics snapshots
/// 
/// Implementations (MemoryMetricsCollector, etc.) provide specific metrics storage.
/// Used throughout the application for performance monitoring and telemetry.
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Records execution time for an operation.
    /// </summary>
    void RecordExecutionTime(string operationName, TimeSpan duration);

    /// <summary>
    /// Increments a counter for an operation.
    /// </summary>
    void IncrementCounter(string counterName, int value = 1);

    /// <summary>
    /// Gets all collected metrics.
    /// </summary>
    Task<MetricsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot of collected metrics.
/// </summary>
public record MetricsSnapshot
{
    public required IReadOnlyDictionary<string, TimeSpan> ExecutionTimes { get; init; }
    public required IReadOnlyDictionary<string, long> Counters { get; init; }
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
}

