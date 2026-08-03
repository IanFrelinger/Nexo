using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Resilience.Ports;

namespace Nexo.Infrastructure.Resilience;

/// <summary>
/// Default <see cref="IResilientExecutor"/>: exponential backoff from
/// <see cref="RetryPolicy.BaseDelay"/>, never retries when the caller's token
/// is cancelled.
/// </summary>
public sealed class ResilientExecutor : IResilientExecutor
{
    private readonly ILogger<ResilientExecutor>? _logger;

    /// <summary>Creates an executor with optional logging.</summary>
    public ResilientExecutor(ILogger<ResilientExecutor>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        RetryPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.MaxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(policy), policy.MaxAttempts, "MaxAttempts must be ≥ 1.");

        var isTransient = policy.ResolveIsTransient();
        Exception? last = null;

        for (var attempt = 1; attempt <= policy.MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                last = ex;

                // Non-negotiable: never retry when the caller's own token cancelled.
                if (cancellationToken.IsCancellationRequested)
                    throw;

                var canRetry = attempt < policy.MaxAttempts && isTransient(ex);
                if (!canRetry)
                    throw;

                var delay = TimeSpan.FromMilliseconds(
                    policy.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

                _logger?.LogWarning(
                    ex,
                    "Transient failure on attempt {Attempt}/{MaxAttempts}; retrying in {DelayMs}ms",
                    attempt,
                    policy.MaxAttempts,
                    delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw last ?? new InvalidOperationException("Operation failed with no exception captured.");
    }
}
