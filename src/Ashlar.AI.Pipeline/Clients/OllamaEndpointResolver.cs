namespace Ashlar.AI.Pipeline.Clients;

/// <summary>
/// Resolves the Ollama endpoint used by the default (MEAI) model path.
/// <para>
/// Resolution order (first non-empty value wins):
/// <c>ASHLAR_OLLAMA_BASE_URL</c> / <c>ASHLAR_OLLAMA_MODEL</c> env →
/// <c>Ashlar:Meai:OllamaBaseUrl</c> / <c>Ashlar:Meai:OllamaModel</c> config →
/// legacy <c>OLLAMA_BASE_URL</c> / <c>OLLAMA_MODEL</c> env →
/// <see cref="MeaiPipelineOptions.DefaultOllamaBaseUrl"/> / <see cref="MeaiPipelineOptions.DefaultOllamaModel"/>.
/// </para>
/// <para>
/// The legacy family is what the provider-factory path reads and what every compose stack and most
/// docs set. Without this fallback the default model path inside a container dialled its own
/// <c>localhost:11434</c> (connection refused) while <c>/api/onboarding/status</c> reported Ollama
/// available, because the two paths resolved from different variables.
/// </para>
/// </summary>
public static class OllamaEndpointResolver
{
    /// <summary>Resolves the effective Ollama base URL (no trailing slash).</summary>
    /// <param name="options">Bound <c>Ashlar:Meai</c> options; may be null when the section is absent.</param>
    /// <returns>The first configured value in precedence order, or the default.</returns>
    public static string ResolveBaseUrl(MeaiPipelineOptions? options)
    {
        var value = FirstConfigured(
            Environment.GetEnvironmentVariable(MeaiPipelineOptions.OllamaBaseUrlEnvVar),
            options?.OllamaBaseUrl,
            Environment.GetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaBaseUrlEnvVar))
            ?? MeaiPipelineOptions.DefaultOllamaBaseUrl;
        return value.TrimEnd('/');
    }

    /// <summary>Resolves the effective Ollama model id.</summary>
    /// <param name="options">Bound <c>Ashlar:Meai</c> options; may be null when the section is absent.</param>
    /// <returns>The first configured value in precedence order, or the default.</returns>
    public static string ResolveModel(MeaiPipelineOptions? options)
    {
        return FirstConfigured(
            Environment.GetEnvironmentVariable(MeaiPipelineOptions.OllamaModelEnvVar),
            options?.OllamaModel,
            Environment.GetEnvironmentVariable(MeaiPipelineOptions.LegacyOllamaModelEnvVar))
            ?? MeaiPipelineOptions.DefaultOllamaModel;
    }

    private static string? FirstConfigured(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim();
            }
        }

        return null;
    }
}
