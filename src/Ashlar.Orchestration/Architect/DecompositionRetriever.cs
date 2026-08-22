using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Orchestration.Architect.Models;

namespace Ashlar.Orchestration.Architect;

/// <summary>
/// Retrieves similar decomposition examples from cache for RAG (Retrieval-Augmented Generation).
/// 
/// Responsibilities:
/// - Retrieves similar decomposition examples from cache
/// - Scores examples by similarity (domain matching, keyword matching)
/// - Returns top-k most similar examples
/// - Maintains decomposition index in cache
/// 
/// Used by ArchitectAgent to provide context from past decompositions.
/// Implements RAG pattern for improved decomposition quality.
/// </summary>
public sealed class DecompositionRetriever
{
    private readonly ICacheStrategy _cache;
    private readonly DomainRecognizer _domainRecognizer;
    private readonly ILogger<DecompositionRetriever> _logger;
    private const string CacheKeyPrefix = "decomposition:example:";
    private const string IndexKey = "decomposition:index";

    /// <summary>
    /// Initializes a new instance of the <see cref="DecompositionRetriever"/> class.
    /// </summary>
    /// <param name="cache">The cache strategy for storing and retrieving examples.</param>
    /// <param name="domainRecognizer">The domain recognizer for extracting domains and keywords.</param>
    /// <param name="logger">The logger instance.</param>
    public DecompositionRetriever(
        ICacheStrategy cache,
        DomainRecognizer domainRecognizer,
        ILogger<DecompositionRetriever> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _domainRecognizer = domainRecognizer ?? throw new ArgumentNullException(nameof(domainRecognizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves similar decomposition examples for a given request.
    /// 
    /// Uses RAG (Retrieval-Augmented Generation) to find past decompositions that are similar
    /// to the current request. Similarity is calculated based on:
    /// - Domain matching (40% weight)
    /// - Keyword overlap (40% weight)
    /// - Quality score (20% weight)
    /// 
    /// Returns the top N most similar examples.
    /// </summary>
    /// <param name="request">The request to find similar examples for.</param>
    /// <param name="maxResults">Maximum number of examples to return (default: 5).</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A read-only list of similar decomposition examples, ordered by similarity score.</returns>
    public async Task<IReadOnlyList<DecompositionExample>> RetrieveSimilarAsync(
        string request,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);
        if (index.Count == 0)
        {
            _logger.LogDebug("No decomposition examples in index");
            return Array.Empty<DecompositionExample>();
        }

        // Recognize domains from request
        var requestDomains = _domainRecognizer.RecognizeDomains(request);
        var requestKeywords = _domainRecognizer.ExtractKeywords(request);

        // Score examples by similarity
        var scoredExamples = new List<(DecompositionExample Example, double Score)>();

        foreach (var exampleId in index)
        {
            var example = await GetExampleAsync(exampleId, cancellationToken);
            if (example == null)
            {
                continue;
            }

            var score = CalculateSimilarityScore(example, requestDomains, requestKeywords);
            scoredExamples.Add((example, score));
        }

        // Return top N examples
        return scoredExamples
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => x.Example)
            .ToList();
    }

    /// <summary>
    /// Stores a decomposition example in the cache.
    /// 
    /// Stores the example with a 30-day expiration and updates the decomposition index.
    /// Examples are stored for future RAG retrieval.
    /// </summary>
    /// <param name="example">The decomposition example to store.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous store operation.</returns>
    public async Task StoreExampleAsync(
        DecompositionExample example,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{example.Id}";
        await _cache.SetAsync(cacheKey, example, TimeSpan.FromDays(30), cancellationToken);

        // Update index
        var index = await GetIndexAsync(cancellationToken);
        if (!index.Contains(example.Id))
        {
            index.Add(example.Id);
            await _cache.SetAsync(IndexKey, index, TimeSpan.FromDays(30), cancellationToken);
        }

        _logger.LogDebug("Stored decomposition example: {ExampleId}", example.Id);
    }

    /// <summary>
    /// Gets all decomposition examples from the cache.
    /// 
    /// Retrieves all examples stored in the decomposition index.
    /// Useful for debugging or bulk operations.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A read-only list of all decomposition examples.</returns>
    public async Task<IReadOnlyList<DecompositionExample>> GetAllExamplesAsync(
        CancellationToken cancellationToken = default)
    {
        var index = await GetIndexAsync(cancellationToken);
        var examples = new List<DecompositionExample>();

        foreach (var exampleId in index)
        {
            var example = await GetExampleAsync(exampleId, cancellationToken);
            if (example != null)
            {
                examples.Add(example);
            }
        }

        return examples;
    }

    /// <summary>
    /// Gets a single decomposition example by ID from the cache.
    /// </summary>
    /// <param name="exampleId">The ID of the example to retrieve.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The decomposition example, or null if not found.</returns>
    private async Task<DecompositionExample?> GetExampleAsync(
        string exampleId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{exampleId}";
        return await _cache.GetAsync<DecompositionExample>(cacheKey, cancellationToken);
    }

    /// <summary>
    /// Gets the decomposition index from the cache.
    /// 
    /// The index is a list of example IDs that are currently stored.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A list of example IDs (empty list if index doesn't exist).</returns>
    private async Task<List<string>> GetIndexAsync(CancellationToken cancellationToken)
    {
        var index = await _cache.GetAsync<List<string>>(IndexKey, cancellationToken);
        return index ?? new List<string>();
    }

    /// <summary>
    /// Calculates a similarity score between an example and a request.
    /// 
    /// Score components:
    /// - Domain matches: 0.4 per matching domain
    /// - Keyword overlap ratio: 0.4 * (matching keywords / total keywords)
    /// - Quality score: 0.2 * example quality score
    /// 
    /// Higher scores indicate better similarity.
    /// </summary>
    /// <param name="example">The decomposition example to score.</param>
    /// <param name="requestDomains">The domains recognized from the request.</param>
    /// <param name="requestKeywords">The keywords extracted from the request.</param>
    /// <returns>A similarity score between 0.0 and approximately 1.0+.</returns>
    private static double CalculateSimilarityScore(
        DecompositionExample example,
        IReadOnlyList<string> requestDomains,
        IReadOnlyList<string> requestKeywords)
    {
        double score = 0.0;

        // Domain match bonus
        var domainMatches = example.DomainTags
            .Intersect(requestDomains, StringComparer.OrdinalIgnoreCase)
            .Count();
        score += domainMatches * 0.4;

        // Keyword overlap
        var keywordMatches = example.Keywords
            .Intersect(requestKeywords, StringComparer.OrdinalIgnoreCase)
            .Count();
        var keywordRatio = requestKeywords.Count > 0
            ? (double)keywordMatches / requestKeywords.Count
            : 0.0;
        score += keywordRatio * 0.4;

        // Quality score bonus
        score += example.QualityScore * 0.2;

        return score;
    }
}

