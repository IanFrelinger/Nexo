using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Policies;

/// <summary>Platform policy for Linux nodes (CUDA/ROCm/Vulkan, generous cache, headless/container aware).</summary>
public sealed class LinuxPolicy : IPlatformPolicy
{
    /// <summary>Platform.</summary>
    public PlatformType Platform => PlatformType.Linux;

    /// <summary>Whether run inference now is allowed.</summary>
    public bool CanRunInferenceNow(NodeProfile profile) =>
        profile.GPUUtilizationPercent < 98f;

    /// <summary>Whether load model is allowed.</summary>
    public bool CanLoadModel(ModelDescriptor model, NodeProfile profile) =>
        model.MinVRAMRequiredBytes <= profile.AvailableVRAMBytes &&
        model.MinRAMRequiredBytes <= profile.AvailableRAMBytes;

    /// <summary>Whether pull model is allowed.</summary>
    public bool CanPullModel(NodeProfile profile) => true;

    /// <summary>Whether advertise remote work is allowed.</summary>
    public bool CanAdvertiseRemoteWork(NodeProfile profile) =>
        profile.GPUUtilizationPercent < 50f;

    /// <summary>Max model cache bytes.</summary>
    public long MaxModelCacheBytes => long.MaxValue;
    /// <summary>Max single model bytes.</summary>
    public long MaxSingleModelBytes => long.MaxValue;
    /// <summary>Max concurrent inference requests.</summary>
    public int MaxConcurrentInferenceRequests => 8;

    /// <summary>Hot model t t l.</summary>
    public TimeSpan HotModelTTL => TimeSpan.FromHours(24);
    /// <summary>Cold eviction age.</summary>
    public TimeSpan ColdEvictionAge => TimeSpan.FromDays(90);

    /// <summary>Should unload after inference.</summary>
    public bool ShouldUnloadAfterInference(NodeProfile profile) => false;

    /// <summary>Whether run idle maintenance is allowed.</summary>
    public bool CanRunIdleMaintenance(NodeProfile profile) => true;

    /// <summary>Allow c u d a.</summary>
    public bool AllowCUDA { get; init; } = true;
    /// <summary>Allow r o cm.</summary>
    public bool AllowROCm { get; init; } = true;
    /// <summary>Allow vulkan.</summary>
    public bool AllowVulkan { get; init; } = true;
    /// <summary>Use huge pages.</summary>
    public bool UseHugePages { get; init; }
    /// <summary>Ollama num g p u.</summary>
    public int OllamaNumGPU { get; init; } = -1;
    /// <summary>Is containerized.</summary>
    public bool IsContainerized { get; init; }
    /// <summary>Is headless.</summary>
    public bool IsHeadless { get; init; }
}
