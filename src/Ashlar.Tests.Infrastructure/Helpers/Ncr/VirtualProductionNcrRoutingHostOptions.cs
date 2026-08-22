using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Core.Application.Execution.Routing;
using Ashlar.Core.Application.Mesh.Models;
using Ashlar.Core.Application.Mesh.Ports;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Ashlar.Infrastructure.Execution;
using Ashlar.Infrastructure.Execution.Routing;
using Ashlar.Infrastructure.Mesh;
using Ashlar.Infrastructure.NodeCapabilityRuntime;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Profiles;

namespace Ashlar.Tests.Infrastructure.Helpers.Ncr;

/// <summary>Knobs for <see cref="VirtualProductionNcrRoutingHost"/>.</summary>
public sealed class VirtualProductionNcrRoutingHostOptions
{
    /// <summary>Defaults tuned for fast CI.</summary>
    public Dictionary<string, string?> ConfigurationOverrides { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ashlar:RunPod:QueueDepthThreshold"] = "8",
        ["Ashlar:RunPod:Timeout"] = "00:01:00",
        ["Ashlar:RunPod:PollingInterval"] = "00:00:00.050",
        ["Ashlar:RunPod:EnablePeerNetworkRouting"] = "true",
        ["Ashlar:RunPod:PreferPeerNetworkOverCloud"] = "true",
        ["Ashlar:RunPod:PeerTrustPolicy"] = "any",
        ["Ashlar:RunPod:PeerDiscoveryInterval"] = "00:00:00.150",
        ["Ashlar:RunPod:PeerCapabilityId"] = "generation.capability-routing",
        ["Ashlar:NodeCapabilityRuntime:ProfileRefreshInterval"] = "00:00:00.150",
        ["Ashlar:NodeCapabilityRuntime:NodeId"] = "virtual-prod-node"
    };

    public TimeSpan PostStartDelay { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Loopback RunPod API behaviour (same paths as production <see cref="RunPodHttpClient"/>).</summary>
    public RunPodLoopbackApiConfiguration RunPodCloud { get; } = new();

    /// <summary>
    /// Process environment variables applied while the host runs (restored on dispose).
    /// Use <c>ASHLAR_TOTAL_VRAM_BYTES</c> / <c>ASHLAR_AVAILABLE_VRAM_BYTES</c> for <see cref="EnvironmentHardwareProfiler"/>.
    /// </summary>
    public Dictionary<string, string?> EnvironmentOverrides { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ASHLAR_ALLOW_MOCK"] = "1"
    };

    /// <summary>Sets VRAM hints for <see cref="EnvironmentHardwareProfiler"/>.</summary>
    public void SetVramBytes(long totalBytes, long? availableBytes = null)
    {
        EnvironmentOverrides["ASHLAR_TOTAL_VRAM_BYTES"] = totalBytes.ToString();
        EnvironmentOverrides["ASHLAR_AVAILABLE_VRAM_BYTES"] = (availableBytes ?? totalBytes).ToString();
    }
}
