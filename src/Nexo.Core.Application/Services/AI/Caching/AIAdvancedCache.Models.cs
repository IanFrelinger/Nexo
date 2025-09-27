using System;
using System.Collections.Generic;

namespace Nexo.Core.Application.Services.AI.Caching
{
    /// <summary>
    /// Data models and enums for AIAdvancedCache.
    /// </summary>
    public partial class AIAdvancedCache
    {
        // This partial class contains data models and enums
        // The actual models are defined below
    }

    /// <summary>
    /// Cache entry
    /// </summary>
    public class CacheEntry
    {
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public DateTime? LastRefreshedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int AccessCount { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Cache policy
    /// </summary>
    public class CachePolicy
    {
        public string Name { get; set; } = string.Empty;
        public TimeSpan? ExpirationTime { get; set; }
        public int MaxSize { get; set; } = 1000;
        public EvictionStrategy EvictionStrategy { get; set; } = EvictionStrategy.LRU;
        public bool EnableRefresh { get; set; } = true;
        public TimeSpan? RefreshThreshold { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Cache result
    /// </summary>
    public class CacheResult<T>
    {
        public bool Found { get; set; }
        public T? Value { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Cache statistics
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int Hits { get; set; }
        public int Misses { get; set; }
        public int Sets { get; set; }
        public int Removals { get; set; }
        public int Evictions { get; set; }
        public int ExpiredHits { get; set; }
        public int Clears { get; set; }
        public double HitRate { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Cache health
    /// </summary>
    public class CacheHealth
    {
        public bool IsHealthy { get; set; }
        public double HitRate { get; set; }
        public int TotalEntries { get; set; }
        public double MemoryUsage { get; set; }
        public double EvictionRate { get; set; }
        public List<string> Issues { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Preload item
    /// </summary>
    public class PreloadItem
    {
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = new();
        public string? PolicyName { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Eviction strategies
    /// </summary>
    public enum EvictionStrategy
    {
        LRU,    // Least Recently Used
        LFU,    // Least Frequently Used
        FIFO,   // First In, First Out
        TTL     // Time To Live
    }
}
