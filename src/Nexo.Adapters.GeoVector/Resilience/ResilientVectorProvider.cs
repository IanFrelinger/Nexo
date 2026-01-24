using Microsoft.Extensions.Logging;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Models;
using Nexo.GeoVector.Values;
using Nexo.Orchestration.GeoVector.Ports;
using Nexo.Orchestration.Resilience;
using Nexo.Orchestration.RateLimiting;

namespace Nexo.Adapters.GeoVector.Resilience;

/// <summary>
/// Wraps an IVectorProvider with retry logic, circuit breaker, and optional rate limiting.
/// Provides production-ready resilience for network-dependent vector providers.
/// </summary>
public sealed class ResilientVectorProvider : IVectorProvider
{
    private readonly IVectorProvider _inner;
    private readonly RetryPolicy _retryPolicy;
    private readonly CircuitBreaker? _circuitBreaker;
    private readonly RateLimiter? _rateLimiter;
    private readonly ILogger<ResilientVectorProvider>? _logger;

    public ResilientVectorProvider(
        IVectorProvider inner,
        RetryPolicy? retryPolicy = null,
        CircuitBreaker? circuitBreaker = null,
        RateLimiter? rateLimiter = null,
        ILogger<ResilientVectorProvider>? logger = null)
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

    public async Task<GeoFeatureSet> GetFeaturesAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken = default)
    {
        var operationKey = $"vector-features-{kind.Value}";

        // Rate limiting (if configured)
        if (_rateLimiter != null)
        {
            return await _rateLimiter.ExecuteAsync(operationKey, async () =>
            {
                return await ExecuteWithResilienceAsync(bounds, kind, operationKey, cancellationToken);
            }, cancellationToken);
        }

        return await ExecuteWithResilienceAsync(bounds, kind, operationKey, cancellationToken);
    }

    private async Task<GeoFeatureSet> ExecuteWithResilienceAsync(
        GeoBounds bounds,
        FeatureKind kind,
        string operationKey,
        CancellationToken cancellationToken)
    {
        // Circuit breaker (if configured)
        if (_circuitBreaker != null)
        {
            return await _circuitBreaker.ExecuteAsync(
                operationKey,
                async () => await ExecuteWithRetryAsync(bounds, kind, cancellationToken),
                fallback: ex => throw new InvalidOperationException($"Vector provider circuit breaker is open for kind {kind.Value}", ex),
                cancellationToken);
        }

        return await ExecuteWithRetryAsync(bounds, kind, cancellationToken);
    }

    private async Task<GeoFeatureSet> ExecuteWithRetryAsync(GeoBounds bounds, FeatureKind kind, CancellationToken cancellationToken)
    {
        return await _retryPolicy.ExecuteAsync(
            async () => await _inner.GetFeaturesAsync(bounds, kind, cancellationToken),
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
