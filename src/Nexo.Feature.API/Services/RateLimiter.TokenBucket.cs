using Microsoft.Extensions.Logging;
using Nexo.Feature.API.Enums;
using Nexo.Feature.API.Models;
using System.Collections.Concurrent;

namespace Nexo.Feature.API.Services;

/// <summary>
/// Token bucket management and core rate limiting logic
/// </summary>
public partial class RateLimiter
{
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
