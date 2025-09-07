using System;
using System.Collections.Generic;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Extended information about an AI model with additional properties
/// </summary>
public record ModelInfoExtended : ModelInfo
{
    /// <summary>
    /// Unique identifier for the model
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of the model
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Version of the model
    /// </summary>
    public string Version { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of model (alias for ModelType)
    /// </summary>
    public ModelType Type => ModelType;
    
    /// <summary>
    /// Provider of the model
    /// </summary>
    public string Provider { get; init; } = string.Empty;
    
    /// <summary>
    /// When the model was last updated
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Additional metadata about the model
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
