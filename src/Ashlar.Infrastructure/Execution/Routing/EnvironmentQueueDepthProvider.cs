using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;

namespace Ashlar.Infrastructure.Execution.Routing;

/// <summary>
/// Reads local queue depth from environment for lightweight runtime routing.
/// </summary>
public sealed class EnvironmentQueueDepthProvider : ILocalQueueDepthProvider
{
    /// <summary>Current local queue depth from environment configuration.</summary>
    public int CurrentDepth
    {
        get
        {
            var raw = Environment.GetEnvironmentVariable("ASHLAR_LOCAL_QUEUE_DEPTH");
            return int.TryParse(raw, out var depth) ? Math.Max(0, depth) : 0;
        }
    }
}
