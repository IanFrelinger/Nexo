namespace Nexo.Core.Application.Resilience.Ports;

/// <summary>
/// Domain-neutral resilient execution port: run an async operation with a
/// bounded retry policy. Never retries when the caller's
/// <see cref="CancellationToken"/> is already cancelled.
/// </summary>
public interface IResilientExecutor
{
    /// <summary>
    /// Executes <paramref name="operation"/>, retrying when
    /// <see cref="RetryPolicy.IsTransient"/> (or
    /// <see cref="TransientClassifiers.Network"/>) reports a transient failure,
    /// up to <see cref="RetryPolicy.MaxAttempts"/>.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="operation">Work to execute; receives the caller's token.</param>
    /// <param name="policy">Retry bound, delay, and classifier.</param>
    /// <param name="cancellationToken">Caller cancellation; never retried when requested.</param>
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        RetryPolicy policy,
        CancellationToken cancellationToken = default);
}
