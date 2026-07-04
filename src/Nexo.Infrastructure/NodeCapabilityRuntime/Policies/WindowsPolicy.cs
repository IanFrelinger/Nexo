using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Policies;

/// <summary>Platform policy for Windows nodes (CUDA/DirectML, Defender-aware cache limits).</summary>
public sealed class WindowsPolicy : IPlatformPolicy
{
    /// <summary>Platform.</summary>
    public PlatformType Platform => PlatformType.Windows;

    /// <summary>Whether run inference now is allowed.</summary>
    public bool CanRunInferenceNow(NodeProfile profile) =>
        profile.GPUUtilizationPercent < 95f;

    /// <summary>Whether load model is allowed.</summary>
    public bool CanLoadModel(ModelDescriptor model, NodeProfile profile) =>
        model.MinVRAMRequiredBytes <= profile.AvailableVRAMBytes &&
        model.MinRAMRequiredBytes <= profile.AvailableRAMBytes;

    /// <summary>Whether pull model is allowed.</summary>
    public bool CanPullModel(NodeProfile profile) => true;

    /// <summary>Whether advertise remote work is allowed.</summary>
    public bool CanAdvertiseRemoteWork(NodeProfile profile) =>
        profile.GPUUtilizationPercent < 60f &&
        profile.CPUUtilizationPercent < 70f;

    /// <summary>Max model cache bytes.</summary>
    public long MaxModelCacheBytes => 100L * 1024 * 1024 * 1024;
    /// <summary>Max single model bytes.</summary>
    public long MaxSingleModelBytes => 50L * 1024 * 1024 * 1024;
    /// <summary>Max concurrent inference requests.</summary>
    public int MaxConcurrentInferenceRequests => 4;

    /// <summary>Hot model t t l.</summary>
    public TimeSpan HotModelTTL => TimeSpan.FromHours(2);
    /// <summary>Cold eviction age.</summary>
    public TimeSpan ColdEvictionAge => TimeSpan.FromDays(60);

    /// <summary>Should unload after inference.</summary>
    public bool ShouldUnloadAfterInference(NodeProfile profile) =>
        profile.AvailableVRAMBytes < 512L * 1024 * 1024;

    /// <summary>Whether run idle maintenance is allowed.</summary>
    public bool CanRunIdleMaintenance(NodeProfile profile) =>
        !profile.IsUserActive;

    /// <summary>Allow c u d a.</summary>
    public bool AllowCUDA { get; init; } = true;
    /// <summary>Allow direct m l.</summary>
    public bool AllowDirectML { get; init; } = true;
    /// <summary>Enforce defender exclusions.</summary>
    public bool EnforceDefenderExclusions { get; init; } = true;
}
