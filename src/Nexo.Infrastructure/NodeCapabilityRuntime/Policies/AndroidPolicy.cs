using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Policies;

/// <summary>Platform policy for Android nodes (battery/thermal constrained inference).</summary>
public sealed class AndroidPolicy : IPlatformPolicy
{
    /// <summary>Platform.</summary>
    public PlatformType Platform => PlatformType.Android;

    /// <summary>Whether run inference now is allowed.</summary>
    public bool CanRunInferenceNow(NodeProfile profile) =>
        profile.ThermalState < ThermalState.Severe &&
        profile.AppState != AppState.BackgroundRestricted;

    /// <summary>Whether load model is allowed.</summary>
    public bool CanLoadModel(ModelDescriptor model, NodeProfile profile) =>
        model.WeightSizeBytes <= MaxSingleModelBytes &&
        model.MinRAMRequiredBytes <= profile.AvailableRAMBytes * 0.45f;

    /// <summary>Whether pull model is allowed.</summary>
    public bool CanPullModel(NodeProfile profile) =>
        profile.NetworkLatency.IsWifi ||
        (AllowCellularPull && profile.BatteryLevelPercent > 30f);

    /// <summary>Whether advertise remote work is allowed.</summary>
    public bool CanAdvertiseRemoteWork(NodeProfile profile) =>
        profile.IsCharging &&
        profile.NetworkLatency.IsWifi &&
        profile.BatteryLevelPercent > MinBatteryForRemoteWork &&
        !profile.IsBatteryOptimizationEnabled &&
        profile.ThermalState < ThermalState.Serious;

    /// <summary>Max model cache bytes.</summary>
    public long MaxModelCacheBytes => 3L * 1024 * 1024 * 1024;
    /// <summary>Max single model bytes.</summary>
    public long MaxSingleModelBytes => 2L * 1024 * 1024 * 1024;
    /// <summary>Max concurrent inference requests.</summary>
    public int MaxConcurrentInferenceRequests => 1;
    /// <summary>Hot model t t l.</summary>
    public TimeSpan HotModelTTL => TimeSpan.FromMinutes(15);
    /// <summary>Cold eviction age.</summary>
    public TimeSpan ColdEvictionAge => TimeSpan.FromDays(14);

    /// <summary>Should unload after inference.</summary>
    public bool ShouldUnloadAfterInference(NodeProfile profile) =>
        profile.AvailableRAMBytes < 512L * 1024 * 1024;

    /// <summary>Whether run idle maintenance is allowed.</summary>
    public bool CanRunIdleMaintenance(NodeProfile profile) =>
        profile.IsCharging &&
        !profile.IsBatteryOptimizationEnabled &&
        profile.NetworkLatency.IsWifi;

    /// <summary>Min battery for remote work.</summary>
    public float MinBatteryForRemoteWork { get; init; } = 20f;
    /// <summary>Allow cellular pull.</summary>
    public bool AllowCellularPull { get; init; }
    /// <summary>Use vulkan compute.</summary>
    public bool UseVulkanCompute { get; init; } = true;
    /// <summary>Use n n a p i.</summary>
    public bool UseNNAPI { get; init; } = true;
    /// <summary>Doze aware.</summary>
    public bool DozeAware { get; init; } = true;
}
