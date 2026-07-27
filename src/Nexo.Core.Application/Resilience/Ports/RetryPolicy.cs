namespace Nexo.Core.Application.Resilience.Ports;

/// <summary>
/// Options for <see cref="IResilientExecutor"/>: attempt bound, base delay,
/// and a pluggable transient classifier.
/// </summary>
/// <param name="MaxAttempts">Maximum attempts including the first try. Must be ≥ 1.</param>
/// <param name="BaseDelay">
/// Base delay used for exponential backoff between attempts
/// (<c>BaseDelay * 2^(attempt-1)</c> after each failed attempt).
/// </param>
/// <param name="IsTransient">
/// Returns true when the failure may be retried. When null,
/// <see cref="TransientClassifiers.Network"/> is used.
/// </param>
public sealed record RetryPolicy(
    int MaxAttempts,
    TimeSpan BaseDelay,
    Func<Exception, bool>? IsTransient = null)
{
    /// <summary>Resolved classifier (defaults to <see cref="TransientClassifiers.Network"/>).</summary>
    public Func<Exception, bool> ResolveIsTransient() => IsTransient ?? TransientClassifiers.Network;

    /// <summary>Sensible default: 3 attempts, 1s base delay, network classifier.</summary>
    public static RetryPolicy Default { get; } = new(3, TimeSpan.FromSeconds(1));
}
