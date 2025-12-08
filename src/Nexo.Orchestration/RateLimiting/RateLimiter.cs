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
            StartTime = DateTimeOffset.UtcNow
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
    private int _requestCount;

    // Lock for thread-safe window reset operations
    public readonly object SyncLock = new();

    public int RequestCount => _requestCount;

    /// <summary>
    /// Atomically increments the request count and returns the new value.
    /// </summary>
    public int IncrementAndGet() => Interlocked.Increment(ref _requestCount);

    /// <summary>
    /// Atomically decrements the request count.
    /// </summary>
    public void Decrement() => Interlocked.Decrement(ref _requestCount);

    /// <summary>
    /// Resets the window for a new time period.
    /// </summary>
    public void Reset(DateTimeOffset newStartTime)
    {
        StartTime = newStartTime;
        Interlocked.Exchange(ref _requestCount, 0);
    }
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

