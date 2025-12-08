using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.RateLimiting;

/// <summary>
/// Rate limiter for controlling request rates.
/// </summary>
public sealed class RateLimiter
{
    private readonly string _name;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;
    private readonly ILogger<RateLimiter>? _logger;
    private readonly ConcurrentDictionary<string, RateLimitWindow> _windows = new();

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
    /// </summary>
    public Task<RateLimitResult> AcquireAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        var window = GetOrCreateWindow(key);
        var now = DateTimeOffset.UtcNow;

        // Reset window if expired
        if (now - window.StartTime >= _window)
        {
            window.StartTime = now;
            window.RequestCount = 0;
        }

        // Check if limit exceeded
        if (window.RequestCount >= _maxRequests)
        {
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

        // Increment request count
        window.RequestCount++;
        var remaining = _maxRequests - window.RequestCount;

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
    /// </summary>
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
    /// </summary>
    public RateLimitStatus GetStatus(string key)
    {
        var window = GetOrCreateWindow(key);
        var now = DateTimeOffset.UtcNow;

        // Reset window if expired
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

    /// <summary>
    /// Resets rate limit for a key.
    /// </summary>
    public void Reset(string key)
    {
        if (_windows.TryRemove(key, out _))
        {
            _logger?.LogDebug("Reset rate limit for {Name} key {Key}", _name, key);
        }
    }

    /// <summary>
    /// Resets all rate limits.
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
            StartTime = DateTimeOffset.UtcNow,
            RequestCount = 0
        });
    }
}

/// <summary>
/// Rate limit window for tracking requests.
/// </summary>
internal sealed class RateLimitWindow
{
    public required string Key { get; init; }
    public DateTimeOffset StartTime { get; set; }
    public int RequestCount { get; set; }
}

/// <summary>
/// Result of a rate limit check.
/// </summary>
public sealed record RateLimitResult
{
    public required bool Allowed { get; init; }
    public required int RemainingRequests { get; init; }
    public required TimeSpan RetryAfter { get; init; }
}

/// <summary>
/// Current rate limit status.
/// </summary>
public sealed record RateLimitStatus
{
    public required string Key { get; init; }
    public required int RemainingRequests { get; init; }
    public required TimeSpan ResetAfter { get; init; }
    public required int Limit { get; init; }
}

/// <summary>
/// Exception thrown when rate limit is exceeded.
/// </summary>
public sealed class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message) : base(message) { }
}

