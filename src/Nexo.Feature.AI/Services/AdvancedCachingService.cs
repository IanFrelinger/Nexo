using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Shared.Interfaces;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Advanced caching service that provides intelligent caching strategies, response deduplication, and similarity matching.
    /// </summary>
    public class AdvancedCachingService : IAdvancedCachingService
    {
        private readonly ILogger<AdvancedCachingService> _logger;
        private readonly ICacheStrategy<string, ModelResponse> _cache;
        private readonly ISemanticCacheKeyGenerator _keyGenerator;
        private readonly Dictionary<string, CacheEntry> _cacheEntries;
        private readonly object _cacheLock = new object();

        public AdvancedCachingService(
            ILogger<AdvancedCachingService> logger,
            ICacheStrategy<string, ModelResponse> cache,
            ISemanticCacheKeyGenerator keyGenerator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
            _cacheEntries = new Dictionary<string, CacheEntry>();
        }

        public async Task<ModelResponse> GetOrSetAsync(ModelRequest request, Func<Task<ModelResponse>> factory, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Advanced caching: Processing request with length {Length}", request.Input?.Length ?? 0);

            try
            {
                // Generate semantic cache key
                var cacheKey = _keyGenerator.Generate(request.Input, request.Metadata);
                
                // Try to get from cache first
                var cachedResponse = await _cache.GetAsync(cacheKey, cancellationToken);
                if (cachedResponse != null)
                {
                    _logger.LogInformation("Cache hit for key: {Key}", cacheKey);
                    return cachedResponse;
                }

                // Check for similar cached responses
                var similarResponse = await FindSimilarResponseAsync(request, cancellationToken);
                if (similarResponse != null)
                {
                    _logger.LogInformation("Found similar cached response, using it");
                    return similarResponse;
                }

                // Generate new response
                _logger.LogInformation("Cache miss, generating new response");
                var newResponse = await factory();
                
                // Store in cache with metadata
                await StoreResponseAsync(cacheKey, newResponse, request, cancellationToken);
                
                return newResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in advanced caching");
                throw;
            }
        }

        public async Task<IEnumerable<ModelResponse>> GetSimilarResponsesAsync(ModelRequest request, double similarityThreshold = 0.8, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Finding similar responses for request");

            try
            {
                var similarResponses = new List<ModelResponse>();
                
                lock (_cacheLock)
                {
                    foreach (var entry in _cacheEntries.Values)
                    {
                        var similarity = CalculateSimilarity(request.Input, entry.Request.Input);
                        if (similarity >= similarityThreshold)
                        {
                            similarResponses.Add(entry.Response);
                        }
                    }
                }

                return similarResponses.OrderByDescending(r => 
                    CalculateSimilarity(request.Input, _cacheEntries.Values.First(e => e.Response == r).Request.Input));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar responses");
                return Enumerable.Empty<ModelResponse>();
            }
        }

        public async Task<bool> InvalidateSimilarAsync(ModelRequest request, double similarityThreshold = 0.9, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Invalidating similar responses");

            try
            {
                var keysToRemove = new List<string>();
                
                lock (_cacheLock)
                {
                    foreach (var entry in _cacheEntries)
                    {
                        var similarity = CalculateSimilarity(request.Input, entry.Value.Request.Input);
                        if (similarity >= similarityThreshold)
                        {
                            keysToRemove.Add(entry.Key);
                        }
                    }
                }

                foreach (var key in keysToRemove)
                {
                    await _cache.RemoveAsync(key, cancellationToken);
                    lock (_cacheLock)
                    {
                        _cacheEntries.Remove(key);
                    }
                }

                _logger.LogInformation("Invalidated {Count} similar responses", keysToRemove.Count);
                return keysToRemove.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating similar responses");
                return false;
            }
        }

        public async Task<Nexo.Feature.AI.Interfaces.CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting cache statistics");

            try
            {
                lock (_cacheLock)
                {
                    return new Nexo.Feature.AI.Interfaces.CacheStatistics
                    {
                        TotalEntries = _cacheEntries.Count,
                        AverageResponseSize = _cacheEntries.Values.Average(e => e.Response.Content?.Length ?? 0),
                        MostCommonRequestTypes = _cacheEntries.Values
                            .GroupBy(e => {
                                if (e.Request.Metadata != null && e.Request.Metadata.ContainsKey("type"))
                                    return e.Request.Metadata["type"]?.ToString() ?? "unknown";
                                return "unknown";
                            })
                            .OrderByDescending(g => g.Count())
                            .Take(5)
                            .Select(g => g.Key)
                            .ToList(),
                        CacheHitRate = CalculateHitRate(),
                        AverageSimilarityScore = _cacheEntries.Values.Average(e => e.SimilarityScore)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache statistics");
                return new Nexo.Feature.AI.Interfaces.CacheStatistics();
            }
        }

        public async Task OptimizeCacheAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Optimizing cache");

            try
            {
                // Remove old entries
                var cutoffTime = DateTime.UtcNow.AddHours(-24);
                var keysToRemove = new List<string>();
                
                lock (_cacheLock)
                {
                    foreach (var entry in _cacheEntries)
                    {
                        if (entry.Value.CreatedAt < cutoffTime)
                        {
                            keysToRemove.Add(entry.Key);
                        }
                    }
                }

                foreach (var key in keysToRemove)
                {
                    await _cache.RemoveAsync(key, cancellationToken);
                    lock (_cacheLock)
                    {
                        _cacheEntries.Remove(key);
                    }
                }

                // Update similarity scores
                await UpdateSimilarityScoresAsync(cancellationToken);

                _logger.LogInformation("Cache optimization completed, removed {Count} old entries", keysToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing cache");
            }
        }

        private async Task<ModelResponse> FindSimilarResponseAsync(ModelRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var bestMatch = (ModelResponse)null;
                var bestSimilarity = 0.0;

                lock (_cacheLock)
                {
                    foreach (var entry in _cacheEntries.Values)
                    {
                        var similarity = CalculateSimilarity(request.Input, entry.Request.Input);
                        if (similarity > bestSimilarity && similarity >= 0.8) // High similarity threshold
                        {
                            bestSimilarity = similarity;
                            bestMatch = entry.Response;
                        }
                    }
                }

                if (bestMatch != null)
                {
                    _logger.LogInformation("Found similar response with similarity: {Similarity}", bestSimilarity);
                }

                return bestMatch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar response");
                return null;
            }
        }

        private async Task StoreResponseAsync(string cacheKey, ModelResponse response, ModelRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _cache.SetAsync(cacheKey, response, null, cancellationToken);
                
                lock (_cacheLock)
                {
                    _cacheEntries[cacheKey] = new CacheEntry
                    {
                        Response = response,
                        Request = request,
                        CreatedAt = DateTime.UtcNow,
                        SimilarityScore = 0.0
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing response in cache");
            }
        }

        private double CalculateSimilarity(string input1, string input2)
        {
            if (string.IsNullOrEmpty(input1) || string.IsNullOrEmpty(input2))
                return 0.0;

            // Simple similarity calculation based on common words
            var words1 = new HashSet<string>(input1.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            var words2 = new HashSet<string>(input2.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union > 0 ? (double)intersection / union : 0.0;
        }

        private async Task UpdateSimilarityScoresAsync(CancellationToken cancellationToken)
        {
            try
            {
                lock (_cacheLock)
                {
                    foreach (var entry in _cacheEntries.Values)
                    {
                        var totalSimilarity = 0.0;
                        var count = 0;

                        foreach (var otherEntry in _cacheEntries.Values)
                        {
                            if (entry != otherEntry)
                            {
                                totalSimilarity += CalculateSimilarity(entry.Request.Input, otherEntry.Request.Input);
                                count++;
                            }
                        }

                        entry.SimilarityScore = count > 0 ? totalSimilarity / count : 0.0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating similarity scores");
            }
        }

        private double CalculateHitRate()
        {
            // This would need to be implemented with actual hit/miss tracking
            // For now, return a placeholder value
            return 0.75;
        }

        private class CacheEntry
        {
            public ModelResponse Response { get; set; }
            public ModelRequest Request { get; set; }
            public DateTime CreatedAt { get; set; }
            public double SimilarityScore { get; set; }
        }
    }

    /// <summary>
    /// Statistics about the cache performance and usage.
    /// </summary>

} 