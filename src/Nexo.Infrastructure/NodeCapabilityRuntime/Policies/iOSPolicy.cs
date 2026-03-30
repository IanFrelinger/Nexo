using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Policies;

public sealed class iOSPolicy : IPlatformPolicy
{
    public PlatformType Platform => PlatformType.iOS;

    public bool CanRunInferenceNow(NodeProfile profile) =>
        profile.AppState != AppState.Background || profile.HasBackgroundTaskPermission;

    public bool CanLoadModel(ModelDescriptor model, NodeProfile profile) =>
        model.WeightSizeBytes <= MaxSingleModelBytes &&
        model.MinRAMRequiredBytes <= (long)(profile.AvailableRAMBytes * 0.4f);

    public bool CanPullModel(NodeProfile profile) =>
        profile.NetworkLatency.IsWifi &&
        (!profile.IsOnBattery || profile.IsCharging || profile.BatteryLevelPercent > 20f);

    public bool CanAdvertiseRemoteWork(NodeProfile profile) =>
        profile.IsCharging &&
        profile.NetworkLatency.IsWifi &&
        profile.AppState == AppState.Background &&
        profile.BatteryLevelPercent > MinBatteryForRemoteWork &&
        profile.ThermalState < ThermalState.Serious;

    public long MaxModelCacheBytes => 2L * 1024 * 1024 * 1024;
    public long MaxSingleModelBytes => 1536L * 1024 * 1024;
    public int MaxConcurrentInferenceRequests => 1;

    public TimeSpan HotModelTTL => TimeSpan.FromMinutes(10);
    public TimeSpan ColdEvictionAge => TimeSpan.FromDays(7);

    public bool ShouldUnloadAfterInference(NodeProfile profile) =>
        !profile.IsCharging || profile.AvailableRAMBytes < 512L * 1024 * 1024;

    public bool CanRunIdleMaintenance(NodeProfile profile) =>
        profile.IsCharging &&
        profile.NetworkLatency.IsWifi &&
        profile.AppState == AppState.Background &&
        profile.HasBackgroundTaskPermission;

    public float MinBatteryForRemoteWork { get; init; } = 20f;
    public bool UseNeuralEngine { get; init; } = true;
    public bool UseMetal { get; init; } = true;
    public bool AllowLowPowerModeInference { get; init; }
}
