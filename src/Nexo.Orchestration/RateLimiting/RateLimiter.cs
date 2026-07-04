using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.RateLimiting;

/// <summary>
/// Rate limiter for controlling request rates.
/// 
/// Implements sliding window rate limiting:
/// - Tracks requests per key within a time window
/// - Rejects requests that exceed maxRequests per window
/// - Provides retry-after information for rejected requests
/// - Thread-safe implementation using concurrent collections
/// 
/// Used to prevent API rate limit violations and control resource consumption.
/// </summary>
public sealed class RateLimiter
{
    private readonly string _name;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly ILogger<RateLimiter>? _logger;
    private readonly ConcurrentDictionary<string, RateLimitWindow> _windows = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimiter"/> class.
    /// </summary>
    /// <param name="name">The name of the rate limiter (for logging and identification).</param>
    /// <param name="maxRequests">Maximum number of requests allowed per window.</param>
    /// <param name="window">The time window for rate limiting (sliding window).</param>
    /// <param name="logger">Optional logger instance.</param>
    public RateLimiter(
        string name,
        int maxRequests,
        TimeSpan window,
        ILogger<RateLimiter>? logger = null)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _maxRequests = maxRequests;
        _window = window;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to acquire a permit for a request.
    /// 
    /// Checks if the request key has exceeded the rate limit within the current window.
    /// Returns success if permit is granted, or rejection with retry-after time if limit exceeded.
    /// </summary>
    /// <param name="key">Unique key identifying the rate limit scope (e.g., API endpoint, user ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit result indicating success or rejection with retry-after time</returns>
    public Task<RateLimitResult> AcquireAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var window = GetOrCreateWindow(key);
        var now = DateTimeOffset.UtcNow;

        // Reset window if expired (needs synchronization)
        lock (window.SyncLock)
        {
            if (now - window.StartTime >= _window)
            {
                window.Reset(now);
            }
        }

        // Atomically increment and check
        var newCount = window.IncrementAndGet();

        if (newCount > _maxRequests)
        {
            // Exceeded limit - decrement and reject
            window.Decrement();
            var retryAfter = _window - (now - window.StartTime);
            _logger?.LogWarning("Rate limit exceeded for {Name} key {Key}. Retry after {RetryAfter}ms",
                _name, key, retryAfter.TotalMilliseconds);

            return Task.FromResult(new RateLimitResult
            {
                Allowed = false,
                RemainingRequests = 0,
                RetryAfter = retryAfter
            });
        }

        var remaining = _maxRequests - newCount;
        _logger?.LogDebug("Rate limit permit acquired for {Name} key {Key}. Remaining: {Remaining}",
            _name, key, remaining);

        return Task.FromResult(new RateLimitResult
        {
            Allowed = true,
            RemainingRequests = remaining,
            RetryAfter = TimeSpan.Zero
        });
    }

    /// <summary>
    /// Executes an operation with rate limiting.
    /// 
    /// Attempts to acquire a permit, then executes the operation if permitted.
    /// Throws RateLimitExceededException if the rate limit is exceeded.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="key">The rate limit key (e.g., API endpoint, user ID).</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="RateLimitExceededException">Thrown if the rate limit is exceeded.</exception>
    public async Task<T> ExecuteAsync<T>(
        string key,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var result = await AcquireAsync(key, cancellationToken);

        if (!result.Allowed)
        {
            throw new RateLimitExceededException(
                $"Rate limit exceeded for {_name}. Retry after {result.RetryAfter.TotalMilliseconds}ms");
        }

        return await operation();
    }

    /// <summary>
    /// Gets current rate limit status for a key.
    /// 
    /// Returns information about remaining requests, reset time, and limit.
    /// </summary>
    /// <param name="key">The rate limit key to check.</param>
    /// <returns>A <see cref="RateLimitStatus"/> with current rate limit information.</returns>
    public RateLimitStatus GetStatus(string key)
    {
        var window = GetOrCreateWindow(key);
        var now = DateTimeOffset.UtcNow;

        // Reset window if expired (needs synchronization)
        lock (window.SyncLock)
        {
            if (now - window.StartTime >= _window)
            {
                return new RateLimitStatus
                {
                    Key = key,
                    RemainingRequests = _maxRequests,
                    ResetAfter = TimeSpan.Zero,
                    Limit = _maxRequests
                };
            }

            return new RateLimitStatus
            {
                Key = key,
                RemainingRequests = Math.Max(0, _maxRequests - window.RequestCount),
                ResetAfter = _window - (now - window.StartTime),
                Limit = _maxRequests
            };
        }
    }

    /// <summary>
    /// Resets rate limit for a key.
    /// 
    /// Removes the rate limit window for the specified key, effectively resetting the limit.
    /// </summary>
    /// <param name="key">The rate limit key to reset.</param>
    public void Reset(string key)
    {
        if (_windows.TryRemove(key, out _))
        {
            _logger?.LogDebug("Reset rate limit for {Name} key {Key}", _name, key);
        }
    }

    /// <summary>
    /// Resets all rate limits.
    /// 
    /// Clears all rate limit windows, effectively resetting all limits.
    /// </summary>
    public void ResetAll()
    {
        _windows.Clear();
        _logger?.LogDebug("Reset all rate limits for {Name}", _name);
    }

    private RateLimitWindow GetOrCreateWindow(string key)
    {
        return _windows.GetOrAdd(key, _ => new RateLimitWindow
        {
            Key = key,
            StartTime = DateTimeOffset.UtcNow
        });
    }
}
