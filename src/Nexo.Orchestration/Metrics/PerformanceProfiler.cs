using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Nexo.Orchestration.Metrics;

/// <summary>
/// Performance profiler for tracking execution time and resource usage.
/// 
/// Implements IDisposable pattern for automatic timing:
/// - Starts stopwatch on creation
/// - Records operation metrics on disposal
/// - Supports metadata attachment
/// 
/// Usage: `using var profiler = new PerformanceProfiler("OperationName", metrics);`
/// Automatically records duration when disposed.
/// </summary>
public sealed class PerformanceProfiler : IDisposable
{
    private readonly string _operationName;
    private readonly OrchestrationMetrics _metrics;
    private readonly Stopwatch _stopwatch;
    private readonly Dictionary<string, object> _metadata;
    private readonly ILogger<PerformanceProfiler>? _logger;
    private bool _disposed;

    public PerformanceProfiler(
        string operationName,
        OrchestrationMetrics metrics,
        ILogger<PerformanceProfiler>? logger = null,
        Dictionary<string, object>? metadata = null)
    {
        _operationName = operationName ?? throw new ArgumentNullException(nameof(operationName));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger;
        _metadata = metadata ?? new Dictionary<string, object>();
        _stopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Adds metadata to the profiler.
    /// </summary>
    public void AddMetadata(string key, object value)
    {
        _metadata[key] = value;
    }

    /// <summary>
    /// Gets the elapsed time so far.
    /// </summary>
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _stopwatch.Stop();
        _metrics.RecordOperation(_operationName, _stopwatch.Elapsed, _metadata);
        
        _logger?.LogDebug("Operation {Operation} completed in {Duration}ms", _operationName, _stopwatch.Elapsed.TotalMilliseconds);
        
        _disposed = true;
    }
}

/// <summary>
/// Resource usage tracker for monitoring memory and CPU usage.
/// 
/// Responsibilities:
/// - Gets current memory usage in MB
/// - Gets current CPU time
/// - Tracks process resource consumption
/// 
/// Used for monitoring resource usage during orchestration.
/// </summary>
public sealed class ResourceUsageTracker
{
    private readonly Process _process;
    private readonly ILogger<ResourceUsageTracker>? _logger;

    public ResourceUsageTracker(ILogger<ResourceUsageTracker>? logger = null)
    {
        _process = Process.GetCurrentProcess();
        _logger = logger;
    }

    /// <summary>
    /// Gets current memory usage in MB.
    /// </summary>
    public long GetMemoryUsageMB()
    {
        try
        {
            _process.Refresh();
            return _process.WorkingSet64 / (1024 * 1024);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get memory usage");
            return 0;
        }
    }

    /// <summary>
    /// Gets current CPU time.
    /// </summary>
    public TimeSpan GetCpuTime()
    {
        try
        {
            _process.Refresh();
            return _process.TotalProcessorTime;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get CPU time");
            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Gets resource usage snapshot.
    /// </summary>
    public ResourceSnapshot GetSnapshot()
    {
        return new ResourceSnapshot
        {
            MemoryUsageMB = GetMemoryUsageMB(),
            CpuTime = GetCpuTime(),
            ThreadCount = _process.Threads.Count,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>
/// Snapshot of resource usage at a point in time.
/// </summary>
public sealed record ResourceSnapshot
{
    public required long MemoryUsageMB { get; init; }
    public required TimeSpan CpuTime { get; init; }
    public required int ThreadCount { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

