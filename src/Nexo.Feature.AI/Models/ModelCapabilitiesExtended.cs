using System.Collections.Generic;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Extended capabilities and features of an AI model
/// </summary>
public record ModelCapabilitiesExtended : ModelCapabilities
{
    /// <summary>
    /// Whether the model supports function calling
    /// </summary>
    public bool SupportsFunctionCalling { get; init; } = false;
    
    /// <summary>
    /// Whether the model supports text embedding
    /// </summary>
    public bool SupportsTextEmbedding { get; init; } = false;
    
    /// <summary>
    /// Maximum input length in tokens
    /// </summary>
    public int MaxInputLength { get; init; } = 4096;
    
    /// <summary>
    /// Maximum output length in tokens
    /// </summary>
    public int MaxOutputLength { get; init; } = 1000;
    
    /// <summary>
    /// List of supported languages
    /// </summary>
    public List<string> SupportedLanguages { get; init; } = new();
}
