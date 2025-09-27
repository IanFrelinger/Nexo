using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Caching;
using System.Collections.Concurrent;
using System.Linq;

namespace Nexo.Infrastructure.Services.Caching
{
    /// <summary>
    /// In-memory cache adapter implementing IDistributedCache.
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class MemoryCacheAdapter : IDistributedCache
    {
        private readonly ILogger<MemoryCacheAdapter> _logger;
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
        private readonly ICacheSerializer _serializer;
        private readonly ICacheEvictionPolicy _evictionPolicy;
        private readonly SemaphoreSlim _cacheLock = new(1, 1);
        private readonly Timer _cleanupTimer;
        private readonly long _maxSizeBytes;
        private readonly int _maxItems;

        private readonly CacheStatistics _statistics = new();
        private long _currentSizeBytes;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _perKeyLocks = new();

        public MemoryCacheAdapter(
            ILogger<MemoryCacheAdapter> logger,
            ICacheSerializer serializer,
            ICacheEvictionPolicy evictionPolicy, Timer cleanupTimer, long maxSizeBytes = 100 * 1024 * 1024, // 100MB default
            int maxItems = 10000)
        {
            ArgumentNullException.ThrowIfNull(logger);
            serializer ??= new JsonCacheSerializer();
            evictionPolicy ??= new LruEvictionPolicy();
            _logger = logger;
            _serializer = serializer;
            _evictionPolicy = evictionPolicy;
            _cleanupTimer = cleanupTimer;
            _maxSizeBytes = maxSizeBytes;
            _maxItems = maxItems;

            // Start cleanup timer to remove expired items
            // _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            _logger.LogInformation("Memory cache adapter initialized with max size: {MaxSize}MB, max items: {MaxItems}", 
                maxSizeBytes / (1024 * 1024), maxItems);
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            _cacheLock?.Dispose();
        }
        // This class acts as an orchestrator for various cache functionalities,
        // with specific categories defined in partial classes.
    }
}