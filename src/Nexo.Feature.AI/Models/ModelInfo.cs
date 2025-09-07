using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Information about an AI model
/// </summary>
public record ModelInfo
{
    /// <summary>
    /// Unique identifier for the model
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Human-readable display name
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of model
    /// </summary>
    public ModelType ModelType { get; init; }
    
    /// <summary>
    /// Whether the model is currently available
    /// </summary>
    public bool IsAvailable { get; init; } = true;
    
    /// <summary>
    /// Model size in bytes
    /// </summary>
    public long SizeBytes { get; init; }
    
    /// <summary>
    /// Maximum context length in tokens
    /// </summary>
    public int MaxContextLength { get; init; }
    
    /// <summary>
    /// Model capabilities and features
    /// </summary>
    public ModelCapabilities Capabilities { get; init; } = new();
}
