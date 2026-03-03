namespace Nexo.Core.Application.Ephemeral.Ports;

/// <summary>
/// Port for ephemeral model lifecycle. Starts/stops model containers per session.
/// When NEXO_EPHEMERAL_MODELS=1, use this instead of long-lived OLLAMA_BASE_URL.
/// </summary>
public interface IEphemeralModelLifecycle
{
    /// <summary>
    /// Starts an ephemeral session. Returns a scope that stops the container when disposed.
    /// Call from the entry point (e.g. OrchestrateCommand) before any LLM requests.
    /// </summary>
    /// <param name="model">Optional model name (e.g. llama3.2). Ignored for Ollama—use OLLAMA_MODEL env.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Disposable scope. Dispose to stop the container.</returns>
    Task<IAsyncDisposable> StartSessionAsync(string? model = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the base URL for the current session. Must be called within an active session
    /// started by <see cref="StartSessionAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Base URL (e.g. http://localhost:12345).</returns>
    Task<string> GetBaseUrlAsync(CancellationToken cancellationToken = default);
}
