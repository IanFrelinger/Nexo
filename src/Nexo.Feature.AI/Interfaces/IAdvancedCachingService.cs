using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Interfaces
{
    /// <summary>
    /// Interface for advanced caching service that provides intelligent caching strategies, response deduplication, and similarity matching.
    /// </summary>
    public interface IAdvancedCachingService
    {
        /// <summary>
        /// Gets a cached response or generates a new one using the factory function.
        /// </summary>
        /// <param name="request">The model request.</param>
        /// <param name="factory">Factory function to generate new response if not cached.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or newly generated response.</returns>
        Task<ModelResponse> GetOrSetAsync(ModelRequest request, Func<Task<ModelResponse>> factory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds responses similar to the given request.
        /// </summary>
        /// <param name="request">The model request to find similar responses for.</param>
        /// <param name="similarityThreshold">Minimum similarity threshold (0.0 to 1.0).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of similar responses.</returns>
        Task<IEnumerable<ModelResponse>> GetSimilarResponsesAsync(ModelRequest request, double similarityThreshold = 0.8, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates cache entries similar to the given request.
        /// </summary>
        /// <param name="request">The model request to invalidate similar entries for.</param>
        /// <param name="similarityThreshold">Minimum similarity threshold for invalidation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if any entries were invalidated, false otherwise.</returns>
        Task<bool> InvalidateSimilarAsync(ModelRequest request, double similarityThreshold = 0.9, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics about the cache performance and usage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Cache statistics.</returns>
        Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes the cache by removing old entries and updating similarity scores.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task OptimizeCacheAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Statistics about the cache performance and usage.
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Gets or sets the total number of cache entries.
        /// </summary>
        public int TotalEntries { get; set; }

        /// <summary>
        /// Gets or sets the average response size in characters.
        /// </summary>
        public double AverageResponseSize { get; set; }

        /// <summary>
        /// Gets or sets the most common request types.
        /// </summary>
        public List<string> MostCommonRequestTypes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the cache hit rate (0.0 to 1.0).
        /// </summary>
        public double CacheHitRate { get; set; }

        /// <summary>
        /// Gets or sets the average similarity score across all entries.
        /// </summary>
        public double AverageSimilarityScore { get; set; }
    }
} 