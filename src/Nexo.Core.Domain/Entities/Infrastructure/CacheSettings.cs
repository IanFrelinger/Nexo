namespace Nexo.Core.Domain.Entities.Infrastructure;

/// <summary>
/// Configuration settings for caching behavior
/// </summary>
public record CacheSettings
{
    /// <summary>
    /// Maximum cache size in MB
    /// </summary>
    public int MaxSizeMB { get; init; } = 100;
    
    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; init; } = 30;
    
    /// <summary>
    /// Whether caching is enabled
    /// </summary>
    public bool Enabled { get; init; } = true;
    
    /// <summary>
    /// Cache eviction policy
    /// </summary>
    public CacheEvictionPolicy EvictionPolicy { get; init; } = CacheEvictionPolicy.LeastRecentlyUsed;
    
    /// <summary>
    /// Whether to use distributed caching
    /// </summary>
    public bool UseDistributedCache { get; init; } = false;
    
    /// <summary>
    /// Cache key prefix
    /// </summary>
    public string KeyPrefix { get; init; } = "nexo";
    
    /// <summary>
    /// Cache backend type
    /// </summary>
    public string Backend { get; init; } = "Memory";
    
    /// <summary>
    /// Default TTL in seconds
    /// </summary>
    public int DefaultTtlSeconds { get; init; } = 300;
}

/// <summary>
/// Cache eviction policies
/// </summary>
public enum CacheEvictionPolicy
{
    /// <summary>
    /// Remove least recently used items first
    /// </summary>
    LeastRecentlyUsed,
    
    /// <summary>
    /// Remove least frequently used items first
    /// </summary>
    LeastFrequentlyUsed,
    
    /// <summary>
    /// Remove items in first-in-first-out order
    /// </summary>
    FirstInFirstOut,
    
    /// <summary>
    /// Remove items randomly
    /// </summary>
    Random
}
