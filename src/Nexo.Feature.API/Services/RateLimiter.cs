using Microsoft.Extensions.Logging;
using Nexo.Feature.API.Enums;
using Nexo.Feature.API.Interfaces;
using Nexo.Feature.API.Models;
using System.Collections.Concurrent;

namespace Nexo.Feature.API.Services;

/// <summary>
/// Rate limiter implementation using token bucket algorithm
/// </summary>
public class RateLimiter : IRateLimiter
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

    /// <summary>
    /// Configures rate limiting rules
    /// </summary>
    public async Task<RateLimitConfigurationResult> ConfigureRateLimitingAsync(RateLimitConfiguration configuration)
    {
        try
        {
            if (string.IsNullOrEmpty(configuration.Identifier))
            {
                return new RateLimitConfigurationResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Identifier is required"
                };
            }

            var bucketKey = GetBucketKey(configuration.Identifier, configuration.Scope);
            
            // Store configuration
            _configurations.AddOrUpdate(bucketKey, configuration, (key, oldValue) => configuration);

            // Update or create bucket with new configuration
            var bucket = GetOrCreateBucket(bucketKey, configuration.Scope);
            bucket.UpdateConfiguration(configuration.MaxRequests, TimeSpan.FromSeconds(configuration.TimeWindowSeconds));

            _logger.LogInformation("Configured rate limiting for {Identifier} ({Scope}): {MaxRequests} requests per {TimeWindowSeconds}s", 
                configuration.Identifier, configuration.Scope, configuration.MaxRequests, configuration.TimeWindowSeconds);

            return new RateLimitConfigurationResult
            {
                IsSuccess = true,
                Identifier = configuration.Identifier,
                ConfiguredAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring rate limiting for {Identifier}", configuration.Identifier);
            
            return new RateLimitConfigurationResult
            {
                IsSuccess = false,
                Identifier = configuration.Identifier,
                ErrorMessage = ex.Message,
                ConfiguredAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Gets rate limiting statistics and metrics
    /// </summary>
    public async Task<RateLimitStatistics> GetStatisticsAsync()
    {
        lock (_statisticsLock)
        {
            var totalChecks = GetStatistic("TotalChecks");
            var totalRateLimited = GetStatistic("TotalRateLimited");

            return new RateLimitStatistics
            {
                TotalChecks = totalChecks,
                TotalRateLimited = totalRateLimited,
                ActiveConfigurations = _configurations.Count,
                StatisticsByScope = GetStatisticsByScope(),
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Gets or creates a token bucket for the given key and scope
    /// </summary>
    private TokenBucket GetOrCreateBucket(string bucketKey, RateLimitScope scope)
    {
        return _buckets.GetOrAdd(bucketKey, key =>
        {
            var config = GetDefaultConfiguration(scope);
            return new TokenBucket(config.MaxRequests, TimeSpan.FromSeconds(config.TimeWindowSeconds));
        });
    }

    /// <summary>
    /// Gets the bucket key for an identifier and scope
    /// </summary>
    private string GetBucketKey(string identifier, RateLimitScope scope)
    {
        return $"{scope}:{identifier}";
    }

    /// <summary>
    /// Gets default configuration for a scope
    /// </summary>
    private RateLimitConfiguration GetDefaultConfiguration(RateLimitScope scope)
    {
        return scope switch
        {
            RateLimitScope.User => new RateLimitConfiguration
            {
                MaxRequests = 100,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            RateLimitScope.Service => new RateLimitConfiguration
            {
                MaxRequests = 1000,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            RateLimitScope.Global => new RateLimitConfiguration
            {
                MaxRequests = 10000,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            RateLimitScope.IPAddress => new RateLimitConfiguration
            {
                MaxRequests = 200,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            RateLimitScope.APIKey => new RateLimitConfiguration
            {
                MaxRequests = 500,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            RateLimitScope.Endpoint => new RateLimitConfiguration
            {
                MaxRequests = 300,
                TimeWindowSeconds = 60,
                Scope = scope
            },
            _ => new RateLimitConfiguration
            {
                MaxRequests = 100,
                TimeWindowSeconds = 60,
                Scope = scope
            }
        };
    }

    /// <summary>
    /// Increments a statistic counter
    /// </summary>
    private void IncrementStatistic(string key)
    {
        lock (_statisticsLock)
        {
            if (_statistics.ContainsKey(key))
            {
                _statistics[key]++;
            }
            else
            {
                _statistics[key] = 1;
            }
        }
    }

    /// <summary>
    /// Gets a statistic value
    /// </summary>
    private long GetStatistic(string key)
    {
        lock (_statisticsLock)
        {
            return _statistics.GetValueOrDefault(key, 0);
        }
    }

    /// <summary>
    /// Gets statistics grouped by scope
    /// </summary>
    private Dictionary<RateLimitScope, long> GetStatisticsByScope()
    {
        var result = new Dictionary<RateLimitScope, long>();
        foreach (RateLimitScope scope in Enum.GetValues(typeof(RateLimitScope)))
        {
            result[scope] = 0; // TODO: Implement scope-specific statistics
        }
        return result;
    }
}

/// <summary>
/// Token bucket implementation for rate limiting
/// </summary>
public class TokenBucket
{
    private readonly object _lock = new();
    private int _tokens;
    private DateTime _lastRefillTime;
    private int _capacity;
    private TimeSpan _refillTimeWindow;

    public TokenBucket(int capacity, TimeSpan refillTimeWindow)
    {
        _capacity = capacity;
        _refillTimeWindow = refillTimeWindow;
        _tokens = capacity;
        _lastRefillTime = DateTime.UtcNow;
    }

    public int Capacity => _capacity;
    public TimeSpan RefillTimeWindow => _refillTimeWindow;
    public DateTime LastRefillTime => _lastRefillTime;

    /// <summary>
    /// Tries to consume tokens from the bucket
    /// </summary>
    public bool TryConsume(int tokens, DateTime now)
    {
        lock (_lock)
        {
            RefillTokens(now);
            
            if (_tokens >= tokens)
            {
                _tokens -= tokens;
                return true;
            }
            
            return false;
        }
    }

    /// <summary>
    /// Gets the current number of tokens
    /// </summary>
    public int GetCurrentTokens(DateTime now)
    {
        lock (_lock)
        {
            RefillTokens(now);
            return _tokens;
        }
    }

    /// <summary>
    /// Gets the time until the next token refill
    /// </summary>
    public TimeSpan GetTimeUntilNextRefill(DateTime now)
    {
        var timeSinceLastRefill = now - _lastRefillTime;
        if (timeSinceLastRefill >= _refillTimeWindow)
        {
            return TimeSpan.Zero;
        }
        
        return _refillTimeWindow - timeSinceLastRefill;
    }

    /// <summary>
    /// Updates the bucket configuration
    /// </summary>
    public void UpdateConfiguration(int capacity, TimeSpan refillTimeWindow)
    {
        lock (_lock)
        {
            _capacity = capacity;
            _refillTimeWindow = refillTimeWindow;
            
            // Adjust current tokens if capacity changed
            if (_tokens > _capacity)
            {
                _tokens = _capacity;
            }
        }
    }

    /// <summary>
    /// Resets the bucket to full capacity
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _tokens = _capacity;
            _lastRefillTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Refills tokens based on time elapsed
    /// </summary>
    private void RefillTokens(DateTime now)
    {
        var timeSinceLastRefill = now - _lastRefillTime;
        var refillCycles = (int)(timeSinceLastRefill.TotalMilliseconds / _refillTimeWindow.TotalMilliseconds);
        
        if (refillCycles > 0)
        {
            _tokens = Math.Min(_capacity, _tokens + refillCycles);
            _lastRefillTime = now;
        }
    }
} 