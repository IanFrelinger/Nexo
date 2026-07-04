using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;

namespace Nexo.Core.Application.NodeCapabilityRuntime.Ports;

/// <summary>Coordinates model load, unload, pull, and eviction across backends.</summary>
public interface IModelLifecycleManager
{
    /// <summary>Ensures a model is loaded, pulling if necessary.</summary>
    Task<bool> EnsureLoadedAsync(ModelDescriptor model, CancellationToken ct = default);

    /// <summary>Unloads a model from all backends.</summary>
    Task UnloadAsync(ModelDescriptor model);

    /// <summary>Pulls a model artifact with optional progress reporting.</summary>
    Task PullAsync(ModelDescriptor model, IProgress<PullProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Evicts a model from cache according to platform policy.</summary>
    Task EvictAsync(ModelDescriptor model);

    /// <summary>Runs idle maintenance (eviction, prefetch) when platform policy allows.</summary>
    Task RunIdleMaintenanceAsync(CancellationToken ct = default);
}
