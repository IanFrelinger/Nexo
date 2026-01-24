using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.Orchestration.GeoTerrain.Ports;
using Nexo.Orchestration.Resilience;
using Nexo.Orchestration.RateLimiting;

namespace Nexo.Adapters.GeoTerrain.Resilience;

/// <summary>
/// Wraps an IElevationProvider with retry logic, circuit breaker, and optional rate limiting.
/// Provides production-ready resilience for network-dependent elevation providers.
/// </summary>
public sealed class ResilientElevationProvider : IElevationProvider
{
    private readonly IElevationProvider _inner;
    private readonly RetryPolicy _retryPolicy;
    private readonly CircuitBreaker? _circuitBreaker;
    private readonly RateLimiter? _rateLimiter;
    private readonly ILogger<ResilientElevationProvider>? _logger;

    public ResilientElevationProvider(
        IElevationProvider inner,
        RetryPolicy? retryPolicy = null,
        CircuitBreaker? circuitBreaker = null,
        RateLimiter? rateLimiter = null,
        ILogger<ResilientElevationProvider>? logger = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _retryPolicy = retryPolicy ?? new RetryPolicy(
            strategy: RetryStrategy.ExponentialBackoff,
            maxAttempts: 3,
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromMinutes(2),
            logger: null); // RetryPolicy will use its own logger if needed
        _circuitBreaker = circuitBreaker;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<ElevationTile> GetSrtmTileAsync(SrtmTileId tileId, CancellationToken cancellationToken = default)
    {
        var operationKey = $"elevation-tile-{tileId}";

        // Rate limiting (if configured)
        if (_rateLimiter != null)
        {
            return await _rateLimiter.ExecuteAsync(operationKey, async () =>
            {
                return await ExecuteWithResilienceAsync(tileId, operationKey, cancellationToken);
            }, cancellationToken);
        }

        return await ExecuteWithResilienceAsync(tileId, operationKey, cancellationToken);
    }

    private async Task<ElevationTile> ExecuteWithResilienceAsync(
        SrtmTileId tileId,
        string operationKey,
        CancellationToken cancellationToken)
    {
        // Circuit breaker (if configured)
        if (_circuitBreaker != null)
        {
            return await _circuitBreaker.ExecuteAsync(
                operationKey,
                async () => await ExecuteWithRetryAsync(tileId, cancellationToken),
                fallback: ex => throw new InvalidOperationException($"Elevation provider circuit breaker is open for tile {tileId}", ex),
                cancellationToken);
        }

        return await ExecuteWithRetryAsync(tileId, cancellationToken);
    }

    private async Task<ElevationTile> ExecuteWithRetryAsync(SrtmTileId tileId, CancellationToken cancellationToken)
    {
        return await _retryPolicy.ExecuteAsync(
            async () => await _inner.GetSrtmTileAsync(tileId, cancellationToken),
            shouldRetry: IsRetryableException,
            cancellationToken);
    }

    private static bool IsRetryableException(Exception ex)
    {
        // Retry on network-related exceptions
        if (ex is HttpRequestException || ex is TaskCanceledException || ex is TimeoutException)
            return true;

        // Don't retry on argument/validation errors
        if (ex is ArgumentException || ex is ArgumentNullException || ex is InvalidOperationException)
            return false;

        // Default: retry (conservative approach)
        return true;
    }
}
