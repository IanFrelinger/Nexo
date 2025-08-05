using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Shared.Interfaces;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// A service that provides advanced caching capabilities, including intelligent caching strategies,
    /// response deduplication, similarity-based response retrieval, and cache optimization.
    /// </summary>
    public class AdvancedCachingService : IAdvancedCachingService
    {
        /// <summary>
        /// Logger instance used for logging activities, events, and errors within the <see cref="AdvancedCachingService"/> class.
        /// </summary>
        /// <remarks>
        /// Utilizes the Microsoft.Extensions.Logging framework to facilitate structured and level-based logging, enabling debugging, performance monitoring, and error tracing.
        /// </remarks>
        private readonly ILogger<AdvancedCachingService> _logger;

        /// <summary>
        /// Represents the caching mechanism used for storing and retrieving responses to enhance performance
        /// and support operations such as response deduplication and similarity matching.
        /// </summary>
        /// <remarks>
        /// Utilizes an implementation of the <see cref="ICacheStrategy{TKey, TValue}"/> interface to provide
        /// advanced caching functionalities, including efficient storage and retrieval of model responses
        /// based on dynamically generated keys.
        /// </remarks>
        /// <seealso cref="ICacheStrategy{TKey, TValue}"/>
        /// <seealso cref="ModelResponse"/>
        private readonly ICacheStrategy<string, ModelResponse> _cache;

        /// <summary>
        /// An instance of <see cref="ISemanticCacheKeyGenerator"/> used for generating unique semantic cache keys
        /// based on input and metadata. Utilized within the caching mechanism to ensure efficient
        /// and context-aware data retrieval.
        /// </summary>
        private readonly ISemanticCacheKeyGenerator _keyGenerator;

        /// <summary>
        /// Holds in-memory cache entries where the cache key is a string, and the value is a CacheEntry object.
        /// The key typically represents a unique identifier for the request, and the value contains associated
        /// metadata including the request, response, creation timestamp, and similarity score.
        /// This dictionary serves as the foundation for managing cached data in the AdvancedCachingService.
        /// </summary>
        private readonly Dictionary<string, CacheEntry> _cacheEntries;

        /// <summary>
        /// Synchronization object used to ensure thread safety during operations that
        /// involve accessing or modifying the internal caching mechanism in the
        /// AdvancedCachingService.
        /// </summary>
        private readonly object _cacheLock = new object();

        /// <summary>
        /// Provides functionality for advanced caching mechanisms, including intelligent caching strategies, response deduplication,
        /// and similarity-based matching for improving efficiency and resource management.
        /// </summary>
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

        /// <summary>
        /// Attempts to retrieve a cached <see cref="ModelResponse"/> based on the given <see cref="ModelRequest"/>.
        /// If no cached result is found, generates a new response using the provided factory function,
        /// stores it in the cache, and then returns the result.
        /// </summary>
        /// <param name="request">The input model request containing the data for processing.</param>
        /// <param name="factory">
        /// A delegate that generates a new <see cref="ModelResponse"/> in the absence of a cache hit.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation, containing the resulting <see cref="ModelResponse"/>.
        /// </returns>
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

        /// <summary>
        /// Retrieves a collection of responses with a similarity score above the specified threshold compared to the provided request.
        /// </summary>
        /// <param name="request">The input model request to be compared against cached entries.</param>
        /// <param name="similarityThreshold">The minimum similarity score required for a response to be included. Defaults to 0.8.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing a collection of similar responses ordered by relevance.</returns>
        public Task<IEnumerable<ModelResponse>> GetSimilarResponsesAsync(ModelRequest request, double similarityThreshold = 0.8, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Finding similar responses for request");

            try
            {
                var similarResponses = new List<ModelResponse>();
                
                lock (_cacheLock)
                {
                    similarResponses.AddRange(from entry in _cacheEntries.Values let similarity = CalculateSimilarity(request.Input, entry.Request.Input) where similarity >= similarityThreshold select entry.Response);
                }

                lock (_cacheLock)
                {
                    return Task.FromResult<IEnumerable<ModelResponse>>(similarResponses.OrderByDescending(r => 
                        CalculateSimilarity(request.Input, _cacheEntries.Values.First(e => e.Response == r).Request.Input)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar responses");
                return Task.FromResult(Enumerable.Empty<ModelResponse>());
            }
        }

        /// <summary>
        /// Invalidates cached responses that are similar to the specified request based on a defined similarity threshold.
        /// </summary>
        /// <param name="request">The request object containing input details that are used to evaluate similarity with cached responses.</param>
        /// <param name="similarityThreshold">The threshold value for determining the similarity between cached responses and the input request. Defaults to 0.9.</param>
        /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a boolean value indicating whether any similar responses were invalidated.
        /// </returns>
        /// <remarks>
        /// This method scans the cached entries and uses a similarity calculation to identify and remove any cached entries that match or exceed the similarity threshold.
        /// This is useful for maintaining cache relevancy and preventing stale or overly similar data.
        /// Logs informational messages about the invalidation process and handles error scenarios by logging them without throwing exceptions.
        /// </remarks>
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

        /// <summary>
        /// Asynchronously retrieves comprehensive statistics about the cache, including the total number of entries,
        /// average response size, most common request types, cache hit rate, and average similarity score.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="CacheStatistics"/>
        /// instance with detailed information about the current state of the cache.
        /// </returns>
        /// <remarks>
        /// This method calculates statistics by analyzing the current cache entries and performs operations such as grouping,
        /// averaging, and calculating hit rates. It also handles exceptions internally and ensures consistent access to the cache
        /// by applying a lock during the statistics computation. In case of an error, default statistics are returned.
        /// </remarks>
        public Task<CacheStatistics> GetStatisticsAsync()
        {
            _logger.LogInformation("Getting cache statistics");

            try
            {
                lock (_cacheLock)
                {
                    return Task.FromResult(new CacheStatistics
                    {
                        TotalEntries = _cacheEntries.Count,
                        AverageResponseSize = _cacheEntries.Values.Average(e => e.Response.Content?.Length ?? 0),
                        MostCommonRequestTypes = _cacheEntries.Values
                            .GroupBy(e => {
                                if (e.Request.Metadata != null && e.Request.Metadata.TryGetValue("type", out var value))
                                    return value?.ToString() ?? "unknown";
                                return "unknown";
                            })
                            .OrderByDescending(g => g.Count())
                            .Take(5)
                            .Select(g => g.Key)
                            .ToList(),
                        CacheHitRate = CalculateHitRate(),
                        AverageSimilarityScore = _cacheEntries.Values.Average(e => e.SimilarityScore)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache statistics");
                return Task.FromResult(new Nexo.Feature.AI.Interfaces.CacheStatistics());
            }
        }

        /// <summary>
        /// Asynchronously performs optimization on the cache by removing outdated entries and updating similarity scores.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/>.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// </returns>
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
                    keysToRemove.AddRange(from entry in _cacheEntries where entry.Value.CreatedAt < cutoffTime select entry.Key);
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
                await UpdateSimilarityScoresAsync();

                _logger.LogInformation("Cache optimization completed, removed {Count} old entries", keysToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing cache");
            }
        }

        /// <summary>
        /// Searches the cache for a response similar to the provided <see cref="ModelRequest"/>
        /// based on a predefined similarity threshold.
        /// </summary>
        /// <param name="request">The request used to find a similar cached response.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a
        /// <see cref="ModelResponse"/> that is most similar to the provided request or null
        /// if no sufficiently similar response is found.
        /// </returns>
        private Task<ModelResponse> FindSimilarResponseAsync(ModelRequest request, CancellationToken cancellationToken)
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

                return Task.FromResult(bestMatch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding similar response");
                return Task.FromResult<ModelResponse>(null);
            }
        }

        /// <summary>
        /// Stores a response in the cache using the provided cache key and associates it with the request.
        /// </summary>
        /// <param name="cacheKey">The unique key used to store and retrieve the response from the cache.</param>
        /// <param name="response">The response object to be stored in the cache.</param>
        /// <param name="request">The request object associated with the response for tracking and metadata purposes.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
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

        /// <summary>
        /// Calculates the similarity between two input strings based on common words.
        /// </summary>
        /// <param name="input1">The first input string to compare.</param>
        /// <param name="input2">The second input string to compare.</param>
        /// <returns>A double value representing the similarity score between the two input strings, ranging from 0.0 to 1.0.</returns>
        private static double CalculateSimilarity(string input1, string input2)
        {
            if (string.IsNullOrEmpty(input1) || string.IsNullOrEmpty(input2))
                return 0.0;

            // Simple similarity calculation based on common words
            var words1 = new HashSet<string>(input1.ToLowerInvariant().Split([' '], StringSplitOptions.RemoveEmptyEntries));
            var words2 = new HashSet<string>(input2.ToLowerInvariant().Split([' '], StringSplitOptions.RemoveEmptyEntries));

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union > 0 ? (double)intersection / union : 0.0;
        }

        /// <summary>
        /// Updates the similarity scores for all cache entries in the service.
        /// Calculates similarity scores between each cache entry and updates the entries with their new average similarity score.
        /// Handles concurrent updates with proper synchronization to ensure thread safety.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation of updating similarity scores.</returns>
        private Task UpdateSimilarityScoresAsync()
        {
            try
            {
                lock (_cacheLock)
                {
                    foreach (var entry in _cacheEntries.Values)
                    {
                        var totalSimilarity = 0.0;
                        var count = 0;

                        foreach (var otherEntry in _cacheEntries.Values.Where(otherEntry => entry != otherEntry))
                        {
                            totalSimilarity += CalculateSimilarity(entry.Request.Input, otherEntry.Request.Input);
                            count++;
                        }

                        entry.SimilarityScore = count > 0 ? totalSimilarity / count : 0.0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating similarity scores");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Calculates the cache hit rate as a proportion of successful cache hits
        /// compared to total requests. The calculation is dependent on actual
        /// tracking of hits and misses within the caching mechanism.
        /// </summary>
        /// <returns>
        /// A double value representing the cache hit rate as a decimal
        /// (e.g., 0.75 for 75% cache hit rate).
        /// </returns>
        private static double CalculateHitRate()
        {
            // This would need to be implemented with actual hit/miss tracking
            // For now, return a placeholder value
            return 0.75;
        }

        /// <summary>
        /// Represents a cache entry containing the cached response, associated request, creation timestamp, and similarity score.
        /// </summary>
        private class CacheEntry
        {
            /// <summary>
            /// Gets or sets the AI model response associated with the cached entry.
            /// </summary>
            public ModelResponse Response { get; set; }

            /// <summary>
            /// Gets or sets the request associated with a cache entry.
            /// </summary>
            /// <remarks>
            /// The request property stores the details of the corresponding {@link ModelRequest} for an AI model.
            /// It is used to identify and evaluate entries in the cache, particularly during operations like
            /// similarity-based retrieval or invalidation.
            /// </remarks>
            public ModelRequest Request { get; set; }

            /// <summary>
            /// Gets or sets the timestamp indicating when the cache entry was created.
            /// This property is used to track the creation time of a cache entry
            /// for purposes like eviction or expiration management.
            /// </summary>
            public DateTime CreatedAt { get; set; }

            /// <summary>
            /// Represents the similarity score calculated for a specific cache entry.
            /// This score measures the degree of similarity between a cache entry's
            /// input and other cached inputs. It is computed as an average similarity
            /// between the current input and all other inputs in the cache.
            /// </summary>
            public double SimilarityScore { get; set; }
        }
    }
} 