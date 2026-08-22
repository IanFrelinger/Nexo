using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;

namespace Ashlar.Infrastructure.NodeCapabilityRuntime.Lifecycle;

/// <summary>
/// Default lifecycle manager delegating to model serving backend.
/// </summary>
public sealed class DefaultModelLifecycleManager : IModelLifecycleManager
{
    private readonly IModelServingBackend _backend;

    /// <summary>Initializes a new default model lifecycle manager.</summary>
    public DefaultModelLifecycleManager(IModelServingBackend backend)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    /// <summary>Ensure loaded asynchronously.</summary>
    public async Task<bool> EnsureLoadedAsync(ModelDescriptor model, CancellationToken ct = default)
    {
        var loaded = await _backend.ListLoadedModelsAsync(ct).ConfigureAwait(false);
        if (!loaded.Contains(model.Id, StringComparer.OrdinalIgnoreCase))
        {
            await _backend.LoadModelAsync(model.Id, ct).ConfigureAwait(false);
        }

        return true;
    }

    /// <summary>Unload asynchronously.</summary>
    public Task UnloadAsync(ModelDescriptor model)
        => _backend.UnloadModelAsync(model.Id);

    /// <summary>Pull asynchronously.</summary>
    public Task PullAsync(ModelDescriptor model, IProgress<PullProgress>? progress = null, CancellationToken ct = default)
        => _backend.PullModelAsync(model.Id, progress, ct);

    /// <summary>Evict asynchronously.</summary>
    public Task EvictAsync(ModelDescriptor model)
        => _backend.UnloadModelAsync(model.Id);

    /// <summary>Run idle maintenance asynchronously.</summary>
    public Task RunIdleMaintenanceAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
