namespace Nexo.Feature.AI.Models;

/// <summary>
/// Response model from AI model execution
/// </summary>
public record ModelResponse
{
    /// <summary>
    /// The generated response text
    /// </summary>
    public string Response { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; init; } = true;
    
    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Number of input tokens used
    /// </summary>
    public int InputTokens { get; init; }
    
    /// <summary>
    /// Number of output tokens generated
    /// </summary>
    public int OutputTokens { get; init; }
    
    /// <summary>
    /// Total cost of the request (if available)
    /// </summary>
    public decimal? Cost { get; init; }
    
    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; init; }
    
    /// <summary>
    /// Provider ID for this response
    /// </summary>
    public string ProviderId { get; init; } = string.Empty;
    
    /// <summary>
    /// Model name that generated this response
    /// </summary>
    public string ModelName { get; init; } = string.Empty;
    
    /// <summary>
    /// Additional metadata from the model
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
