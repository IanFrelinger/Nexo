using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Policies;

public sealed class LinuxPolicy : IPlatformPolicy
{
    public PlatformType Platform => PlatformType.Linux;

    public bool CanRunInferenceNow(NodeProfile profile) =>
        profile.GPUUtilizationPercent < 98f;

    public bool CanLoadModel(ModelDescriptor model, NodeProfile profile) =>
        model.MinVRAMRequiredBytes <= profile.AvailableVRAMBytes &&
        model.MinRAMRequiredBytes <= profile.AvailableRAMBytes;

    public bool CanPullModel(NodeProfile profile) => true;

    public bool CanAdvertiseRemoteWork(NodeProfile profile) =>
        profile.GPUUtilizationPercent < 50f;

    public long MaxModelCacheBytes => long.MaxValue;
    public long MaxSingleModelBytes => long.MaxValue;
    public int MaxConcurrentInferenceRequests => 8;

    public TimeSpan HotModelTTL => TimeSpan.FromHours(24);
    public TimeSpan ColdEvictionAge => TimeSpan.FromDays(90);

    public bool ShouldUnloadAfterInference(NodeProfile profile) => false;

    public bool CanRunIdleMaintenance(NodeProfile profile) => true;

    public bool AllowCUDA { get; init; } = true;
    public bool AllowROCm { get; init; } = true;
    public bool AllowVulkan { get; init; } = true;
    public bool UseHugePages { get; init; }
    public int OllamaNumGPU { get; init; } = -1;
    public bool IsContainerized { get; init; }
    public bool IsHeadless { get; init; }
}
