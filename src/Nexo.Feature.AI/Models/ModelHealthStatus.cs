namespace Nexo.Feature.AI.Models;

/// <summary>
/// Health status information for an AI model
/// </summary>
public record ModelHealthStatus
{
    /// <summary>
    /// Whether the model is healthy and operational
    /// </summary>
    public bool IsHealthy { get; init; } = true;
    
    /// <summary>
    /// Status message describing the current state
    /// </summary>
    public string Status { get; init; } = "Healthy";
    
    /// <summary>
    /// When the health status was last checked
    /// </summary>
    public DateTime LastChecked { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; init; }
    
    /// <summary>
    /// Error rate percentage (0-100)
    /// </summary>
    public double ErrorRate { get; init; }
    
    /// <summary>
    /// Provider ID for this model
    /// </summary>
    public string ProviderId { get; init; } = string.Empty;
    
    /// <summary>
    /// Additional health metrics
    /// </summary>
    public Dictionary<string, object>? Metrics { get; init; }
}
