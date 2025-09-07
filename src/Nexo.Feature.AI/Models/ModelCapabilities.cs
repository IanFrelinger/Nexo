namespace Nexo.Feature.AI.Models;

/// <summary>
/// Capabilities and features of an AI model
/// </summary>
public record ModelCapabilities
{
    /// <summary>
    /// Whether the model supports text generation
    /// </summary>
    public bool SupportsTextGeneration { get; init; } = true;
    
    /// <summary>
    /// Whether the model supports code generation
    /// </summary>
    public bool SupportsCodeGeneration { get; init; } = false;
    
    /// <summary>
    /// Whether the model supports analysis tasks
    /// </summary>
    public bool SupportsAnalysis { get; init; } = false;
    
    /// <summary>
    /// Whether the model supports optimization tasks
    /// </summary>
    public bool SupportsOptimization { get; init; } = false;
    
    /// <summary>
    /// Whether the model supports streaming responses
    /// </summary>
    public bool SupportsStreaming { get; init; } = false;
}
