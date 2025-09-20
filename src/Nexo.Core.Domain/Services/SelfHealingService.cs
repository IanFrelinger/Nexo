using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Domain.Services
{
    /// <summary>
    /// Service for self-healing with retry, backoff, circuit breaker, and failover
    /// </summary>
    public class SelfHealingService : ISelfHealingService
    {
        private readonly ILogger<SelfHealingService> _logger;
        private readonly Dictionary<string, CircuitBreakerState> _circuitBreakers;
        private readonly Dictionary<string, RetryState> _retryStates;
        private readonly List<IProvider> _providers;

        public SelfHealingService(ILogger<SelfHealingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _circuitBreakers = new Dictionary<string, CircuitBreakerState>();
            _retryStates = new Dictionary<string, RetryState>();
            _providers = new List<IProvider>();
        }

        public async Task<T> ExecuteWithSelfHealingAsync<T>(
            string operationId,
            Func<Task<T>> operation,
            SelfHealingOptions options,
            CancellationToken cancellationToken = default)
        {
            var retryState = GetOrCreateRetryState(operationId);
            var circuitBreaker = GetOrCreateCircuitBreaker(operationId);

            for (int attempt = 1; attempt <= options.MaxRetries; attempt++)
            {
                try
                {
                    // Check circuit breaker
                    if (circuitBreaker.State == CircuitBreakerStateType.Open)
                    {
                        if (DateTime.UtcNow - circuitBreaker.LastFailureTime < circuitBreaker.Timeout)
                        {
                            throw new CircuitBreakerOpenException($"Circuit breaker is open for operation {operationId}");
                        }
                        else
                        {
                            // Try to reset circuit breaker
                            circuitBreaker.State = CircuitBreakerStateType.HalfOpen;
                            _logger.LogInformation("Circuit breaker half-open for operation {OperationId}", operationId);
                        }
                    }

                    // Execute operation
                    var result = await operation();
                    
                    // Success - reset circuit breaker and retry state
                    circuitBreaker.State = CircuitBreakerStateType.Closed;
                    circuitBreaker.SuccessCount = 0;
                    circuitBreaker.FailureCount = 0;
                    retryState.Attempts = 0;
                    retryState.LastAttempt = DateTime.UtcNow;

                    _logger.LogDebug("Operation {OperationId} succeeded on attempt {Attempt}", operationId, attempt);
                    return result;
                }
                catch (Exception ex) when (IsRetryableException(ex))
                {
                    _logger.LogWarning(ex, "Operation {OperationId} failed on attempt {Attempt}: {Error}", 
                        operationId, attempt, ex.Message);

                    // Update retry state
                    retryState.Attempts = attempt;
                    retryState.LastAttempt = DateTime.UtcNow;
                    retryState.LastError = ex.Message;

                    // Update circuit breaker
                    circuitBreaker.FailureCount++;
                    circuitBreaker.LastFailureTime = DateTime.UtcNow;

                    if (circuitBreaker.FailureCount >= options.CircuitBreakerThreshold)
                    {
                        circuitBreaker.State = CircuitBreakerStateType.Open;
                        circuitBreaker.Timeout = TimeSpan.FromSeconds(Math.Pow(2, circuitBreaker.FailureCount));
                        _logger.LogWarning("Circuit breaker opened for operation {OperationId} after {Failures} failures", 
                            operationId, circuitBreaker.FailureCount);
                    }

                    // Check if we should retry
                    if (attempt >= options.MaxRetries)
                    {
                        _logger.LogError("Operation {OperationId} failed after {MaxRetries} attempts", 
                            operationId, options.MaxRetries);
                        throw;
                    }

                    // Calculate backoff delay
                    var delay = CalculateBackoffDelay(attempt, options);
                    _logger.LogDebug("Waiting {Delay}ms before retry {Attempt} for operation {OperationId}", 
                        delay.TotalMilliseconds, attempt + 1, operationId);

                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw new InvalidOperationException($"Operation {operationId} failed after all retry attempts");
        }

        public async Task<T> ExecuteWithFailoverAsync<T>(
            string operationId,
            Func<Task<T>> primaryOperation,
            Func<Task<T>> fallbackOperation,
            SelfHealingOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await ExecuteWithSelfHealingAsync(operationId, primaryOperation, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary operation failed for {OperationId}, attempting failover", operationId);
                
                try
                {
                    return await ExecuteWithSelfHealingAsync($"{operationId}_fallback", fallbackOperation, options, cancellationToken);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Both primary and fallback operations failed for {OperationId}", operationId);
                    throw new FailoverException("Both primary and fallback operations failed", ex, fallbackEx);
                }
            }
        }

        public async Task<T> ExecuteWithProviderFailoverAsync<T>(
            string operationId,
            List<IProvider> providers,
            Func<IProvider, Task<T>> operation,
            SelfHealingOptions options,
            CancellationToken cancellationToken = default)
        {
            var lastException = new Exception("No providers available");

            foreach (var provider in providers)
            {
                try
                {
                    _logger.LogDebug("Attempting operation {OperationId} with provider {ProviderName}", 
                        operationId, provider.Name);

                    return await ExecuteWithSelfHealingAsync(
                        $"{operationId}_{provider.Name}",
                        () => operation(provider),
                        options,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {ProviderName} failed for operation {OperationId}: {Error}", 
                        provider.Name, operationId, ex.Message);
                    lastException = ex;
                }
            }

            throw new FailoverException("All providers failed", lastException);
        }

        public async Task<SelfHealingMetrics> GetMetricsAsync(
            string operationId,
            CancellationToken cancellationToken = default)
        {
            var retryState = GetOrCreateRetryState(operationId);
            var circuitBreaker = GetOrCreateCircuitBreaker(operationId);

            return await Task.FromResult(new SelfHealingMetrics
            {
                OperationId = operationId,
                TotalAttempts = retryState.Attempts,
                LastAttempt = retryState.LastAttempt,
                LastError = retryState.LastError,
                CircuitBreakerState = circuitBreaker.State,
                CircuitBreakerFailures = circuitBreaker.FailureCount,
                CircuitBreakerSuccesses = circuitBreaker.SuccessCount,
                CircuitBreakerLastFailure = circuitBreaker.LastFailureTime
            });
        }

        private RetryState GetOrCreateRetryState(string operationId)
        {
            if (!_retryStates.TryGetValue(operationId, out var state))
            {
                state = new RetryState();
                _retryStates[operationId] = state;
            }
            return state;
        }

        private CircuitBreakerState GetOrCreateCircuitBreaker(string operationId)
        {
            if (!_circuitBreakers.TryGetValue(operationId, out var state))
            {
                state = new CircuitBreakerState();
                _circuitBreakers[operationId] = state;
            }
            return state;
        }

        private bool IsRetryableException(Exception ex)
        {
            return ex is HttpRequestException ||
                   ex is TaskCanceledException ||
                   ex is TimeoutException ||
                   ex.Message.Contains("429") || // Too Many Requests
                   ex.Message.Contains("503") || // Service Unavailable
                   ex.Message.Contains("502");   // Bad Gateway
        }

        private TimeSpan CalculateBackoffDelay(int attempt, SelfHealingOptions options)
        {
            switch (options.BackoffStrategy)
            {
                case BackoffStrategy.Linear:
                    return TimeSpan.FromMilliseconds(options.BaseDelayMs * attempt);
                
                case BackoffStrategy.Exponential:
                    return TimeSpan.FromMilliseconds(options.BaseDelayMs * Math.Pow(2, attempt - 1));
                
                case BackoffStrategy.Fixed:
                default:
                    return TimeSpan.FromMilliseconds(options.BaseDelayMs);
            }
        }
    }

    /// <summary>
    /// Self-healing options
    /// </summary>
    public class SelfHealingOptions
    {
        public int MaxRetries { get; set; } = 3;
        public int BaseDelayMs { get; set; } = 1000;
        public BackoffStrategy BackoffStrategy { get; set; } = BackoffStrategy.Exponential;
        public int CircuitBreakerThreshold { get; set; } = 5;
        public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Backoff strategy
    /// </summary>
    public enum BackoffStrategy
    {
        Fixed,
        Linear,
        Exponential
    }

    /// <summary>
    /// Circuit breaker state
    /// </summary>
    public class CircuitBreakerState
    {
        public CircuitBreakerStateType State { get; set; } = CircuitBreakerStateType.Closed;
        public int FailureCount { get; set; }
        public int SuccessCount { get; set; }
        public DateTime LastFailureTime { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Circuit breaker state type
    /// </summary>
    public enum CircuitBreakerStateType
    {
        Closed,
        Open,
        HalfOpen
    }

    /// <summary>
    /// Retry state
    /// </summary>
    public class RetryState
    {
        public int Attempts { get; set; }
        public DateTime LastAttempt { get; set; }
        public string? LastError { get; set; }
    }

    /// <summary>
    /// Provider interface
    /// </summary>
    public interface IProvider
    {
        string Name { get; }
        bool IsHealthy { get; }
        int Priority { get; }
    }

    /// <summary>
    /// Self-healing metrics
    /// </summary>
    public class SelfHealingMetrics
    {
        public string OperationId { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public DateTime LastAttempt { get; set; }
        public string? LastError { get; set; }
        public CircuitBreakerStateType CircuitBreakerState { get; set; }
        public int CircuitBreakerFailures { get; set; }
        public int CircuitBreakerSuccesses { get; set; }
        public DateTime CircuitBreakerLastFailure { get; set; }
    }

    /// <summary>
    /// Circuit breaker open exception
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }

    /// <summary>
    /// Failover exception
    /// </summary>
    public class FailoverException : Exception
    {
        public Exception PrimaryException { get; }
        public Exception? FallbackException { get; }

        public FailoverException(string message, Exception primaryException, Exception? fallbackException = null) 
            : base(message)
        {
            PrimaryException = primaryException;
            FallbackException = fallbackException;
        }
    }

    /// <summary>
    /// Interface for self-healing service
    /// </summary>
    public interface ISelfHealingService
    {
        Task<T> ExecuteWithSelfHealingAsync<T>(
            string operationId,
            Func<Task<T>> operation,
            SelfHealingOptions options,
            CancellationToken cancellationToken = default);

        Task<T> ExecuteWithFailoverAsync<T>(
            string operationId,
            Func<Task<T>> primaryOperation,
            Func<Task<T>> fallbackOperation,
            SelfHealingOptions options,
            CancellationToken cancellationToken = default);

        Task<T> ExecuteWithProviderFailoverAsync<T>(
            string operationId,
            List<IProvider> providers,
            Func<IProvider, Task<T>> operation,
            SelfHealingOptions options,
            CancellationToken cancellationToken = default);

        Task<SelfHealingMetrics> GetMetricsAsync(
            string operationId,
            CancellationToken cancellationToken = default);
    }
}
