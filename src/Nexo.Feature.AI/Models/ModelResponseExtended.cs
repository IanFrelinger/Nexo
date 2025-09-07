using System.Collections.Generic;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Extended response model from AI model execution with additional properties
/// </summary>
public record ModelResponseExtended : ModelResponse
{
    /// <summary>
    /// The model that generated the response
    /// </summary>
    public string Model { get; init; } = string.Empty;
    
    /// <summary>
    /// Total tokens used (input + output)
    /// </summary>
    public int TotalTokens { get; init; }
    
    /// <summary>
    /// The generated content (alias for Response)
    /// </summary>
    public string Content => Response;
}
