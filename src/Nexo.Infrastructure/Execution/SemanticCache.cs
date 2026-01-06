using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// In-memory semantic cache for brick execution results.
/// </summary>
public class SemanticCache : ISemanticCache
{
    private readonly Dictionary<string, BrickOutput> _cache = new();
    private readonly ILogger<SemanticCache> _logger;
    
    public SemanticCache(ILogger<SemanticCache> logger)
    {
        _logger = logger;
    }
    
    public Task<BrickOutput?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(cacheKey, out var output))
        {
            _logger.LogDebug("Cache hit for key {CacheKey}", cacheKey);
            return Task.FromResult<BrickOutput?>(output);
        }
        
        _logger.LogDebug("Cache miss for key {CacheKey}", cacheKey);
        return Task.FromResult<BrickOutput?>(null);
    }
    
    public Task SetAsync(string cacheKey, BrickOutput output, CancellationToken cancellationToken = default)
    {
        _cache[cacheKey] = output;
        _logger.LogDebug("Cached output for key {CacheKey}", cacheKey);
        return Task.CompletedTask;
    }
}

