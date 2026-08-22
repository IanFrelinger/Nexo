using Ashlar.Core.Application.Execution.Routing;

namespace Ashlar.Core.Application.NodeCapabilityRuntime.Ports;

/// <summary>
/// Snapshot of routing-relevant NCR capability data.
/// </summary>
public interface INCRCapabilitySnapshot
{
    /// <summary>Free GPU memory available for model loading.</summary>
    long AvailableVramBytes { get; }

    /// <summary>GPU compute class used for federation routing decisions.</summary>
    GpuComputeClass ComputeClass { get; }

    /// <summary>Current local generation queue depth.</summary>
    int CurrentQueueDepth { get; }

    /// <summary>UTC timestamp when this snapshot was captured.</summary>
    DateTimeOffset CapturedAt { get; }
}
