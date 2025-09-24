using System.Collections.Generic;
using Nexo.Feature.AI.Enums;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Configuration settings for AI operations
/// </summary>
public record AiConfiguration
{
    /// <summary>
    /// The AI mode (Development, Production, etc.)
    /// </summary>
    public Nexo.Feature.AI.Enums.AiMode Mode { get; init; } = Nexo.Feature.AI.Enums.AiMode.Development;
    
    /// <summary>
    /// Default model configuration
    /// </summary>
    public ModelConfiguration DefaultModel { get; init; } = new();
    
    /// <summary>
    /// Available model providers
    /// </summary>
    public List<ProviderConfiguration> Providers { get; init; } = new();
    
    /// <summary>
    /// Resource allocation settings
    /// </summary>
    public ResourceConfiguration Resources { get; init; } = new();
    
    /// <summary>
    /// Caching configuration
    /// </summary>
    public CacheConfiguration Cache { get; init; } = new();
    
    /// <summary>
    /// Fallback configuration
    /// </summary>
    public FallbackConfiguration Fallback { get; init; } = new();
    
    /// <summary>
    /// Monitoring and logging configuration
    /// </summary>
    public MonitoringConfiguration Monitoring { get; init; } = new();
}

/// <summary>
/// Model configuration settings
/// </summary>
public record ModelConfiguration
{
    /// <summary>
    /// Default model name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Model type
    /// </summary>
    public Enums.ModelType Type { get; init; } = Enums.ModelType.TextGeneration;
    
    /// <summary>
    /// Default temperature setting
    /// </summary>
    public double Temperature { get; init; } = 0.7;
    
    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int MaxTokens { get; init; } = 1000;
}

/// <summary>
/// Provider configuration settings
/// </summary>
public record ProviderConfiguration
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Provider type
    /// </summary>
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// API endpoint
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;
    
    /// <summary>
    /// API key (encrypted)
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the provider is enabled
    /// </summary>
    public bool IsEnabled { get; init; } = true;
    
    /// <summary>
    /// Priority for provider selection
    /// </summary>
    public int Priority { get; init; } = 0;
}

/// <summary>
/// Resource allocation configuration
/// </summary>
public record ResourceConfiguration
{
    /// <summary>
    /// Maximum concurrent requests
    /// </summary>
    public int MaxConcurrentRequests { get; init; } = 10;
    
    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; init; } = 30;
    
    /// <summary>
    /// Memory limit in MB
    /// </summary>
    public int MemoryLimitMB { get; init; } = 1024;
    
    /// <summary>
    /// CPU limit percentage
    /// </summary>
    public int CpuLimitPercentage { get; init; } = 80;
}

/// <summary>
/// Cache configuration settings
/// </summary>
public record CacheConfiguration
{
    /// <summary>
    /// Whether caching is enabled
    /// </summary>
    public bool IsEnabled { get; init; } = true;
    
    /// <summary>
    /// Cache duration in minutes
    /// </summary>
    public int DurationMinutes { get; init; } = 60;
    
    /// <summary>
    /// Maximum cache size in MB
    /// </summary>
    public int MaxSizeMB { get; init; } = 100;
    
    /// <summary>
    /// Cache provider type
    /// </summary>
    public string Provider { get; init; } = "Memory";
}

/// <summary>
/// Fallback configuration settings
/// </summary>
public record FallbackConfiguration
{
    /// <summary>
    /// Whether fallback is enabled
    /// </summary>
    public bool IsEnabled { get; init; } = true;
    
    /// <summary>
    /// Fallback model name
    /// </summary>
    public string ModelName { get; init; } = string.Empty;
    
    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetries { get; init; } = 3;
    
    /// <summary>
    /// Retry delay in seconds
    /// </summary>
    public int RetryDelaySeconds { get; init; } = 5;
}

/// <summary>
/// Monitoring configuration settings
/// </summary>
public record MonitoringConfiguration
{
    /// <summary>
    /// Whether monitoring is enabled
    /// </summary>
    public bool IsEnabled { get; init; } = true;
    
    /// <summary>
    /// Log level
    /// </summary>
    public string LogLevel { get; init; } = "Information";
    
    /// <summary>
    /// Whether to log requests
    /// </summary>
    public bool LogRequests { get; init; } = false;
    
    /// <summary>
    /// Whether to log responses
    /// </summary>
    public bool LogResponses { get; init; } = false;
    
    /// <summary>
    /// Metrics collection interval in seconds
    /// </summary>
    public int MetricsIntervalSeconds { get; init; } = 60;
}
