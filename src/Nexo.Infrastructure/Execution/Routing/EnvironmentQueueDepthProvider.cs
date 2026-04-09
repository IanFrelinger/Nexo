using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.Execution.Routing;

/// <summary>
/// Reads local queue depth from environment for lightweight runtime routing.
/// </summary>
public sealed class EnvironmentQueueDepthProvider : ILocalQueueDepthProvider
{
    public int CurrentDepth
    {
        get
        {
            var raw = Environment.GetEnvironmentVariable("NEXO_LOCAL_QUEUE_DEPTH");
            return int.TryParse(raw, out var depth) ? Math.Max(0, depth) : 0;
        }
    }
}
