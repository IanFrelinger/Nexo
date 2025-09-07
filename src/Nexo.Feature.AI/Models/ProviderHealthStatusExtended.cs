using System;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Extended health status information for an AI provider
/// </summary>
public record ProviderHealthStatusExtended : ProviderHealthStatus
{
    /// <summary>
    /// When the last health check was performed
    /// </summary>
    public DateTime LastHealthCheck { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Number of available models
    /// </summary>
    public int AvailableModelsCount { get; init; }
    
    /// <summary>
    /// Number of errors encountered
    /// </summary>
    public int ErrorCount { get; init; }
}
