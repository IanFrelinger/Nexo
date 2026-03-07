using Nexo.Infrastructure.Execution;

namespace Nexo.Infrastructure.Execution.LoadPolicy;

/// <summary>
/// Preference for where to execute LLM requests: edge (local) or server (cloud).
/// </summary>
public enum LoadPreference
{
    /// <summary>Prefer local/edge (Ollama, ONNX, LLamaSharp).</summary>
    Edge,

    /// <summary>Prefer server/cloud (OpenAI, Azure).</summary>
    Server,

    /// <summary>Auto: evaluate at runtime (battery, availability, etc.).</summary>
    Auto
}

/// <summary>
/// Policy for deciding whether to route LLM requests to edge (local) or server (cloud).
/// </summary>
public interface ILoadPolicy
{
    /// <summary>
    /// Gets the current load preference.
    /// </summary>
    LoadPreference GetPreference();

    /// <summary>
    /// Resolves the effective provider for the given preference.
    /// Edge -> "ollama" or "local"; Server -> "openai" or "azure".
    /// </summary>
    /// <param name="providerFactory">Factory to check availability.</param>
    /// <returns>Provider name to use, or null if none available.</returns>
    string? ResolveProvider(IProviderFactory providerFactory);
}
