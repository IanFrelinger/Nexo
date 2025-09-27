using System.Collections.Generic;

namespace Nexo.Feature.Networking.Models;

/// <summary>
/// Result and response models for networking functionality
/// </summary>
public partial class NetworkingRequest
{
    // This partial class contains result and response models
}

/// <summary>
/// Result of networking functionality generation
/// </summary>
public record NetworkingResult
{
    /// <summary>
    /// Whether the generation was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Generated networking configuration
    /// </summary>
    public NetworkingConfiguration? Configuration { get; init; }
    
    /// <summary>
    /// File path where configuration was saved
    /// </summary>
    public string? FilePath { get; init; }
    
    /// <summary>
    /// Configuration format (JSON, YAML, etc.)
    /// </summary>
    public string Format { get; init; } = "JSON";
    
    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Generation time in milliseconds
    /// </summary>
    public long GenerationTimeMs { get; init; }
}
