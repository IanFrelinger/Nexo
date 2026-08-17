namespace Nexo.Infrastructure.NodeCapabilityRuntime.Backends;

/// <summary>
/// Options for the NCR Ollama serving backend.
/// </summary>
public sealed class OllamaBackendOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Nexo:NodeCapabilityRuntime:Ollama";

    /// <summary>Base URL used when nothing is configured (loopback; Ollama typically runs as a local sidecar).</summary>
    public const string DefaultBaseUrl = "http://127.0.0.1:11434";

    /// <summary>Base url.</summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>
    /// Applies the same precedence the MEAI model path uses so all Ollama families point at one host:
    /// <c>NEXO_OLLAMA_BASE_URL</c> env → <c>Nexo:NodeCapabilityRuntime:Ollama:BaseUrl</c> config →
    /// legacy <c>OLLAMA_BASE_URL</c> env → <see cref="DefaultBaseUrl"/>. Compose stacks set only the
    /// legacy family, so without this fallback the NCR probe dialled the container's own loopback.
    /// </summary>
    /// <param name="configuredBaseUrl">Value bound from <see cref="SectionName"/>; null/empty when the key is absent.</param>
    /// <returns>The first non-empty value in precedence order (trailing slash trimmed).</returns>
    public static string ResolveBaseUrl(string? configuredBaseUrl)
    {
        foreach (var candidate in new[]
                 {
                     Environment.GetEnvironmentVariable("NEXO_OLLAMA_BASE_URL"),
                     configuredBaseUrl,
                     Environment.GetEnvironmentVariable("OLLAMA_BASE_URL"),
                 })
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim().TrimEnd('/');
            }
        }

        return DefaultBaseUrl;
    }
}
