using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;

namespace Nexo.Core.Application.NodeCapabilityRuntime.Ports;

public interface INodeCapabilityRuntime
{
    NodeProfile CurrentProfile { get; }

    Task<ModelResolution> SelectModelAsync(TaskContext context, CancellationToken ct = default);

    Task EnsureModelReadyAsync(ModelDescriptor model, CancellationToken ct = default);

    Task<NodeTier> GetTierAsync();

    IObservable<ConstraintUpdate> ConstraintChanges { get; }

    IReadOnlyList<ModelDescriptor> AvailableModels { get; }

    NodeCapabilityManifest GetCapabilityManifest();

    Task RecordOutcomeAsync(
        ModelResolution resolution,
        BrickExecutionOutcome outcome,
        CancellationToken ct = default);
}

public interface IPlatformPolicy
{
    PlatformType Platform { get; }

    bool CanRunInferenceNow(NodeProfile profile);
    bool CanLoadModel(ModelDescriptor model, NodeProfile profile);
    bool CanPullModel(NodeProfile profile);
    bool CanAdvertiseRemoteWork(NodeProfile profile);

    long MaxModelCacheBytes { get; }
    long MaxSingleModelBytes { get; }
    int MaxConcurrentInferenceRequests { get; }

    TimeSpan HotModelTTL { get; }
    TimeSpan ColdEvictionAge { get; }
    bool ShouldUnloadAfterInference(NodeProfile profile);
    bool CanRunIdleMaintenance(NodeProfile profile);
}

public interface IHardwareProfiler
{
    Task<NodeProfile> CaptureAsync(CancellationToken ct = default);
}

public interface IModelServingBackend
{
    BackendType BackendType { get; }
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task<InferenceResult> RunInferenceAsync(InferenceRequest request, CancellationToken ct = default);
    Task LoadModelAsync(string modelId, CancellationToken ct = default);
    Task UnloadModelAsync(string modelId, CancellationToken ct = default);
    Task PullModelAsync(string modelId, IProgress<PullProgress>? progress = null, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListLoadedModelsAsync(CancellationToken ct = default);
}

public interface IModelLifecycleManager
{
    Task<bool> EnsureLoadedAsync(ModelDescriptor model, CancellationToken ct = default);
    Task UnloadAsync(ModelDescriptor model);
    Task PullAsync(ModelDescriptor model, IProgress<PullProgress>? progress = null, CancellationToken ct = default);
    Task EvictAsync(ModelDescriptor model);
    Task RunIdleMaintenanceAsync(CancellationToken ct = default);
}
