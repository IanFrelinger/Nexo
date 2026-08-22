namespace Ashlar.Core.Application.ModelArtifacts.Ports;

/// <summary>
/// Aggregates <see cref="IModelArtifactCatalogSource"/> implementations so callers
/// can discover models without depending on Ollama vs Docker directly.
/// </summary>
public interface IModelArtifactCatalogService
{
    /// <summary>
    /// Locally installed or discoverable artifacts (e.g. Ollama <c>/api/tags</c> on this host, Docker Ollama).
    /// Does not include remote library listings.
    /// </summary>
    Task<IReadOnlyList<ModelArtifactRecord>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Models published for install (e.g. Ollama library at ollama.com). Requires network access.
    /// </summary>
    Task<IReadOnlyList<ModelArtifactRecord>> ListInstallableAsync(CancellationToken cancellationToken = default);
}
