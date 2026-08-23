namespace Ashlar.Infrastructure.Execution.Ollama;

/// <summary>Structured error payload for Ollama provider operations.</summary>
/// <param name="Code">Canonical error code.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Metadata">Optional diagnostic metadata.</param>
public sealed record Error(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string>? Metadata = null);
