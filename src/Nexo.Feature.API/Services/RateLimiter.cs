using Microsoft.Extensions.Logging;
using Nexo.Feature.API.Enums;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using System.Collections.Concurrent;

namespace Nexo.Feature.API.Services;

/// <summary>
/// Rate limiter implementation using token bucket algorithm.
/// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
/// </summary>
public partial class RateLimiter : IRateLimiter
{
    private readonly ILogger<RateLimiter> _logger;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly ConcurrentDictionary<string, RateLimitConfiguration> _configurations = new();
    private readonly object _statisticsLock = new();
    private readonly Dictionary<string, long> _statistics = new();

    public RateLimiter(ILogger<RateLimiter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if a request is allowed based on rate limiting rules
    /// </summary>
    public async Task<RateLimitResult> CheckRateLimitAsync(RateLimitRequest request)
    {
        try
        {
            var bucketKey = GetBucketKey(request.Identifier, request.Scope);
            var bucket = GetOrCreateBucket(bucketKey, request.Scope);
            
            var now = DateTime.UtcNow;
            var isAllowed = bucket.TryConsume(request.Weight, now);
            
            var result = new RateLimitResult
            {
                IsAllowed = isAllowed,
                CurrentCount = bucket.GetCurrentTokens(now),
                MaxCount = bucket.Capacity,
                TimeWindowSeconds = (int)bucket.RefillTimeWindow.TotalSeconds,
                ResetInSeconds = (int)bucket.GetTimeUntilNextRefill(now).TotalSeconds,
                Identifier = request.Identifier,
                Scope = request.Scope,
                CheckedAt = now
            };

            // Update statistics
            IncrementStatistic("TotalChecks");
            if (!isAllowed)
            {
                IncrementStatistic("TotalRateLimited");
            }

            _logger.LogDebug("Rate limit check for {Identifier} ({Scope}): {IsAllowed}, {CurrentCount}/{MaxCount}", 
                request.Identifier, request.Scope, isAllowed, result.CurrentCount, result.MaxCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for {Identifier}", request.Identifier);
            
            // Allow request on error (fail open)
            return new RateLimitResult
            {
                IsAllowed = true,
                Identifier = request.Identifier,
                Scope = request.Scope,
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Records a request for rate limiting purposes
    /// </summary>
    public async Task<RateLimitRecordingResult> RecordRequestAsync(RateLimitRequest request)
    {
        try
        {
            var bucketKey = GetBucketKey(request.Identifier, request.Scope);
            var bucket = GetOrCreateBucket(bucketKey, request.Scope);
            
            var now = DateTime.UtcNow;
            var newCount = bucket.GetCurrentTokens(now);

            _logger.LogDebug("Recorded request for {Identifier} ({Scope}): {NewCount} tokens remaining", 
                request.Identifier, request.Scope, newCount);

            return new RateLimitRecordingResult
            {
                IsSuccess = true,
                Identifier = request.Identifier,
                NewCount = newCount,
                RecordedAt = now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording request for {Identifier}", request.Identifier);
            
            return new RateLimitRecordingResult
            {
                IsSuccess = false,
                Identifier = request.Identifier,
                ErrorMessage = ex.Message,
                RecordedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Gets current rate limit status for a specific identifier
    /// </summary>
    public async Task<RateLimitStatus> GetRateLimitStatusAsync(string identifier, RateLimitScope scope)
    {
        try
        {
            var bucketKey = GetBucketKey(identifier, scope);
            var bucket = GetOrCreateBucket(bucketKey, scope);
            var now = DateTime.UtcNow;

            return new RateLimitStatus
            {
                Identifier = identifier,
                Scope = scope,
                CurrentCount = bucket.GetCurrentTokens(now),
                MaxCount = bucket.Capacity,
                TimeWindowSeconds = (int)bucket.RefillTimeWindow.TotalSeconds,
                ResetInSeconds = (int)bucket.GetTimeUntilNextRefill(now).TotalSeconds,
                IsRateLimited = bucket.GetCurrentTokens(now) <= 0,
                LastRequestAt = bucket.LastRefillTime,
                StatusAt = now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rate limit status for {Identifier}", identifier);
            
            return new RateLimitStatus
            {
                Identifier = identifier,
                Scope = scope,
                StatusAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Resets rate limit counters for a specific identifier
    /// </summary>
    public async Task<RateLimitResetResult> ResetRateLimitAsync(string identifier, RateLimitScope scope)
    {
        try
        {
            var bucketKey = GetBucketKey(identifier, scope);
            var previousCount = 0;

            if (_buckets.TryGetValue(bucketKey, out var bucket))
            {
                previousCount = bucket.GetCurrentTokens(DateTime.UtcNow);
                bucket.Reset();
            }

            _logger.LogInformation("Reset rate limit for {Identifier} ({Scope})", identifier, scope);

            return new RateLimitResetResult
            {
                IsSuccess = true,
                Identifier = identifier,
                PreviousCount = previousCount,
                ResetAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting rate limit for {Identifier}", identifier);
            
            return new RateLimitResetResult
            {
                IsSuccess = false,
                Identifier = identifier,
                ErrorMessage = ex.Message,
                ResetAt = DateTime.UtcNow
            };
        }
    }
    // This class acts as an orchestrator for various rate limiting functionalities,
    // with specific categories defined in partial classes.
}