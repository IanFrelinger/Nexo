using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Interfaces.Caching;

namespace Nexo.Infrastructure.Services.Caching.Advanced
{
    /// <summary>
    /// Service for intelligent response deduplication and similarity matching.
    /// Part of Phase 3.3 advanced caching features.
    /// </summary>
    public class ResponseDeduplicationService : IResponseDeduplicationService
    {
        private readonly ICacheStrategy<string, CachedResponse> _cache;
        private readonly ISimilarityCalculator _similarityCalculator;
        private readonly DeduplicationConfiguration _configuration;

        public ResponseDeduplicationService(
            ICacheStrategy<string, CachedResponse> cache,
            ISimilarityCalculator similarityCalculator,
            DeduplicationConfiguration configuration)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _similarityCalculator = similarityCalculator ?? throw new ArgumentNullException(nameof(similarityCalculator));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Attempts to find a similar cached response for the given request.
        /// </summary>
        public async Task<CachedResponse?> FindSimilarResponseAsync(
            string requestHash, 
            string requestContent, 
            CancellationToken cancellationToken = default)
        {
            // First, try exact match
            var exactMatch = await _cache.GetAsync(requestHash, cancellationToken);
            if (exactMatch != null)
            {
                return exactMatch;
            }

            // If no exact match, try similarity matching
            if (_configuration.EnableSimilarityMatching)
            {
                return await FindSimilarResponseByContentAsync(requestContent, cancellationToken);
            }

            return null;
        }

        /// <summary>
        /// Caches a response with intelligent deduplication.
        /// </summary>
        public async Task CacheResponseAsync(
            string requestHash, 
            string requestContent, 
            string responseContent, 
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default)
        {
            var cachedResponse = new CachedResponse
            {
                RequestHash = requestHash,
                RequestContent = requestContent,
                ResponseContent = responseContent,
                CreatedAt = DateTimeOffset.UtcNow,
                LastAccessedAt = DateTimeOffset.UtcNow,
                AccessCount = 1,
                Size = Encoding.UTF8.GetByteCount(responseContent)
            };

            await _cache.SetAsync(requestHash, cachedResponse, ttl, cancellationToken);

            // Store content hash for similarity matching
            if (_configuration.EnableSimilarityMatching)
            {
                var contentHash = ComputeContentHash(requestContent);
                await _cache.SetAsync($"content:{contentHash}", cachedResponse, ttl, cancellationToken);
            }
        }

        /// <summary>
        /// Finds similar responses based on content similarity.
        /// </summary>
        private async Task<CachedResponse?> FindSimilarResponseByContentAsync(
            string requestContent, 
            CancellationToken cancellationToken)
        {
            // This is a simplified implementation - in practice, you'd use more sophisticated
            // similarity algorithms and indexing strategies
            var contentHash = ComputeContentHash(requestContent);
            return await _cache.GetAsync($"content:{contentHash}", cancellationToken);
        }

        /// <summary>
        /// Computes a hash for content similarity matching.
        /// </summary>
        private string ComputeContentHash(string content)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Computes similarity score between two pieces of content.
        /// </summary>
        public async Task<double> ComputeSimilarityAsync(string content1, string content2, CancellationToken cancellationToken = default)
        {
            return await _similarityCalculator.ComputeSimilarityAsync(content1, content2, cancellationToken);
        }

        /// <summary>
        /// Gets deduplication statistics.
        /// </summary>
        public async Task<DeduplicationStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            // This would typically query cache statistics
            return new DeduplicationStatistics
            {
                TotalCachedResponses = 0, // Would be populated from cache stats
                DuplicateResponses = 0,
                SimilarityMatches = 0,
                CacheHitRate = 0.0,
                LastUpdated = DateTimeOffset.UtcNow
            };
        }
    }

    /// <summary>
    /// Interface for response deduplication service.
    /// </summary>
    public interface IResponseDeduplicationService
    {
        Task<CachedResponse?> FindSimilarResponseAsync(string requestHash, string requestContent, CancellationToken cancellationToken = default);
        Task CacheResponseAsync(string requestHash, string requestContent, string responseContent, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
        Task<double> ComputeSimilarityAsync(string content1, string content2, CancellationToken cancellationToken = default);
        Task<DeduplicationStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for similarity calculation algorithms.
    /// </summary>
    public interface ISimilarityCalculator
    {
        Task<double> ComputeSimilarityAsync(string content1, string content2, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Jaccard similarity calculator for text content.
    /// </summary>
    public class JaccardSimilarityCalculator : ISimilarityCalculator
    {
        public async Task<double> ComputeSimilarityAsync(string content1, string content2, CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            var tokens1 = Tokenize(content1);
            var tokens2 = Tokenize(content2);

            var intersection = tokens1.Intersect(tokens2).Count();
            var union = tokens1.Union(tokens2).Count();

            return union == 0 ? 0.0 : (double)intersection / union;
        }

        private static HashSet<string> Tokenize(string content)
        {
            return content
                .ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();
        }
    }

    /// <summary>
    /// Cosine similarity calculator for text content.
    /// </summary>
    public class CosineSimilarityCalculator : ISimilarityCalculator
    {
        public async Task<double> ComputeSimilarityAsync(string content1, string content2, CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Simulate async operation

            var vector1 = CreateTermFrequencyVector(content1);
            var vector2 = CreateTermFrequencyVector(content2);

            var dotProduct = vector1.Zip(vector2, (a, b) => a * b).Sum();
            var magnitude1 = Math.Sqrt(vector1.Sum(x => x * x));
            var magnitude2 = Math.Sqrt(vector2.Sum(x => x * x));

            return magnitude1 == 0 || magnitude2 == 0 ? 0.0 : dotProduct / (magnitude1 * magnitude2);
        }

        private static double[] CreateTermFrequencyVector(string content)
        {
            var tokens = content
                .ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            var termCounts = tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
            var allTerms = termCounts.Keys.ToList();

            return allTerms.Select(term => (double)termCounts.GetValueOrDefault(term, 0)).ToArray();
        }
    }

    /// <summary>
    /// Cached response model.
    /// </summary>
    public class CachedResponse
    {
        public string RequestHash { get; set; } = string.Empty;
        public string RequestContent { get; set; } = string.Empty;
        public string ResponseContent { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastAccessedAt { get; set; }
        public int AccessCount { get; set; }
        public long Size { get; set; }
        public double SimilarityScore { get; set; }
    }

    /// <summary>
    /// Deduplication statistics.
    /// </summary>
    public class DeduplicationStatistics
    {
        public int TotalCachedResponses { get; set; }
        public int DuplicateResponses { get; set; }
        public int SimilarityMatches { get; set; }
        public double CacheHitRate { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
    }

    /// <summary>
    /// Configuration for response deduplication.
    /// </summary>
    public class DeduplicationConfiguration
    {
        public bool EnableSimilarityMatching { get; set; } = true;
        public double SimilarityThreshold { get; set; } = 0.8;
        public int MaxCacheSize { get; set; } = 10000;
        public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(1);
        public SimilarityAlgorithm Algorithm { get; set; } = SimilarityAlgorithm.Jaccard;
    }

    /// <summary>
    /// Similarity algorithms.
    /// </summary>
    public enum SimilarityAlgorithm
    {
        Jaccard,
        Cosine,
        Levenshtein
    }
}
