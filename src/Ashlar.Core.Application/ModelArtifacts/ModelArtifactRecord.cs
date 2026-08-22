namespace Ashlar.Core.Application.ModelArtifacts;

/// <summary>
/// One entry from a catalog source (e.g. Ollama <c>/api/tags</c> or a Docker-hosted Ollama).
/// </summary>
/// <param name="Id">Stable artifact identifier within the source.</param>
/// <param name="SourceId">Catalog source that produced this entry.</param>
/// <param name="Kind">Artifact kind (model, embedding, etc.).</param>
/// <param name="SizeHintBytes">Approximate size in bytes, when known.</param>
/// <param name="Metadata">Optional key-value metadata from the catalog.</param>
public sealed record ModelArtifactRecord(
    string Id,
    string SourceId,
    ModelArtifactKind Kind,
    long SizeHintBytes,
    IReadOnlyDictionary<string, string>? Metadata = null);
