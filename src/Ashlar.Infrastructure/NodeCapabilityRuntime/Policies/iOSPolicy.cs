using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;

namespace Ashlar.Infrastructure.NodeCapabilityRuntime.Policies;

/// <summary>Platform policy for iOS nodes (Neural Engine/battery constrained inference).</summary>
public sealed class iOSPolicy : IPlatformPolicy
{
    /// <summary>Platform.</summary>
    public PlatformType Platform => PlatformType.iOS;

    /// <summary>Whether run inference now is allowed.</summary>
    public bool CanRunInferenceNow(NodeProfile profile) =>
        profile.AppState != AppState.Background || profile.HasBackgroundTaskPermission;

    /// <summary>Whether load model is allowed.</summary>
    public bool CanLoadModel(ModelDescriptor model, NodeProfile profile) =>
        model.WeightSizeBytes <= MaxSingleModelBytes &&
        model.MinRAMRequiredBytes <= (long)(profile.AvailableRAMBytes * 0.4f);

    /// <summary>Whether pull model is allowed.</summary>
    public bool CanPullModel(NodeProfile profile) =>
        profile.NetworkLatency.IsWifi &&
        (!profile.IsOnBattery || profile.IsCharging || profile.BatteryLevelPercent > 20f);

    /// <summary>Whether advertise remote work is allowed.</summary>
    public bool CanAdvertiseRemoteWork(NodeProfile profile) =>
        profile.IsCharging &&
        profile.NetworkLatency.IsWifi &&
        profile.AppState == AppState.Background &&
        profile.BatteryLevelPercent > MinBatteryForRemoteWork &&
        profile.ThermalState < ThermalState.Serious;

    /// <summary>Max model cache bytes.</summary>
    public long MaxModelCacheBytes => 2L * 1024 * 1024 * 1024;
    /// <summary>Max single model bytes.</summary>
    public long MaxSingleModelBytes => 1536L * 1024 * 1024;
    /// <summary>Max concurrent inference requests.</summary>
    public int MaxConcurrentInferenceRequests => 1;

    /// <summary>Hot model t t l.</summary>
    public TimeSpan HotModelTTL => TimeSpan.FromMinutes(10);
    /// <summary>Cold eviction age.</summary>
    public TimeSpan ColdEvictionAge => TimeSpan.FromDays(7);

    /// <summary>Should unload after inference.</summary>
    public bool ShouldUnloadAfterInference(NodeProfile profile) =>
        !profile.IsCharging || profile.AvailableRAMBytes < 512L * 1024 * 1024;

    /// <summary>Whether run idle maintenance is allowed.</summary>
    public bool CanRunIdleMaintenance(NodeProfile profile) =>
        profile.IsCharging &&
        profile.NetworkLatency.IsWifi &&
        profile.AppState == AppState.Background &&
        profile.HasBackgroundTaskPermission;

    /// <summary>Min battery for remote work.</summary>
    public float MinBatteryForRemoteWork { get; init; } = 20f;
    /// <summary>Use neural engine.</summary>
    public bool UseNeuralEngine { get; init; } = true;
    /// <summary>Use metal.</summary>
    public bool UseMetal { get; init; } = true;
    /// <summary>Allow low power mode inference.</summary>
    public bool AllowLowPowerModeInference { get; init; }
}
