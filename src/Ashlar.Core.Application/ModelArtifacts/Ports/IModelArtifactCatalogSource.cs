namespace Ashlar.Core.Application.ModelArtifacts.Ports;

/// <summary>
/// Optional dependency that lists model artifacts from a concrete backend
/// (Ollama HTTP API, Docker engine, etc.).
/// </summary>
public interface IModelArtifactCatalogSource
{
    /// <summary>Stable id for filtering and diagnostics (e.g. <c>ollama-tags</c>).</summary>
    string SourceId { get; }

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModelArtifactRecord>> ListAsync(CancellationToken cancellationToken = default);
}
