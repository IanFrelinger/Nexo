using Nexo.Core.Application.NodeCapabilityRuntime.Models;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;

namespace Nexo.Infrastructure.NodeCapabilityRuntime.Lifecycle;

/// <summary>
/// Default lifecycle manager delegating to model serving backend.
/// </summary>
public sealed class DefaultModelLifecycleManager : IModelLifecycleManager
{
    private readonly IModelServingBackend _backend;

    public DefaultModelLifecycleManager(IModelServingBackend backend)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    public async Task<bool> EnsureLoadedAsync(ModelDescriptor model, CancellationToken ct = default)
    {
        var loaded = await _backend.ListLoadedModelsAsync(ct).ConfigureAwait(false);
        if (!loaded.Contains(model.Id, StringComparer.OrdinalIgnoreCase))
        {
            await _backend.LoadModelAsync(model.Id, ct).ConfigureAwait(false);
        }

        return true;
    }

    public Task UnloadAsync(ModelDescriptor model)
        => _backend.UnloadModelAsync(model.Id);

    public Task PullAsync(ModelDescriptor model, IProgress<PullProgress>? progress = null, CancellationToken ct = default)
        => _backend.PullModelAsync(model.Id, progress, ct);

    public Task EvictAsync(ModelDescriptor model)
        => _backend.UnloadModelAsync(model.Id);

    public Task RunIdleMaintenanceAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
