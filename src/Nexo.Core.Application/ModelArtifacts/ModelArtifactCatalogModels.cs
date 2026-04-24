namespace Nexo.Core.Application.ModelArtifacts;

/// <summary>Kind of discoverable model-related artifact.</summary>
public enum ModelArtifactKind
{
    /// <summary>Installed Ollama model (local daemon or Docker-probed).</summary>
    OllamaModel,

    /// <summary>Model published on the Ollama library (installable via <c>ollama pull</c>).</summary>
    OllamaRemoteLibraryModel,

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
