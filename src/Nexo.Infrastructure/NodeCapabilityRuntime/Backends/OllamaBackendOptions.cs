namespace Nexo.Infrastructure.NodeCapabilityRuntime.Backends;

/// <summary>
/// Options for the NCR Ollama serving backend.
/// </summary>
public sealed class OllamaBackendOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Nexo:NodeCapabilityRuntime:Ollama";

    /// <summary>Base url.</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
}
