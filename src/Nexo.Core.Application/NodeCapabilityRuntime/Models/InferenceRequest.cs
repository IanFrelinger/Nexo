namespace Nexo.Core.Application.NodeCapabilityRuntime.Models;

/// <summary>
/// Request payload for a single inference invocation.
/// </summary>
public sealed record InferenceRequest
{
    /// <summary>Identifier of the model to run.</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>User or task prompt sent to the model.</summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>Optional system prompt establishing model behavior.</summary>
    public string? SystemPrompt { get; init; }

    /// <summary>Backend-specific generation parameters (temperature, top-p, etc.).</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}
