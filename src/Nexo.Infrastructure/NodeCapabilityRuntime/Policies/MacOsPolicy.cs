using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Policies;

public sealed class MacOsPolicy : IPlatformPolicy
{
    public PlatformType Platform => PlatformType.macOS;

    public bool CanRunInferenceNow(NodeProfile profile) =>
        profile.ThermalState < ThermalState.Serious &&
        profile.GPUUtilizationPercent < 90f;

    public bool CanLoadModel(ModelDescriptor model, NodeProfile profile) =>
        model.MinRAMRequiredBytes <= profile.AvailableRAMBytes;

    public bool CanPullModel(NodeProfile profile) =>
        !profile.IsOnBattery || profile.IsCharging || profile.BatteryLevelPercent > 50f;

    public bool CanAdvertiseRemoteWork(NodeProfile profile) =>
        profile.IsCharging &&
        profile.ThermalState < ThermalState.Fair &&
        profile.CPUUtilizationPercent < 60f;

    public long MaxModelCacheBytes => 50L * 1024 * 1024 * 1024;
    public long MaxSingleModelBytes => 40L * 1024 * 1024 * 1024;
    public int MaxConcurrentInferenceRequests => 2;

    public TimeSpan HotModelTTL => TimeSpan.FromMinutes(45);
    public TimeSpan ColdEvictionAge => TimeSpan.FromDays(45);

    public bool ShouldUnloadAfterInference(NodeProfile profile) =>
        profile.IsOnBattery && !profile.IsCharging;

    public bool CanRunIdleMaintenance(NodeProfile profile) =>
        !profile.IsUserActive && profile.ThermalState < ThermalState.Fair;
}
