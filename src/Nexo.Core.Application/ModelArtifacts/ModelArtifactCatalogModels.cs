namespace Nexo.Core.Application.ModelArtifacts;

/// <summary>Kind of discoverable model-related artifact.</summary>
public enum ModelArtifactKind
{
    OllamaModel,
    DockerImage
}

/// <summary>
/// One entry from a catalog source (e.g. Ollama <c>/api/tags</c> or a Docker-hosted Ollama).
/// </summary>
public sealed record ModelArtifactRecord(
    string Id,
    string SourceId,
    ModelArtifactKind Kind,
    long SizeHintBytes,
    IReadOnlyDictionary<string, string>? Metadata = null);
