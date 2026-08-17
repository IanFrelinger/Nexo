namespace Nexo.AI.Pipeline;

/// <summary>
/// Configuration for the Microsoft.Extensions.AI pipeline.
/// Bound from <c>Nexo:Meai</c> (and feature flag <c>Nexo:UseMeaiPipeline</c>).
/// </summary>
public sealed class MeaiPipelineOptions
{
    /// <summary>Configuration section for nested MEAI options.</summary>
    public const string SectionName = "Nexo:Meai";

    /// <summary>Feature-flag configuration key.</summary>
    public const string FeatureFlagKey = "Nexo:UseMeaiPipeline";

    /// <summary>Environment variable that enables the MEAI pipeline when set to <c>1</c> or <c>true</c>.</summary>
    public const string FeatureFlagEnvVar = "NEXO_USE_MEAI_PIPELINE";

    /// <summary>Environment variable that overrides the Ollama base URL for this pipeline (highest precedence).</summary>
    public const string OllamaBaseUrlEnvVar = "NEXO_OLLAMA_BASE_URL";

    /// <summary>Environment variable that overrides the Ollama model id for this pipeline (highest precedence).</summary>
    public const string OllamaModelEnvVar = "NEXO_OLLAMA_MODEL";

    /// <summary>
    /// Legacy Ollama base URL environment variable read by the provider-factory path and set by every
    /// compose stack. Honoured after <see cref="OllamaBaseUrlEnvVar"/> and <see cref="OllamaBaseUrl"/>.
    /// </summary>
    public const string LegacyOllamaBaseUrlEnvVar = "OLLAMA_BASE_URL";

    /// <summary>Legacy Ollama model environment variable; same precedence as <see cref="LegacyOllamaBaseUrlEnvVar"/>.</summary>
    public const string LegacyOllamaModelEnvVar = "OLLAMA_MODEL";

    /// <summary>Effective Ollama base URL when nothing is configured (matches NexoDefaults.OllamaDefaultBaseUrl).</summary>
    public const string DefaultOllamaBaseUrl = "http://localhost:11434";

    /// <summary>Effective Ollama model when nothing is configured (matches NexoDefaults.OllamaDefaultModel).</summary>
    public const string DefaultOllamaModel = "llama3.1:latest";

    /// <summary>
    /// Ollama base URL from <c>Nexo:Meai:OllamaBaseUrl</c>. Null/empty means "not configured" so the
    /// resolver can fall through to the legacy env family; see <c>Clients.OllamaEndpointResolver</c>.
    /// </summary>
    public string? OllamaBaseUrl { get; set; }

    /// <summary>
    /// Ollama model id from <c>Nexo:Meai:OllamaModel</c>. Null/empty means "not configured"
    /// (same fallback chain as <see cref="OllamaBaseUrl"/>).
    /// </summary>
    public string? OllamaModel { get; set; }

    /// <summary>
    /// Path to a GGUF model for the <c>local:onnx</c> (LLamaSharp) target.
    /// Falls back to <c>NEXO_LOCAL_MODEL_PATH</c> when unset.
    /// </summary>
    public string? LocalModelPath { get; set; }

    /// <summary>Context size for LLamaSharp local inference.</summary>
    public int LocalContextSize { get; set; } = 2048;

    /// <summary>Max tokens for LLamaSharp local inference.</summary>
    public int LocalMaxTokens { get; set; } = 4096;

    /// <summary>AWS Bedrock tiered cloud targets.</summary>
    public BedrockMeaiOptions Bedrock { get; set; } = new();

    /// <summary>
    /// Explicit cloud target keys the default access policy should allow
    /// (e.g. <c>cloud:bedrock:balanced</c>). Empty = cloud denied.
    /// </summary>
    public List<string> AllowedCloudTargets { get; set; } = new();
}
