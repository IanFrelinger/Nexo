using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;

namespace Ashlar.Infrastructure.NodeCapabilityRuntime.Policies;

/// <summary>Platform policy for macOS nodes (Metal/ANE-aware inference and cache limits).</summary>
public sealed class MacOsPolicy : IPlatformPolicy
{
    /// <summary>Platform.</summary>
    public PlatformType Platform => PlatformType.macOS;

    /// <summary>Whether run inference now is allowed.</summary>
    public bool CanRunInferenceNow(NodeProfile profile) =>
        profile.ThermalState < ThermalState.Serious &&
        profile.GPUUtilizationPercent < 90f;

    /// <summary>Whether load model is allowed.</summary>
    public bool CanLoadModel(ModelDescriptor model, NodeProfile profile) =>
        model.MinRAMRequiredBytes <= profile.AvailableRAMBytes;

    /// <summary>Whether pull model is allowed.</summary>
    public bool CanPullModel(NodeProfile profile) =>
        !profile.IsOnBattery || profile.IsCharging || profile.BatteryLevelPercent > 50f;

    /// <summary>Whether advertise remote work is allowed.</summary>
    public bool CanAdvertiseRemoteWork(NodeProfile profile) =>
        profile.IsCharging &&
        profile.ThermalState < ThermalState.Fair &&
        profile.CPUUtilizationPercent < 60f;

    /// <summary>Max model cache bytes.</summary>
    public long MaxModelCacheBytes => 50L * 1024 * 1024 * 1024;
    /// <summary>Max single model bytes.</summary>
    public long MaxSingleModelBytes => 40L * 1024 * 1024 * 1024;
    /// <summary>Max concurrent inference requests.</summary>
    public int MaxConcurrentInferenceRequests => 2;

    /// <summary>Hot model t t l.</summary>
    public TimeSpan HotModelTTL => TimeSpan.FromMinutes(45);
    /// <summary>Cold eviction age.</summary>
    public TimeSpan ColdEvictionAge => TimeSpan.FromDays(45);

    /// <summary>Should unload after inference.</summary>
    public bool ShouldUnloadAfterInference(NodeProfile profile) =>
        profile.IsOnBattery && !profile.IsCharging;

    /// <summary>Whether run idle maintenance is allowed.</summary>
    public bool CanRunIdleMaintenance(NodeProfile profile) =>
        !profile.IsUserActive && profile.ThermalState < ThermalState.Fair;
}
