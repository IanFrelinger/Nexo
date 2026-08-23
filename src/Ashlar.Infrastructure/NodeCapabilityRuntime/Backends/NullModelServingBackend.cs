using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;

namespace Ashlar.Infrastructure.NodeCapabilityRuntime.Backends;

/// <summary>
/// No-op backend used as a safe default for runtime wiring and tests.
/// </summary>
public sealed class NullModelServingBackend : IModelServingBackend
{
    private readonly HashSet<string> _loaded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Backend type.</summary>
    public BackendType BackendType => BackendType.OnnxRuntime;

    /// <summary>Is available asynchronously.</summary>
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(true);
    }

    /// <summary>Run inference asynchronously.</summary>
    public Task<InferenceResult> RunInferenceAsync(InferenceRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(new InferenceResult
        {
            Output = $"[{request.ModelId}] simulated inference output",
            Duration = TimeSpan.FromMilliseconds(10)
        });
    }

    /// <summary>Load model asynchronously.</summary>
    public Task LoadModelAsync(string modelId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _loaded.Add(modelId);
        return Task.CompletedTask;
    }

    /// <summary>Unload model asynchronously.</summary>
    public Task UnloadModelAsync(string modelId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _loaded.Remove(modelId);
        return Task.CompletedTask;
    }

    /// <summary>Pull model asynchronously.</summary>
    public Task PullModelAsync(string modelId, IProgress<PullProgress>? progress = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        progress?.Report(new PullProgress { ModelId = modelId, BytesDownloaded = 1, TotalBytes = 1 });
        return Task.CompletedTask;
    }

    /// <summary>List loaded models asynchronously.</summary>
    public Task<IReadOnlyList<string>> ListLoadedModelsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<string>>(_loaded.ToArray());
    }
}
