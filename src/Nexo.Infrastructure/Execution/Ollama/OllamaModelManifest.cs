namespace Nexo.Infrastructure.Execution.Ollama;

public sealed record OllamaModelManifest(
    string Name,
    long Size,
    DateTimeOffset? ModifiedAt);
