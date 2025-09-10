using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Caching;

namespace Nexo.Infrastructure.Services.Caching.Advanced
{
    /// <summary>
    /// Intelligent cache eviction policy that uses multiple strategies for optimal cache management.
    /// Implements Phase 3.3 advanced caching requirements.
    /// </summary>
    public class IntelligentEvictionPolicy : ICacheEvictionPolicy
    {
        private readonly IList<ICacheEvictionStrategy> _strategies;
        private readonly CacheEvictionConfiguration _configuration;

        public IntelligentEvictionPolicy(IEnumerable<ICacheEvictionStrategy> strategies, CacheEvictionConfiguration configuration)
        {
            _strategies = strategies?.ToList() ?? throw new ArgumentNullException(nameof(strategies));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Selects items for eviction using intelligent multi-strategy approach.
        /// </summary>
        public IEnumerable<CacheItem> SelectForEviction(IEnumerable<CacheItem> items, int count)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (count <= 0) return Enumerable.Empty<CacheItem>();

            var itemList = items.ToList();
            if (itemList.Count <= count) return itemList;

            // Apply intelligent selection based on configuration
            var candidates = itemList.AsEnumerable();

            // Apply each strategy in order of priority
            foreach (var strategy in _strategies.OrderBy(s => s.Priority))
            {
                candidates = strategy.FilterCandidates(candidates, count);
                if (candidates.Count() <= count) break;
            }

            // Select final candidates based on scoring
            var scoredCandidates = candidates
                .Select(item => new { Item = item, Score = CalculateEvictionScore(item) })
                .OrderBy(x => x.Score)
                .Take(count)
                .Select(x => x.Item);

            return scoredCandidates;
        }

        /// <summary>
        /// Calculates eviction score for a cache item (lower score = more likely to evict).
        /// </summary>
        private double CalculateEvictionScore(CacheItem item)
        {
            var score = 0.0;

            // Age factor (older items score higher for eviction)
            var age = DateTimeOffset.UtcNow - item.CreatedAt;
            score += age.TotalMinutes * _configuration.AgeWeight;

            // Access frequency factor (less accessed items score higher)
            var accessRatio = item.AccessCount / Math.Max(age.TotalMinutes, 1);
            score += (1.0 / (accessRatio + 1)) * _configuration.AccessWeight;

            // Priority factor (lower priority items score higher)
            score += (int)item.Priority * _configuration.PriorityWeight;

            // Size factor (larger items score higher for eviction)
            score += item.SizeBytes * _configuration.SizeWeight;

            // Last access time factor (longer since last access scores higher)
            var timeSinceLastAccess = DateTimeOffset.UtcNow - item.LastAccessedAt;
            score += timeSinceLastAccess.TotalMinutes * _configuration.LastAccessWeight;

            return score;
        }
    }

    /// <summary>
    /// Interface for cache eviction strategies.
    /// </summary>
    public interface ICacheEvictionStrategy
    {
        int Priority { get; }
        string Name { get; }
        IEnumerable<CacheItem> FilterCandidates(IEnumerable<CacheItem> candidates, int targetCount);
    }

    /// <summary>
    /// LRU (Least Recently Used) eviction strategy.
    /// </summary>
    public class LruEvictionStrategy : ICacheEvictionStrategy
    {
        public int Priority => 1;
        public string Name => "LRU";

        public IEnumerable<CacheItem> FilterCandidates(IEnumerable<CacheItem> candidates, int targetCount)
        {
            return candidates
                .OrderBy(item => item.LastAccessedAt)
                .Take(targetCount * 2); // Take more than needed for further filtering
        }
    }

    /// <summary>
    /// LFU (Least Frequently Used) eviction strategy.
    /// </summary>
    public class LfuEvictionStrategy : ICacheEvictionStrategy
    {
        public int Priority => 2;
        public string Name => "LFU";

        public IEnumerable<CacheItem> FilterCandidates(IEnumerable<CacheItem> candidates, int targetCount)
        {
            return candidates
                .OrderBy(item => item.AccessCount)
                .ThenBy(item => item.LastAccessedAt)
                .Take(targetCount * 2);
        }
    }

    /// <summary>
    /// Size-based eviction strategy (evict largest items first).
    /// </summary>
    public class SizeBasedEvictionStrategy : ICacheEvictionStrategy
    {
        public int Priority => 3;
        public string Name => "Size-Based";

        public IEnumerable<CacheItem> FilterCandidates(IEnumerable<CacheItem> candidates, int targetCount)
        {
            return candidates
                .OrderByDescending(item => item.SizeBytes)
                .Take(targetCount * 2);
        }
    }

    /// <summary>
    /// Priority-based eviction strategy.
    /// </summary>
    public class PriorityBasedEvictionStrategy : ICacheEvictionStrategy
    {
        public int Priority => 4;
        public string Name => "Priority-Based";

        public IEnumerable<CacheItem> FilterCandidates(IEnumerable<CacheItem> candidates, int targetCount)
        {
            return candidates
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.LastAccessedAt)
                .Take(targetCount * 2);
        }
    }

    /// <summary>
    /// Configuration for cache eviction policies.
    /// </summary>
    public class CacheEvictionConfiguration
    {
        public double AgeWeight { get; set; } = 1.0;
        public double AccessWeight { get; set; } = 2.0;
        public double PriorityWeight { get; set; } = 3.0;
        public double SizeWeight { get; set; } = 0.5;
        public double LastAccessWeight { get; set; } = 1.5;
        public int MaxCandidates { get; set; } = 1000;
        public TimeSpan MaxAge { get; set; } = TimeSpan.FromHours(24);
    }
}