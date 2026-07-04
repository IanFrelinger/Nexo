using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Nexo.Infrastructure.Execution.Routing;
using Nexo.Infrastructure.Mesh;
using Nexo.Infrastructure.NodeCapabilityRuntime;
using Nexo.Infrastructure.NodeCapabilityRuntime.Profiles;

namespace Nexo.Tests.Infrastructure.Helpers.Ncr;

/// <summary>Knobs for <see cref="VirtualProductionNcrRoutingHost"/>.</summary>
public sealed class VirtualProductionNcrRoutingHostOptions
{
    /// <summary>Defaults tuned for fast CI.</summary>
    public Dictionary<string, string?> ConfigurationOverrides { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Nexo:RunPod:QueueDepthThreshold"] = "8",
        ["Nexo:RunPod:Timeout"] = "00:01:00",
        ["Nexo:RunPod:PollingInterval"] = "00:00:00.050",
        ["Nexo:RunPod:EnablePeerNetworkRouting"] = "true",
        ["Nexo:RunPod:PreferPeerNetworkOverCloud"] = "true",
        ["Nexo:RunPod:PeerTrustPolicy"] = "any",
        ["Nexo:RunPod:PeerDiscoveryInterval"] = "00:00:00.150",
        ["Nexo:RunPod:PeerCapabilityId"] = "generation.capability-routing",
        ["Nexo:NodeCapabilityRuntime:ProfileRefreshInterval"] = "00:00:00.150",
        ["Nexo:NodeCapabilityRuntime:NodeId"] = "virtual-prod-node"
    };

    public TimeSpan PostStartDelay { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Loopback RunPod API behaviour (same paths as production <see cref="RunPodHttpClient"/>).</summary>
    public RunPodLoopbackApiConfiguration RunPodCloud { get; } = new();

    /// <summary>
    /// Process environment variables applied while the host runs (restored on dispose).
    /// Use <c>NEXO_TOTAL_VRAM_BYTES</c> / <c>NEXO_AVAILABLE_VRAM_BYTES</c> for <see cref="EnvironmentHardwareProfiler"/>.
    /// </summary>
    public Dictionary<string, string?> EnvironmentOverrides { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NEXO_ALLOW_MOCK"] = "1"
    };

    /// <summary>Sets VRAM hints for <see cref="EnvironmentHardwareProfiler"/>.</summary>
    public void SetVramBytes(long totalBytes, long? availableBytes = null)
    {
        EnvironmentOverrides["NEXO_TOTAL_VRAM_BYTES"] = totalBytes.ToString();
        EnvironmentOverrides["NEXO_AVAILABLE_VRAM_BYTES"] = (availableBytes ?? totalBytes).ToString();
    }
}
