namespace Nexo.Infrastructure.NodeCapabilityRuntime.Backends;

/// <summary>
/// Options for the NCR Ollama serving backend.
/// </summary>
public sealed class OllamaBackendOptions
{
    public const string SectionName = "Nexo:NodeCapabilityRuntime:Ollama";

    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
}
