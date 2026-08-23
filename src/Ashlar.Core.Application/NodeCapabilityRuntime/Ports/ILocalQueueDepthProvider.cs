using Ashlar.Core.Application.Execution.Routing;

namespace Ashlar.Core.Application.NodeCapabilityRuntime.Ports;

/// <summary>
/// Source of local generation queue depth used by NCR capability routing.
/// </summary>
public interface ILocalQueueDepthProvider
{
    /// <summary>Current number of pending generation requests on this node.</summary>
    int CurrentDepth { get; }
}
