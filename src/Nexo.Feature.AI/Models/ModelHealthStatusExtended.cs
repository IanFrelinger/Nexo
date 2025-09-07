using System;
using System.Collections.Generic;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Extended health status information for an AI model
/// </summary>
public record ModelHealthStatusExtended : ModelHealthStatus
{
    /// <summary>
    /// Name of the provider
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;
    
    /// <summary>
    /// Last error that occurred
    /// </summary>
    public string? LastError { get; init; }
    
    /// <summary>
    /// When the health status was last checked (alias for LastChecked)
    /// </summary>
    public DateTime LastCheckTime => LastChecked;
}
