using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Core.Domain.Entities.Infrastructure;

namespace Nexo.Core.Application.Services.Environment;

/// <summary>
/// Interface for detecting and monitoring environment changes
/// </summary>
public interface IEnvironmentDetector
{
    /// <summary>
    /// Detect the current environment
    /// </summary>
    Task<DetectedEnvironment> DetectCurrentEnvironmentAsync();
    
    /// <summary>
    /// Get the current environment profile
    /// </summary>
    Task<EnvironmentProfile> GetCurrentEnvironmentAsync();
    
    /// <summary>
    /// Monitor for environment changes
    /// </summary>
    Task StartEnvironmentMonitoringAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stop environment monitoring
    /// </summary>
    Task StopEnvironmentMonitoringAsync();
    
    /// <summary>
    /// Check if environment has changed since last detection
    /// </summary>
    Task<bool> HasEnvironmentChangedAsync();
    
    /// <summary>
    /// Get environment change history
    /// </summary>
    Task<IEnumerable<EnvironmentChange>> GetEnvironmentChangeHistoryAsync(TimeSpan timeWindow);
    
    /// <summary>
    /// Event fired when environment changes
    /// </summary>
    event EventHandler<EnvironmentChangeEventArgs>? OnEnvironmentChange;
}


/// <summary>
/// Detected environment information
/// </summary>
public class DetectedEnvironment
{
    public string EnvironmentId { get; set; } = Guid.NewGuid().ToString();
    public Nexo.Core.Domain.Entities.Infrastructure.EnvironmentContext Context { get; set; } = new();
    public PlatformType Platform { get; set; }
    public EnvironmentResources Resources { get; set; } = new();
    public NetworkProfile NetworkProfile { get; set; } = new();
    public SecurityProfile SecurityProfile { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public bool HasChanged { get; set; }
}

/// <summary>
/// Environment resources information
/// </summary>
public class EnvironmentResources
{
    public int CpuCores { get; set; }
    public long TotalMemoryMB { get; set; }
    public long AvailableMemoryMB { get; set; }
    public long TotalDiskSpaceMB { get; set; }
    public long AvailableDiskSpaceMB { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public double DiskUtilization { get; set; }
    public bool IsResourceConstrained { get; set; }
}

/// <summary>
/// Security profile for environment
/// </summary>
public class SecurityProfile
{
    public SecurityLevel SecurityLevel { get; set; }
    public bool IsSecureConnection { get; set; }
    public bool HasAuthentication { get; set; }
    public bool HasEncryption { get; set; }
    public IEnumerable<string> SecurityFeatures { get; set; } = Enumerable.Empty<string>();
    public Dictionary<string, object> SecuritySettings { get; set; } = new();
}

/// <summary>
/// Environment optimization configuration
/// </summary>
public class EnvironmentOptimization
{
    public string OptimizationId { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public int Priority { get; set; }
    public bool IsEnabled { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Environment change record
/// </summary>
public class EnvironmentChange
{
    public string ChangeId { get; set; } = Guid.NewGuid().ToString();
    public string ChangeType { get; set; } = string.Empty;
    public DetectedEnvironment PreviousEnvironment { get; set; } = new();
    public DetectedEnvironment NewEnvironment { get; set; } = new();
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> ChangeDetails { get; set; } = new();
}

/// <summary>
/// Environment validation result
/// </summary>
public class EnvironmentValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<string> ValidationWarnings { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<string> Recommendations { get; set; } = Enumerable.Empty<string>();
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
}

// Enums
public enum SecurityLevel
{
    Low,
    Medium,
    High,
    Critical
}
