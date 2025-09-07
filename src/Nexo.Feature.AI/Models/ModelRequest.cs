namespace Nexo.Feature.AI.Models;

/// <summary>
/// Request model for AI model execution
/// </summary>
public record ModelRequest
{
    /// <summary>
    /// The input text or prompt for the model
    /// </summary>
    public string Input { get; init; } = string.Empty;
    
    /// <summary>
    /// System prompt or context for the model
    /// </summary>
    public string? SystemPrompt { get; init; }
    
    /// <summary>
    /// Temperature for response generation (0.0 to 1.0)
    /// </summary>
    public double Temperature { get; init; } = 0.7;
    
    /// <summary>
    /// Maximum number of tokens to generate
    /// </summary>
    public int MaxTokens { get; init; } = 1000;
    
    /// <summary>
    /// Whether to include metadata in the response
    /// </summary>
    public bool IncludeMetadata { get; init; } = false;
    
    /// <summary>
    /// Additional context or parameters
    /// </summary>
    public Dictionary<string, object>? Context { get; init; }
}
