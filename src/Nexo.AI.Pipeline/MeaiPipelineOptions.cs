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

    /// <summary>Ollama base URL (default localhost:11434).</summary>
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>Default Ollama model id.</summary>
    public string OllamaModel { get; set; } = "llama3.1:latest";

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
