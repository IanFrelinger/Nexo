using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.NodeCapabilityRuntime.Models;

namespace Nexo.Core.Application.NodeCapabilityRuntime.Ports;

/// <summary>Backend adapter for local or remote model inference and lifecycle operations.</summary>
public interface IModelServingBackend
{
    /// <summary>Backend identifier (Ollama, llama.cpp, cloud API, etc.).</summary>
    BackendType BackendType { get; }

    /// <summary>Whether the backend is reachable and ready.</summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>Runs a single inference request against a loaded model.</summary>
    Task<InferenceResult> RunInferenceAsync(InferenceRequest request, CancellationToken ct = default);

    /// <summary>Loads a model into memory on the backend.</summary>
    Task LoadModelAsync(string modelId, CancellationToken ct = default);

    /// <summary>Unloads a model from memory.</summary>
    Task UnloadModelAsync(string modelId, CancellationToken ct = default);

    /// <summary>Pulls a model artifact from a remote registry.</summary>
    Task PullModelAsync(string modelId, IProgress<PullProgress>? progress = null, CancellationToken ct = default);

    /// <summary>Lists model ids currently loaded in the backend.</summary>
    Task<IReadOnlyList<string>> ListLoadedModelsAsync(CancellationToken ct = default);
}
