using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.Resilience;

/// <summary>
/// Circuit breaker pattern implementation for resilient service calls.
/// </summary>
public sealed class CircuitBreaker
{
    private readonly string _name;
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private readonly ILogger<CircuitBreaker>? _logger;
    private readonly ConcurrentDictionary<string, CircuitState> _circuits = new();

    public CircuitBreaker(
        string name,
        int failureThreshold = 5,
        TimeSpan? timeout = null,
        ILogger<CircuitBreaker>? logger = null)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _failureThreshold = failureThreshold;
        _timeout = timeout ?? TimeSpan.FromMinutes(1);
        _logger = logger;
    }

    /// <summary>
    /// Executes an operation with circuit breaker protection.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        string operationKey,
        Func<Task<T>> operation,
        Func<Exception, T>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        var state = GetOrCreateState(operationKey);

        // Check circuit state with lock
        lock (state.SyncLock)
        {
            if (state.State == CircuitStateType.Open)
            {
                if (state.LastFailureTime.HasValue && DateTimeOffset.UtcNow - state.LastFailureTime.Value < _timeout)
                {
                    _logger?.LogWarning("Circuit breaker {Name} is OPEN for {Operation}. Using fallback.", 
                        _name, operationKey);
                    
                    if (fallback != null)
                    {
                        try
                        {
                            return fallback(new CircuitBreakerOpenException(
                                $"Circuit breaker {_name} is open for operation {operationKey}"));
                        }
                        catch
                        {
                            throw new CircuitBreakerOpenException(
                                $"Circuit breaker {_name} is open for operation {operationKey}");
                        }
                    }
                    throw new CircuitBreakerOpenException(
                        $"Circuit breaker {_name} is open for operation {operationKey}");
                }
                else
                {
                    // Timeout expired, attempt half-open
                    state.State = CircuitStateType.HalfOpen;
                    state.ConsecutiveSuccesses = 0;
                    _logger?.LogInformation(
                        "Circuit breaker {Name} transitioning to HALF-OPEN for {Operation}", 
                        _name, operationKey);
                }
            }
        }

        try
        {
            var result = await operation();

            // Success - update state with lock
            lock (state.SyncLock)
            {
                state.ConsecutiveFailures = 0;
                state.LastSuccessTime = DateTimeOffset.UtcNow;

                if (state.State == CircuitStateType.HalfOpen)
                {
                    state.ConsecutiveSuccesses++;
                    if (state.ConsecutiveSuccesses >= 2)
                    {
                        state.State = CircuitStateType.Closed;
                        _logger?.LogInformation(
                            "Circuit breaker {Name} transitioning to CLOSED for {Operation}", 
                            _name, operationKey);
                    }
                }
                else if (state.State != CircuitStateType.Closed)
                {
                    state.State = CircuitStateType.Closed;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            // Failure - update state with lock
            lock (state.SyncLock)
            {
                state.ConsecutiveFailures++;
                state.LastFailureTime = DateTimeOffset.UtcNow;
                state.LastError = ex;

                if (state.ConsecutiveFailures >= _failureThreshold)
                {
                    state.State = CircuitStateType.Open;
                    _logger?.LogError(ex, 
                        "Circuit breaker {Name} opened for {Operation} after {Failures} failures",
                        _name, operationKey, state.ConsecutiveFailures);
                }
            }

            // Try fallback if available
            if (fallback != null)
            {
                try
                {
                    return fallback(ex);
                }
                catch
                {
                    // Fallback failed, rethrow original
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Gets the current state of a circuit.
    /// </summary>
    public CircuitStateType GetState(string operationKey)
    {
        return GetOrCreateState(operationKey).State;
    }

    /// <summary>
    /// Manually opens a circuit.
    /// </summary>
    public void Open(string operationKey)
    {
        var state = GetOrCreateState(operationKey);
        lock (state.SyncLock)
        {
            state.State = CircuitStateType.Open;
            state.LastFailureTime = DateTimeOffset.UtcNow;
        }
        _logger?.LogWarning("Circuit breaker {Name} manually opened for {Operation}", _name, operationKey);
    }

    /// <summary>
    /// Manually closes a circuit.
    /// </summary>
    public void Close(string operationKey)
    {
        var state = GetOrCreateState(operationKey);
        lock (state.SyncLock)
        {
            state.State = CircuitStateType.Closed;
            state.ConsecutiveFailures = 0;
            state.ConsecutiveSuccesses = 0;
        }
        _logger?.LogInformation("Circuit breaker {Name} manually closed for {Operation}", _name, operationKey);
    }

    /// <summary>
    /// Resets all circuits.
    /// </summary>
    public void Reset()
    {
        _circuits.Clear();
        _logger?.LogInformation("Circuit breaker {Name} reset", _name);
    }

    private CircuitState GetOrCreateState(string operationKey)
    {
        return _circuits.GetOrAdd(operationKey, _ => new CircuitState
        {
            OperationKey = operationKey,
            State = CircuitStateType.Closed
        });
    }
}

/// <summary>
/// State of a circuit breaker.
/// </summary>
internal sealed class CircuitState
{
    public required string OperationKey { get; init; }
    public CircuitStateType State { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int ConsecutiveSuccesses { get; set; }
    public DateTimeOffset? LastFailureTime { get; set; }
    public DateTimeOffset? LastSuccessTime { get; set; }
    public Exception? LastError { get; set; }

    // Lock for thread-safe state transitions
    public readonly object SyncLock = new();
}

/// <summary>
/// State of a circuit breaker.
/// </summary>
public enum CircuitStateType
{
    Closed,   // Normal operation
    Open,     // Failing, reject requests
    HalfOpen  // Testing if service recovered
}

/// <summary>
/// Exception thrown when circuit breaker is open.
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}

