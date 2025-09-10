using System;

namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Environment profile for system configuration
/// </summary>
public record EnvironmentProfile
{
    /// <summary>
    /// Platform type
    /// </summary>
    public PlatformType PlatformType { get; init; }
    
    /// <summary>
    /// Environment type (alias for PlatformType)
    /// </summary>
    public PlatformType EnvironmentType => PlatformType;
    
    /// <summary>
    /// Environment identifier
    /// </summary>
    public string EnvironmentId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Environment name (Development, Production, etc.)
    /// </summary>
    public string EnvironmentName { get; init; } = string.Empty;
    
    /// <summary>
    /// CPU cores available
    /// </summary>
    public int CpuCores { get; init; }
    
    /// <summary>
    /// Available memory in MB
    /// </summary>
    public long AvailableMemoryMB { get; init; }
    
    /// <summary>
    /// Whether running in debug mode
    /// </summary>
    public bool IsDebugMode { get; init; }
    
    /// <summary>
    /// Framework version
    /// </summary>
    public string FrameworkVersion { get; init; } = string.Empty;
    
    /// <summary>
    /// Network profile
    /// </summary>
    public NetworkProfile NetworkProfile { get; init; } = new();
    
    /// <summary>
    /// Context information
    /// </summary>
    public string Context { get; init; } = "Default";
    
    /// <summary>
    /// Platform information
    /// </summary>
    public PlatformType Platform { get; init; } = PlatformType.DotNet;
    
    /// <summary>
    /// Resource information
    /// </summary>
    public string Resources { get; init; } = "Default";
    
    /// <summary>
    /// Additional environment properties
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
    
    /// <summary>
    /// Whether the environment is resource-constrained
    /// </summary>
    public bool IsConstrained { get; init; }
    
    /// <summary>
    /// Whether the environment is mobile
    /// </summary>
    public bool IsMobile { get; init; }
    
    /// <summary>
    /// Whether the environment has changed
    /// </summary>
    public bool HasChanged { get; init; }
    
}

/// <summary>
/// Network profile information
/// </summary>
public record NetworkProfile
{
    /// <summary>
    /// Network latency in milliseconds
    /// </summary>
    public double LatencyMs { get; init; } = 0;
    
    /// <summary>
    /// Network latency (alias for LatencyMs)
    /// </summary>
    public double Latency => LatencyMs;
    
    /// <summary>
    /// Bandwidth in Mbps
    /// </summary>
    public double BandwidthMbps { get; init; } = 0;
    
    /// <summary>
    /// Bandwidth (alias for BandwidthMbps)
    /// </summary>
    public double Bandwidth => BandwidthMbps;
    
    /// <summary>
    /// Whether network is reliable
    /// </summary>
    public bool IsReliable { get; init; } = true;
    
    /// <summary>
    /// Connection type
    /// </summary>
    public string ConnectionType { get; init; } = "Unknown";
}

/// <summary>
/// Runtime environment profile (alias for EnvironmentProfile)
/// </summary>
public record RuntimeEnvironmentProfile : EnvironmentProfile
{
    /// <summary>
    /// Current platform (alias for PlatformType)
    /// </summary>
    public PlatformType CurrentPlatform => PlatformType;
    
    /// <summary>
    /// Processor count
    /// </summary>
    public int ProcessorCount { get; init; } = Environment.ProcessorCount;
    
    /// <summary>
    /// Whether environment is constrained
    /// </summary>
    public new bool IsConstrained { get; init; } = false;
    
    /// <summary>
    /// Whether environment is mobile
    /// </summary>
    public new bool IsMobile { get; init; } = false;
    
    /// <summary>
    /// Whether environment is web-based
    /// </summary>
    public bool IsWeb { get; init; } = false;
    
    /// <summary>
    /// Whether environment is Unity
    /// </summary>
    public bool IsUnity { get; init; } = false;
    
    /// <summary>
    /// Optimization level
    /// </summary>
    public OptimizationLevel OptimizationLevel { get; init; } = OptimizationLevel.None;
}
