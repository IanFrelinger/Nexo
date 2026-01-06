using Nexo.Core.Domain.Clusters;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Executes cluster instances with real-time event streaming.
/// </summary>
public interface IClusterExecutor
{
    /// <summary>
    /// Execute a cluster instance with real-time event streaming.
    /// </summary>
    IAsyncEnumerable<ExecutionEvent> ExecuteAsync(
        ClusterInstance instance,
        ClusterInput input,
        IExecutionContext context,
        CancellationToken ct = default);
    
    /// <summary>
    /// Execute multiple instances of a cluster in parallel.
    /// </summary>
    IAsyncEnumerable<ExecutionEvent> ExecuteScaledAsync(
        string clusterId,
        IReadOnlyList<ClusterInstance> instances,
        ClusterInput sharedInput,
        IExecutionContext context,
        CancellationToken ct = default);
}

