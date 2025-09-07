using System.Collections.Generic;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Result of model validation
/// </summary>
public record ModelValidationResult
{
    /// <summary>
    /// Whether the model is valid
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Model name
    /// </summary>
    public string ModelName { get; init; } = string.Empty;
    
    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; init; } = new();
    
    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; init; } = new();
    
    /// <summary>
    /// Validation score (0-100)
    /// </summary>
    public int Score { get; init; }
    
    /// <summary>
    /// Additional validation metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
