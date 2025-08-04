using System;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Application.Services
{
    /// <summary>
    /// Decorator that adds caching behavior to any async processor.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class CachingAsyncProcessor<TRequest, TKey, TResponse> : IAsyncProcessor<TRequest, TResponse>
    {
        private readonly IAsyncProcessor<TRequest, TResponse> _innerProcessor;
        private readonly ICacheStrategy<TKey, TResponse> _cache;
        private readonly Func<TRequest, TKey> _keySelector;
        private readonly ILogger<CachingAsyncProcessor<TRequest, TKey, TResponse>> _logger;

        public CachingAsyncProcessor(
            IAsyncProcessor<TRequest, TResponse> innerProcessor,
            ICacheStrategy<TKey, TResponse> cache,
            Func<TRequest, TKey> keySelector,
            ILogger<CachingAsyncProcessor<TRequest, TKey, TResponse>> logger = null)
        {
            _innerProcessor = innerProcessor ?? throw new ArgumentNullException(nameof(innerProcessor));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _logger = logger;
        }

        public async Task<TResponse> ProcessAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            var cacheKey = _keySelector(request);
            _logger?.LogDebug("Processing request with cache key: {CacheKey}", cacheKey);
            
            var cacheStrategy = _cache as CacheStrategy<TKey, TResponse>;
            bool found = false;
            TResponse cachedResponse = default(TResponse);
            
            if (cacheStrategy != null)
            {
                _logger?.LogDebug("Using CacheStrategy.TryGetValue for cache lookup");
                found = cacheStrategy.TryGetValue(cacheKey, out cachedResponse);
            }
            else
            {
                _logger?.LogDebug("Using fallback GetAsync for cache lookup");
                cachedResponse = await _cache.GetAsync(cacheKey, cancellationToken);
                found = cachedResponse != null;
            }
            
            if (found)
            {
                _logger?.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return cachedResponse;
            }
            
            _logger?.LogDebug("Cache miss for key: {CacheKey}, processing request", cacheKey);
            var response = await _innerProcessor.ProcessAsync(request, cancellationToken);
            
            _logger?.LogDebug("Setting cache for key: {CacheKey}", cacheKey);
            await _cache.SetAsync(cacheKey, response, ttl: null, cancellationToken);
            
            return response;
        }
    }
} 