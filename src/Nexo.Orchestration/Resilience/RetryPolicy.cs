using Microsoft.Extensions.Logging;

namespace Nexo.Orchestration.Resilience;

/// <summary>
/// Configurable retry policy with multiple strategies.
/// 
/// Supports multiple retry strategies:
/// - Fixed: Constant delay between retries
/// - Linear: Linearly increasing delay
/// - ExponentialBackoff: Exponentially increasing delay
/// - JitteredExponentialBackoff: Exponential with random jitter
/// 
/// Provides configurable max attempts, delays, and custom retry conditions.
/// Used throughout orchestration for resilient operation execution.
/// </summary>
public sealed class RetryPolicy
{
    private readonly ILogger<RetryPolicy>? _logger;
    private readonly RetryStrategy _strategy;
    private readonly int _maxAttempts;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _maxDelay;
    private readonly double _backoffMultiplier;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryPolicy"/> class.
    /// </summary>
    /// <param name="strategy">The retry strategy to use (default: ExponentialBackoff).</param>
    /// <param name="maxAttempts">Maximum number of retry attempts (default: 3).</param>
    /// <param name="initialDelay">Initial delay before first retry (default: 1 second).</param>
    /// <param name="maxDelay">Maximum delay between retries (default: 5 minutes).</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff (default: 2.0).</param>
    /// <param name="logger">Optional logger instance.</param>
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
    /// 
    /// Retries the operation up to MaxAttempts times if it fails.
    /// Uses the configured retry strategy to calculate delays between retries.
    /// If shouldRetry returns false for an exception, the exception is immediately rethrown.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="shouldRetry">Optional function to determine if an exception should be retried.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="Exception">Thrown if the operation fails after all retry attempts.</exception>
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
    /// 
    /// Convenience overload for operations that don't return a value.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="shouldRetry">Optional function to determine if an exception should be retried.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Calculates the delay before the next retry attempt.
    /// 
    /// Delay calculation based on strategy:
    /// - Fixed: Always returns initialDelay
    /// - Linear: initialDelay * attempt
    /// - ExponentialBackoff: initialDelay * (backoffMultiplier ^ (attempt - 1)), capped at maxDelay
    /// - JitteredExponentialBackoff: Same as ExponentialBackoff but with random jitter (0.5x to 1.0x)
    /// </summary>
    /// <param name="attempt">The current attempt number (1-based).</param>
    /// <returns>The delay before the next retry.</returns>
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

