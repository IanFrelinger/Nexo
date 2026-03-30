using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Policies;

public sealed class AndroidPolicy : IPlatformPolicy
{
    public PlatformType Platform => PlatformType.Android;

    public bool CanRunInferenceNow(NodeProfile profile) =>
        profile.ThermalState < ThermalState.Severe &&
        profile.AppState != AppState.BackgroundRestricted;

    public bool CanLoadModel(ModelDescriptor model, NodeProfile profile) =>
        model.WeightSizeBytes <= MaxSingleModelBytes &&
        model.MinRAMRequiredBytes <= profile.AvailableRAMBytes * 0.45f;

    public bool CanPullModel(NodeProfile profile) =>
        profile.NetworkLatency.IsWifi ||
        (AllowCellularPull && profile.BatteryLevelPercent > 30f);

    public bool CanAdvertiseRemoteWork(NodeProfile profile) =>
        profile.IsCharging &&
        profile.NetworkLatency.IsWifi &&
        profile.BatteryLevelPercent > MinBatteryForRemoteWork &&
        !profile.IsBatteryOptimizationEnabled &&
        profile.ThermalState < ThermalState.Serious;

    public long MaxModelCacheBytes => 3L * 1024 * 1024 * 1024;
    public long MaxSingleModelBytes => 2L * 1024 * 1024 * 1024;
    public int MaxConcurrentInferenceRequests => 1;
    public TimeSpan HotModelTTL => TimeSpan.FromMinutes(15);
    public TimeSpan ColdEvictionAge => TimeSpan.FromDays(14);

    public bool ShouldUnloadAfterInference(NodeProfile profile) =>
        profile.AvailableRAMBytes < 512L * 1024 * 1024;

    public bool CanRunIdleMaintenance(NodeProfile profile) =>
        profile.IsCharging &&
        !profile.IsBatteryOptimizationEnabled &&
        profile.NetworkLatency.IsWifi;

    public float MinBatteryForRemoteWork { get; init; } = 20f;
    public bool AllowCellularPull { get; init; }
    public bool UseVulkanCompute { get; init; } = true;
    public bool UseNNAPI { get; init; } = true;
    public bool DozeAware { get; init; } = true;
}
