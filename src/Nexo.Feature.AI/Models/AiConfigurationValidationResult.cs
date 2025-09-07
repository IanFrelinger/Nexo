using System.Collections.Generic;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Result of AI configuration validation
/// </summary>
public record AiConfigurationValidationResult
{
    /// <summary>
    /// Whether the configuration is valid
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; init; } = new();
    
    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; init; } = new();
    
    /// <summary>
    /// Overall validation message
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
