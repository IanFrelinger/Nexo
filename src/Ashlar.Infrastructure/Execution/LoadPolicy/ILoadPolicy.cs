using Ashlar.Infrastructure.Execution;

namespace Ashlar.Infrastructure.Execution.LoadPolicy;

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
