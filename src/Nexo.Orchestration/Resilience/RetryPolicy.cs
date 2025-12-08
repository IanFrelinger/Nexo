using Microsoft.Extensions.Logging;

namespace Nexo.Orchestration.Resilience;

/// <summary>
/// Configurable retry policy with multiple strategies.
/// </summary>
public sealed class RetryPolicy
{
    private readonly ILogger<RetryPolicy>? _logger;
    private readonly RetryStrategy _strategy;
    private readonly int _maxAttempts;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _maxDelay;
    private readonly double _backoffMultiplier;

    public RetryPolicy(
        RetryStrategy strategy = RetryStrategy.ExponentialBackoff,
        int maxAttempts = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        double backoffMultiplier = 2.0,
        ILogger<RetryPolicy>? logger = null)
    {
        _strategy = strategy;
        _maxAttempts = maxAttempts;
        _initialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        _maxDelay = maxDelay ?? TimeSpan.FromMinutes(5);
        _backoffMultiplier = backoffMultiplier;
        _logger = logger;
    }

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < _maxAttempts)
        {
            attempt++;

            try
            {
                var result = await operation();
                
                if (attempt > 1)
                {
                    _logger?.LogInformation("Operation succeeded on attempt {Attempt}", attempt);
                }

                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;

                // Check if we should retry
                if (shouldRetry != null && !shouldRetry(ex))
                {
                    _logger?.LogWarning("Operation failed with non-retryable error: {Error}", ex.Message);
                    throw;
                }

                if (attempt >= _maxAttempts)
                {
                    _logger?.LogError(ex, "Operation failed after {Attempts} attempts", _maxAttempts);
                    throw;
                }

                var delay = CalculateDelay(attempt);
                _logger?.LogWarning(ex, "Operation failed on attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms",
                    attempt, _maxAttempts, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed");
    }

    /// <summary>
    /// Executes an operation with retry logic (void return).
    /// </summary>
    public async Task ExecuteAsync(
        Func<Task> operation,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<object?>(async () =>
        {
            await operation();
            return null;
        }, shouldRetry, cancellationToken);
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        return _strategy switch
        {
            RetryStrategy.Fixed => _initialDelay,
            RetryStrategy.Linear => TimeSpan.FromMilliseconds(_initialDelay.TotalMilliseconds * attempt),
            RetryStrategy.ExponentialBackoff => TimeSpan.FromMilliseconds(
                Math.Min(
                    _initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt - 1),
                    _maxDelay.TotalMilliseconds)),
            RetryStrategy.JitteredExponentialBackoff => TimeSpan.FromMilliseconds(
                Math.Min(
                    _initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, attempt - 1) * (0.5 + Random.Shared.NextDouble() * 0.5),
                    _maxDelay.TotalMilliseconds)),
            _ => _initialDelay
        };
    }
}

/// <summary>
/// Retry strategy types.
/// </summary>
public enum RetryStrategy
{
    Fixed,                        // Fixed delay between retries
    Linear,                       // Linear increase in delay
    ExponentialBackoff,           // Exponential increase in delay
    JitteredExponentialBackoff    // Exponential with random jitter
}

