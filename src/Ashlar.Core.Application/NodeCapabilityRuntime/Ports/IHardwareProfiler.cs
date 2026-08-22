using Ashlar.Core.Application.Execution.Ports;
using Ashlar.Core.Application.NodeCapabilityRuntime.Models;

namespace Ashlar.Core.Application.NodeCapabilityRuntime.Ports;

/// <summary>Captures hardware and platform characteristics for capability routing.</summary>
public interface IHardwareProfiler
{
    /// <summary>Captures a fresh node profile snapshot.</summary>
    Task<NodeProfile> CaptureAsync(CancellationToken ct = default);
}
