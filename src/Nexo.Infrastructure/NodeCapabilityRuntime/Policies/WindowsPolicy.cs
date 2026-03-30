using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Policies;

public sealed class WindowsPolicy : IPlatformPolicy
{
    public PlatformType Platform => PlatformType.Windows;

    public bool CanRunInferenceNow(NodeProfile profile) =>
        profile.GPUUtilizationPercent < 95f;

    public bool CanLoadModel(ModelDescriptor model, NodeProfile profile) =>
        model.MinVRAMRequiredBytes <= profile.AvailableVRAMBytes &&
        model.MinRAMRequiredBytes <= profile.AvailableRAMBytes;

    public bool CanPullModel(NodeProfile profile) => true;

    public bool CanAdvertiseRemoteWork(NodeProfile profile) =>
        profile.GPUUtilizationPercent < 60f &&
        profile.CPUUtilizationPercent < 70f;

    public long MaxModelCacheBytes => 100L * 1024 * 1024 * 1024;
    public long MaxSingleModelBytes => 50L * 1024 * 1024 * 1024;
    public int MaxConcurrentInferenceRequests => 4;

    public TimeSpan HotModelTTL => TimeSpan.FromHours(2);
    public TimeSpan ColdEvictionAge => TimeSpan.FromDays(60);

    public bool ShouldUnloadAfterInference(NodeProfile profile) =>
        profile.AvailableVRAMBytes < 512L * 1024 * 1024;

    public bool CanRunIdleMaintenance(NodeProfile profile) =>
        !profile.IsUserActive;

    public bool AllowCUDA { get; init; } = true;
    public bool AllowDirectML { get; init; } = true;
    public bool EnforceDefenderExclusions { get; init; } = true;
}
