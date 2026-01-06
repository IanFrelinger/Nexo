using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Application.Common.Models;
using Nexo.Core.Application.Common.Ports;
using System.Security.Cryptography;
using System.Text;

namespace Nexo.Infrastructure.Analysis.Adapters;

/// <summary>
/// Decorator for IAnalysisService that adds caching (Decorator pattern - OCP).
/// 
/// Provides caching layer for analysis results:
/// - Generates cache keys from path + file modification times
/// - Caches results for 30 minutes
/// - Returns cached results when available
/// - Falls through to inner service on cache miss
/// 
/// Implements the Decorator pattern to add caching without modifying the base service.
/// </summary>
public class CachedAnalysisServiceAdapter : IAnalysisService
{
    private readonly IAnalysisService _inner;
    private readonly ICacheStrategy _cache;
    private readonly ILogger<CachedAnalysisServiceAdapter> _logger;

    public CachedAnalysisServiceAdapter(
        IAnalysisService inner,
        ICacheStrategy cache,
        ILogger<CachedAnalysisServiceAdapter> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AnalysisResult> AnalyzeAsync(
        DirectoryInfo path,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Generate cache key from path + file hash
        var cacheKey = await GenerateCacheKeyAsync(path, cancellationToken);

        // Try to get from cache
        var cachedResult = await _cache.GetAsync<AnalysisResult>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogInformation("Returning cached analysis result for path: {Path}", path.FullName);
            progress?.Report(new ProgressReport
            {
                Percentage = 100,
                Message = "Returning cached analysis result",
                Metadata = new Dictionary<string, object> { ["Cached"] = true }
            });
            return cachedResult;
        }

        // Cache miss, call inner service
        _logger.LogInformation("Cache miss, analyzing path: {Path}", path.FullName);
        var result = await _inner.AnalyzeAsync(path, progress, cancellationToken);

        // Store in cache (30 minute expiration)
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30), cancellationToken);

        return result;
    }

    private async Task<string> GenerateCacheKeyAsync(DirectoryInfo path, CancellationToken cancellationToken)
    {
        // Create a hash from path + file modification times
        var files = path.GetFiles("*", SearchOption.AllDirectories)
            .OrderBy(f => f.FullName)
            .Take(100) // Limit to avoid performance issues
            .Select(f => $"{f.FullName}:{f.LastWriteTimeUtc.Ticks}");

        var content = string.Join("|", files);
        var hash = await ComputeHashAsync(content, cancellationToken);
        return $"analysis:{hash}";
    }

    private static Task<string> ComputeHashAsync(string content, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }, cancellationToken);
    }
}

