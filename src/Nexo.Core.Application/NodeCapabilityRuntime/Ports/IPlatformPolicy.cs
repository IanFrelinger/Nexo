using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;

namespace Nexo.Core.Application.NodeCapabilityRuntime.Ports;

/// <summary>
/// Platform-specific policy governing inference, model loading, and remote work advertisement.
/// </summary>
public interface IPlatformPolicy
{
    /// <summary>Platform this policy applies to.</summary>
    PlatformType Platform { get; }

    /// <summary>Whether inference may run given current node profile.</summary>
    bool CanRunInferenceNow(NodeProfile profile);

    /// <summary>Whether a model may be loaded given size and resource constraints.</summary>
    bool CanLoadModel(ModelDescriptor model, NodeProfile profile);

    /// <summary>Whether remote model pulls are permitted.</summary>
    bool CanPullModel(NodeProfile profile);

    /// <summary>Whether this node may advertise itself for federated remote work.</summary>
    bool CanAdvertiseRemoteWork(NodeProfile profile);

    /// <summary>Maximum total bytes available for cached models.</summary>
    long MaxModelCacheBytes { get; }

    /// <summary>Maximum bytes for a single loaded model.</summary>
    long MaxSingleModelBytes { get; }

    /// <summary>Maximum concurrent inference requests allowed.</summary>
    int MaxConcurrentInferenceRequests { get; }

    /// <summary>Time-to-live for hot (loaded) models before eviction consideration.</summary>
    TimeSpan HotModelTTL { get; }

    /// <summary>Maximum age for cold models before eviction.</summary>
    TimeSpan ColdEvictionAge { get; }

    /// <summary>Whether models should be unloaded immediately after inference on this platform.</summary>
    bool ShouldUnloadAfterInference(NodeProfile profile);

    /// <summary>Whether idle maintenance (eviction, prefetch) may run.</summary>
    bool CanRunIdleMaintenance(NodeProfile profile);
}
