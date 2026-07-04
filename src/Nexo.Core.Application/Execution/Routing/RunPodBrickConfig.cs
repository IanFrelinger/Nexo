using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Configuration for the RunPod execution backend.
/// Bound from the <c>Nexo:RunPod</c> configuration section.
/// <para>
/// Peer-network fields (<c>EnablePeerNetworkRouting</c>, <c>PeerTrustPolicy</c>,
/// <c>TrustedPeerIdsCsv</c>, etc.) control the middle tier of the routing
/// cascade (local → <b>peer</b> → cloud).  When <c>EnablePeerNetworkRouting</c>
/// is false the router skips peers entirely and falls through to RunPod cloud.
/// </para>
/// <para>
/// <b>Trust interaction:</b> <c>PeerTrustPolicy</c> works with the trust
/// subsystem registered in <c>NexoServiceCollectionExtensions</c>.
/// "trusted-preferred" tries trusted peers first but allows untrusted as
/// fallback; "trusted-only" rejects untrusted peers entirely.
/// </para>
/// </summary>
public sealed class RunPodBrickConfig
{
    public const string SectionName = "Nexo:RunPod";

    /// <summary>RunPod API key.  Required for cloud execution; ignored for peer-only routing.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>GPU tier to request (e.g. "NVIDIA_A4000").
    /// See <see cref="NexoDefaults.RunPodDefaultGpuTier"/>.</summary>
    public string PreferredGpuTier { get; set; } = NexoDefaults.RunPodDefaultGpuTier;

    /// <summary>Maximum time to wait for a single RunPod job before aborting.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(NexoDefaults.RunPodDefaultTimeoutMinutes);

    /// <summary>Interval between job-status poll requests to the RunPod API.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(NexoDefaults.RunPodDefaultPollingIntervalSeconds);

    /// <summary>Local directory where RunPod job outputs are staged before
    /// being returned to the caller.  Defaults to a temp-directory subfolder.</summary>
    public string OutputStagingPath { get; set; } = Path.Combine(Path.GetTempPath(), "nexo-runpod");

    /// <summary>When the RunPod queue depth exceeds this value, the router
    /// will attempt peer-network routing (if enabled) before falling back
    /// to cloud.  See <see cref="NexoDefaults.RunPodDefaultQueueDepthThreshold"/>.</summary>
    public int QueueDepthThreshold { get; set; } = NexoDefaults.RunPodDefaultQueueDepthThreshold;

    /// <summary>Base URL for the RunPod API.</summary>
    public string BaseUrl { get; set; } = NexoDefaults.RunPodDefaultBaseUrl;

    /// <summary>Master switch for peer-network routing.  When false, the
    /// routing cascade skips peers and goes directly to RunPod cloud.</summary>
    public bool EnablePeerNetworkRouting { get; set; }

    /// <summary>When true and peers are available, the router prefers a
    /// peer over RunPod cloud even if the cloud queue is empty.</summary>
    public bool PreferPeerNetworkOverCloud { get; set; } = true;

    /// <summary>Trust policy applied to peer selection.
    /// Values: "trusted-preferred" (default), "trusted-only", "any".
    /// Interacts with <c>TrustedPeerIdsCsv</c> / <c>UntrustedPeerIdsCsv</c>.</summary>
    public string PeerTrustPolicy { get; set; } = NexoDefaults.RunPodDefaultPeerTrustPolicy;

    /// <summary>Comma-separated list of explicitly trusted peer node IDs.</summary>
    public string TrustedPeerIdsCsv { get; set; } = string.Empty;

    /// <summary>Comma-separated list of explicitly untrusted (blocked) peer node IDs.</summary>
    public string UntrustedPeerIdsCsv { get; set; } = string.Empty;

    /// <summary>NCR capability ID used to discover peers that advertise
    /// generation capability routing.</summary>
    public string PeerCapabilityId { get; set; } = NexoDefaults.RunPodDefaultPeerCapabilityId;

    /// <summary>Brick ID dispatched when routing a generation job to a peer.</summary>
    public string PeerRoutingBrickId { get; set; } = NexoDefaults.RunPodDefaultPeerRoutingBrickId;

    /// <summary>Timeout for individual peer-to-peer requests.</summary>
    public TimeSpan PeerRequestTimeout { get; set; } = TimeSpan.FromSeconds(NexoDefaults.RunPodDefaultPeerRequestTimeoutSeconds);

    /// <summary>How often the background task refreshes the known-peers list.</summary>
    public TimeSpan PeerDiscoveryInterval { get; set; } = TimeSpan.FromSeconds(NexoDefaults.RunPodDefaultPeerDiscoveryIntervalSeconds);
}
