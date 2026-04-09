using Nexo.Core.Application.Execution.Routing;

namespace Nexo.Core.Application.NodeCapabilityRuntime.Ports;

/// <summary>
/// Snapshot of routing-relevant NCR capability data.
/// </summary>
public interface INCRCapabilitySnapshot
{
    long AvailableVramBytes { get; }

    GpuComputeClass ComputeClass { get; }

    int CurrentQueueDepth { get; }

    DateTimeOffset CapturedAt { get; }
}

/// <summary>
/// Source of local generation queue depth used by NCR capability routing.
/// </summary>
public interface ILocalQueueDepthProvider
{
    int CurrentDepth { get; }
}
