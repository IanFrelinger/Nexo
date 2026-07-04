namespace Nexo.Infrastructure.Execution.Ollama;

/// <summary>Normalized Ollama model metadata used for capability routing and lifecycle.</summary>
/// <param name="Name">Model name including tag.</param>
/// <param name="Size">On-disk size in bytes.</param>
/// <param name="ModifiedAt">Last modification timestamp, when known.</param>
public sealed record OllamaModelManifest(
    string Name,
    long Size,
    DateTimeOffset? ModifiedAt);
