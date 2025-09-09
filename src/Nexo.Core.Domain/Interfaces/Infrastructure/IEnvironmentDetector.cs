using System;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Domain.Interfaces.Infrastructure;

/// <summary>
/// Interface for detecting and monitoring environment changes
/// </summary>
public interface IEnvironmentDetector
{
    /// <summary>
    /// Get current environment profile
    /// </summary>
    Task<EnvironmentProfile> GetCurrentEnvironmentAsync();
    
    /// <summary>
    /// Detect environment changes
    /// </summary>
    Task<EnvironmentChange> DetectEnvironmentChangeAsync();
    
    /// <summary>
    /// Start monitoring environment changes
    /// </summary>
    Task StartMonitoringAsync();
    
    /// <summary>
    /// Stop monitoring environment changes
    /// </summary>
    Task StopMonitoringAsync();
    
    /// <summary>
    /// Get environment history
    /// </summary>
    Task<IEnumerable<DetectedEnvironment>> GetEnvironmentHistoryAsync(TimeSpan? timeWindow = null);
    
    /// <summary>
    /// Check if environment is stable
    /// </summary>
    Task<bool> IsEnvironmentStableAsync();
    
    /// <summary>
    /// Get environment context for a specific operation
    /// </summary>
    Task<EnvironmentContext> GetEnvironmentContextAsync(string operation);
    
    /// <summary>
    /// Event raised when environment changes
    /// </summary>
    event EventHandler<EnvironmentChangeEventArgs>? OnEnvironmentChange;
    
    /// <summary>
    /// Handle environment change
    /// </summary>
    void HandleEnvironmentChange(object sender, EnvironmentChangeEventArgs e);
}

/// <summary>
/// Environment change information
/// </summary>
public record EnvironmentChange
{
    /// <summary>
    /// Type of change detected
    /// </summary>
    public EnvironmentChangeType ChangeType { get; init; }
    
    /// <summary>
    /// Description of the change
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Previous environment state
    /// </summary>
    public EnvironmentProfile? PreviousState { get; init; }
    
    /// <summary>
    /// Current environment state
    /// </summary>
    public EnvironmentProfile CurrentState { get; init; } = new();
    
    /// <summary>
    /// Timestamp when change was detected
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Changed at timestamp (alias for Timestamp)
    /// </summary>
    public DateTime ChangedAt => Timestamp;
    
    /// <summary>
    /// Impact level of the change
    /// </summary>
    public ChangeImpact Impact { get; init; }
}

/// <summary>
/// Types of environment changes
/// </summary>
public enum EnvironmentChangeType
{
    /// <summary>
    /// Platform change
    /// </summary>
    Platform,
    
    /// <summary>
    /// Resource change (CPU, memory)
    /// </summary>
    Resource,
    
    /// <summary>
    /// Network change
    /// </summary>
    Network,
    
    /// <summary>
    /// Configuration change
    /// </summary>
    Configuration,
    
    /// <summary>
    /// Performance change
    /// </summary>
    Performance
}

/// <summary>
/// Impact levels for environment changes
/// </summary>
public enum ChangeImpact
{
    /// <summary>
    /// Low impact
    /// </summary>
    Low,
    
    /// <summary>
    /// Medium impact
    /// </summary>
    Medium,
    
    /// <summary>
    /// High impact
    /// </summary>
    High,
    
    /// <summary>
    /// Critical impact
    /// </summary>
    Critical
}

/// <summary>
/// Detected environment information
/// </summary>
public record DetectedEnvironment
{
    /// <summary>
    /// Environment profile
    /// </summary>
    public EnvironmentProfile Profile { get; init; } = new();
    
    /// <summary>
    /// Environment identifier (alias for Profile.EnvironmentId)
    /// </summary>
    public string EnvironmentId => Profile.EnvironmentId;
    
    /// <summary>
    /// Environment type (alias for Profile.EnvironmentType)
    /// </summary>
    public PlatformType EnvironmentType => Profile.EnvironmentType;
    
    /// <summary>
    /// Properties (alias for Profile.Properties)
    /// </summary>
    public Dictionary<string, object> Properties => Profile.Properties;
    
    /// <summary>
    /// Timestamp when environment was detected
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Detected at timestamp (alias for Timestamp)
    /// </summary>
    public DateTime DetectedAt => Timestamp;
    
    /// <summary>
    /// Confidence level of detection (0-1)
    /// </summary>
    public double Confidence { get; init; }
}

/// <summary>
/// Environment context for operations
/// </summary>
public record EnvironmentContext
{
    /// <summary>
    /// Platform type
    /// </summary>
    public PlatformType PlatformType { get; init; }
    
    /// <summary>
    /// Network profile
    /// </summary>
    public NetworkProfile NetworkProfile { get; init; } = new();
    
    /// <summary>
    /// Resource constraints
    /// </summary>
    public ResourceConstraints ResourceConstraints { get; init; } = new();
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public PerformanceRequirements PerformanceRequirements { get; init; } = new();
    
    /// <summary>
    /// Development environment context
    /// </summary>
    public static EnvironmentContext Development => new() { PlatformType = PlatformType.DotNet };
    
    /// <summary>
    /// Production environment context
    /// </summary>
    public static EnvironmentContext Production => new() { PlatformType = PlatformType.DotNet };
}

/// <summary>
/// Resource constraints
/// </summary>
public record ResourceConstraints
{
    /// <summary>
    /// Maximum CPU usage percentage
    /// </summary>
    public double MaxCpuUsage { get; init; } = 80.0;
    
    /// <summary>
    /// Maximum memory usage in MB
    /// </summary>
    public long MaxMemoryUsage { get; init; } = 1024;
    
    /// <summary>
    /// Maximum execution time in milliseconds
    /// </summary>
    public int MaxExecutionTime { get; init; } = 5000;
}

/// <summary>
/// Event arguments for environment changes
/// </summary>
public class EnvironmentChangeEventArgs : EventArgs
{
    /// <summary>
    /// The environment change that occurred
    /// </summary>
    public EnvironmentChange Change { get; }
    
    /// <summary>
    /// Type of change
    /// </summary>
    public string ChangeType { get; }
    
    public EnvironmentChangeEventArgs(EnvironmentChange change)
    {
        Change = change;
        ChangeType = change.ChangeType.ToString();
    }
}
