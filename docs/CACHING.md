# Caching System Documentation

## Overview

Nexo provides a flexible, compositional caching system that supports both in-memory and distributed Redis caching. The caching system is designed to be decoupled from core services using the decorator pattern, allowing for easy integration and configuration.

## Architecture

### Core Components

1. **ICacheStrategy<TKey, TValue>** - Interface defining cache operations
2. **CacheStrategy<TKey, TValue>** - In-memory implementation
3. **RedisCacheStrategy<TKey, TValue>** - Redis-backed distributed implementation
4. **CachingAsyncProcessor<TRequest, TKey, TResponse>** - Decorator that adds caching to any async processor
5. **CacheConfigurationService** - Factory for creating configured cache strategies
6. **SemanticCacheKeyGenerator** - Generates cache keys based on normalized input and context

### Design Principles

- **Compositional**: Caching is implemented as a decorator around `IAsyncProcessor`
- **Decoupled**: Core services don't depend on caching implementation
- **Configurable**: Easy switching between in-memory and Redis backends
- **Semantic**: Cache keys are generated based on normalized input and context
- **Multi-target**: Compatible with .NET 8, .NET Framework 4.8, and .NET Standard 2.0

## Configuration

### Environment Variables

```bash
# Cache backend selection (inmemory, redis)
CACHE_BACKEND=inmemory

# Default TTL in seconds
CACHE_TTL_SECONDS=300

# Redis-specific configuration
REDIS_CONNECTION_STRING=localhost:6379
REDIS_KEY_PREFIX=nexo:cache:
```

### Cache Settings Model

```csharp
public class CacheSettings
{
    public string Backend { get; set; } = "inmemory";
    public string RedisConnectionString { get; set; } = "localhost:6379";
    public string RedisKeyPrefix { get; set; } = "nexo:cache:";
    public int DefaultTtlSeconds { get; set; } = 300;
}
```

## Usage Examples

### Basic In-Memory Caching

```csharp
// Create in-memory cache with 5-minute TTL
var cache = new CacheStrategy<string, ModelResponse>(TimeSpan.FromMinutes(5));

// Create core processor
var coreProcessor = new AsyncProcessor<ModelRequest, ModelResponse>(
    async (request, cancellationToken) => {
        // Your processing logic here
        return await ProcessRequestAsync(request, cancellationToken);
    });

// Compose with caching
var cachingProcessor = new CachingAsyncProcessor<ModelRequest, string, ModelResponse>(
    coreProcessor,
    cache,
    keySelector: request => SemanticCacheKeyGenerator.Generate(
        request.Input,
        context: null,
        parameters: new Dictionary<string, object> { { "model", "gpt-4" } }
    )
);

// Use the caching processor
var response = await cachingProcessor.ProcessAsync(request);
```

### Redis Distributed Caching

```csharp
// Create Redis cache strategy
var redis = ConnectionMultiplexer.Connect("localhost:6379");
var cache = new RedisCacheStrategy<string, ModelResponse>(redis, "nexo:cache:");

// Use the same composition pattern
var cachingProcessor = new CachingAsyncProcessor<ModelRequest, string, ModelResponse>(
    coreProcessor,
    cache,
    keySelector: request => SemanticCacheKeyGenerator.Generate(request.Input)
);
```

### Configuration-Based Caching

```csharp
// Create configuration service
var settings = new CacheSettings
{
    Backend = "redis",
    RedisConnectionString = "localhost:6379",
    DefaultTtlSeconds = 600
};

var cacheService = new CacheConfigurationService(settings);

// Create configured cache strategy
var cache = cacheService.CreateCacheStrategy<string, ModelResponse>();

// Compose with caching
var cachingProcessor = new CachingAsyncProcessor<ModelRequest, string, ModelResponse>(
    coreProcessor,
    cache,
    keySelector: request => SemanticCacheKeyGenerator.Generate(request.Input)
);
```

## CLI Integration

### Configuration Commands

```bash
# Show current cache configuration
nexo config show

# Set cache backend and TTL
nexo config set-cache redis 600
```

### Environment-Based Configuration

```bash
# Use Redis with custom settings
export CACHE_BACKEND=redis
export REDIS_CONNECTION_STRING=redis://localhost:6379
export CACHE_TTL_SECONDS=600
export REDIS_KEY_PREFIX=myapp:cache:

# Run with caching enabled
nexo ai suggest "your code here"
```

## Cache Key Generation

### Semantic Cache Keys

The `SemanticCacheKeyGenerator` creates cache keys based on:

1. **Normalized Input**: Whitespace-normalized, case-insensitive input
2. **Context**: Additional context parameters (optional)
3. **Model Parameters**: AI model, provider, and other parameters

```csharp
var key = SemanticCacheKeyGenerator.Generate(
    input: "Analyze this code",
    context: new Dictionary<string, object> { { "depth", "deep" } },
    parameters: new Dictionary<string, object> { { "model", "gpt-4" } }
);
// Result: "analyze_this_code|depth:deep|model:gpt-4"
```

### Key Benefits

- **Consistent**: Same input always generates the same key
- **Normalized**: Handles whitespace and case variations
- **Contextual**: Includes relevant context and parameters
- **Collision-resistant**: Uses hash-based approach for uniqueness

## Cache Operations

### Basic Operations

```csharp
// Set value with optional TTL
await cache.SetAsync("key", value, TimeSpan.FromMinutes(5));

// Get value
var value = await cache.GetAsync("key");

// Remove specific key
await cache.RemoveAsync("key");

// Clear all cache entries
await cache.ClearAsync();

// Invalidate (alias for Remove)
await cache.InvalidateAsync("key");
```

### TTL and Expiration

```csharp
// Set with TTL
await cache.SetAsync("key", value, TimeSpan.FromMinutes(10));

// Set without TTL (uses default)
await cache.SetAsync("key", value);

// Check if value exists and is not expired
var value = await cache.GetAsync("key");
if (value != null)
{
    // Value exists and is not expired
}
```

## Performance Considerations

### In-Memory Caching

- **Pros**: Fastest access, no network overhead
- **Cons**: Limited by available memory, not shared across processes
- **Best for**: Single-process applications, development environments

### Redis Caching

- **Pros**: Shared across processes, persistent, scalable
- **Cons**: Network overhead, requires Redis infrastructure
- **Best for**: Multi-process applications, production environments

### Memory Management

```csharp
// Configure memory limits for in-memory cache
var cache = new CacheStrategy<string, ModelResponse>(
    ttl: TimeSpan.FromMinutes(5),
    maxSize: 1000 // Maximum number of entries
);
```

## Error Handling

### Graceful Degradation

```csharp
try
{
    var cache = cacheService.CreateCacheStrategy<string, ModelResponse>();
    var cachingProcessor = new CachingAsyncProcessor<ModelRequest, string, ModelResponse>(
        coreProcessor, cache, keySelector);
    
    return await cachingProcessor.ProcessAsync(request);
}
catch (RedisConnectionException ex)
{
    // Fall back to core processor without caching
    _logger.LogWarning("Cache unavailable, falling back to core processor: {Message}", ex.Message);
    return await coreProcessor.ProcessAsync(request);
}
```

### Configuration Validation

```csharp
// Validate cache configuration
var isValid = await cacheService.ValidateConfigurationAsync();
if (!isValid)
{
    _logger.LogWarning("Cache configuration is invalid, using in-memory fallback");
    // Fall back to in-memory cache
}
```

## Testing

### Unit Tests

```csharp
[Fact]
public async Task CachingAsyncProcessor_CachesResults()
{
    var cache = new CacheStrategy<string, int>();
    var callCount = 0;
    
    var coreProcessor = new AsyncProcessor<string, int>(
        async (input, ct) => { callCount++; return input.Length; });
    
    var cachingProcessor = new CachingAsyncProcessor<string, string, int>(
        coreProcessor, cache, input => input);
    
    // First call - should hit core processor
    var result1 = await cachingProcessor.ProcessAsync("test");
    Assert.Equal(1, callCount);
    
    // Second call - should hit cache
    var result2 = await cachingProcessor.ProcessAsync("test");
    Assert.Equal(1, callCount); // No additional call to core processor
    Assert.Equal(result1, result2);
}
```

### Integration Tests

```csharp
[Fact]
public async Task RedisCacheStrategy_WorksWithRealRedis()
{
    try
    {
        var redis = ConnectionMultiplexer.Connect("localhost:6379");
        var cache = new RedisCacheStrategy<string, int>(redis);
        
        await cache.SetAsync("test", 42, TimeSpan.FromSeconds(1));
        var value = await cache.GetAsync("test");
        Assert.Equal(42, value);
    }
    catch (RedisConnectionException)
    {
        // Skip test if Redis is not available
        Assert.True(true);
    }
}
```

## Best Practices

### 1. Choose Appropriate TTL

```csharp
// Short TTL for frequently changing data
await cache.SetAsync("user-session", session, TimeSpan.FromMinutes(30));

// Long TTL for static data
await cache.SetAsync("configuration", config, TimeSpan.FromHours(24));
```

### 2. Use Semantic Keys

```csharp
// Good: Include relevant context
var key = SemanticCacheKeyGenerator.Generate(
    input, 
    new Dictionary<string, object> { { "user", userId } },
    new Dictionary<string, object> { { "model", modelName } }
);

// Avoid: Simple string concatenation
var key = $"{input}_{userId}_{modelName}"; // Not recommended
```

### 3. Handle Cache Failures Gracefully

```csharp
public async Task<TResponse> ProcessWithCaching<TResponse>(TRequest request)
{
    try
    {
        return await cachingProcessor.ProcessAsync(request);
    }
    catch (Exception ex) when (ex is RedisConnectionException || ex is InvalidOperationException)
    {
        _logger.LogWarning("Cache failed, using core processor: {Message}", ex.Message);
        return await coreProcessor.ProcessAsync(request);
    }
}
```

### 4. Monitor Cache Performance

```csharp
// Track cache hit rates
var stopwatch = Stopwatch.StartNew();
var result = await cachingProcessor.ProcessAsync(request);
stopwatch.Stop();

if (result.FromCache)
{
    _metrics.IncrementCacheHit();
}
else
{
    _metrics.IncrementCacheMiss();
    _metrics.RecordProcessingTime(stopwatch.Elapsed);
}
```

## Troubleshooting

### Common Issues

1. **Redis Connection Failures**
   - Check Redis server is running
   - Verify connection string format
   - Ensure network connectivity

2. **Cache Key Collisions**
   - Use `SemanticCacheKeyGenerator` for consistent keys
   - Include relevant context in key generation
   - Avoid simple string concatenation

3. **Memory Pressure**
   - Monitor cache size and memory usage
   - Adjust TTL settings
   - Consider using Redis for large datasets

4. **Serialization Errors**
   - Ensure cached objects are serializable
   - Use appropriate JSON serialization options
   - Handle circular references

### Debugging

```csharp
// Enable detailed logging
var cache = new CacheStrategy<string, ModelResponse>(
    ttl: TimeSpan.FromMinutes(5),
    enableLogging: true
);

// Check cache statistics
var stats = cache.GetStatistics();
Console.WriteLine($"Hits: {stats.HitCount}, Misses: {stats.MissCount}");
```

## Migration Guide

### From No Caching to In-Memory

1. Add cache strategy to your service
2. Wrap your processor with `CachingAsyncProcessor`
3. Configure TTL and other settings
4. Test performance improvements

### From In-Memory to Redis

1. Install Redis infrastructure
2. Update configuration to use Redis backend
3. Set environment variables for Redis connection
4. Test distributed caching behavior

### From Custom Caching to Nexo Caching

1. Replace custom cache implementations with `ICacheStrategy`
2. Use `CachingAsyncProcessor` for composition
3. Migrate cache key generation to `SemanticCacheKeyGenerator`
4. Update configuration to use `CacheConfigurationService` 