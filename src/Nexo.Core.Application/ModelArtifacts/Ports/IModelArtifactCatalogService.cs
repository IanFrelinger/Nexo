namespace Nexo.Core.Application.ModelArtifacts.Ports;

/// <summary>
/// Aggregates <see cref="IModelArtifactCatalogSource"/> implementations so callers
/// can discover models without depending on Ollama vs Docker directly.
/// </summary>
public interface IModelArtifactCatalogService
{
    Task<IReadOnlyList<ModelArtifactRecord>> ListAllAsync(CancellationToken cancellationToken = default);
}
