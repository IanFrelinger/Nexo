namespace Ashlar.Core.Application.Resilience.Ports;

/// <summary>
/// How the delay between attempts grows. Absorbed from the second, now-deleted
/// <c>Ashlar.Orchestration.Resilience.RetryStrategy</c> so there is one backoff
/// vocabulary in the codebase.
/// </summary>
public enum RetryBackoff
{
    /// <summary>Constant <see cref="RetryPolicy.BaseDelay"/> between attempts.</summary>
    Fixed,

    /// <summary><c>BaseDelay * attempt</c>.</summary>
    Linear,

    /// <summary><c>BaseDelay * BackoffMultiplier^(attempt-1)</c>. The default.</summary>
    Exponential,

    /// <summary>
    /// Exponential scaled by a random factor in [0.5, 1.0). Spreads a thundering
    /// herd of clients that all failed at the same instant.
    /// </summary>
    JitteredExponential
}

/// <summary>
/// Options for <see cref="IResilientExecutor"/>: attempt bound, backoff shape,
/// and a pluggable transient classifier.
///
/// This is the single retry policy type. It also owns the single backoff
/// calculation, via <see cref="DelayForAttempt"/> — call sites whose retry loop
/// genuinely cannot be an <see cref="IResilientExecutor"/> call (result-flag
/// retries, delay advisors) still take their delays from here rather than
/// re-deriving the maths.
/// </summary>
/// <param name="MaxAttempts">Maximum attempts including the first try. Must be ≥ 1.</param>
/// <param name="BaseDelay">Base delay feeding <see cref="DelayForAttempt"/>.</param>
/// <param name="IsTransient">
/// Returns true when the failure may be retried. When null,
/// <see cref="TransientClassifiers.Network"/> is used.
/// </param>
/// <param name="Backoff">Backoff shape. Defaults to <see cref="RetryBackoff.Exponential"/>.</param>
/// <param name="BackoffMultiplier">Growth factor for exponential shapes. Defaults to 2.0.</param>
/// <param name="MaxDelay">
/// Upper bound on a computed delay. Null means uncapped, which is the historical
/// behaviour of the executor and is what keeps <see cref="Default"/> unchanged.
/// </param>
public sealed record RetryPolicy(
    int MaxAttempts,
    TimeSpan BaseDelay,
    Func<Exception, bool>? IsTransient = null,
    RetryBackoff Backoff = RetryBackoff.Exponential,
    double BackoffMultiplier = 2.0,
    TimeSpan? MaxDelay = null)
{
    /// <summary>Resolved classifier (defaults to <see cref="TransientClassifiers.Network"/>).</summary>
    public Func<Exception, bool> ResolveIsTransient() => IsTransient ?? TransientClassifiers.Network;

    /// <summary>Sensible default: 3 attempts, 1s base delay, exponential, network classifier.</summary>
    public static RetryPolicy Default { get; } = new(3, TimeSpan.FromSeconds(1));

    /// <summary>
    /// A constant-delay policy — the shape a stage runner or transport wants when
    /// it is pacing retries rather than backing off a struggling dependency.
    /// </summary>
    /// <param name="maxAttempts">Attempt bound including the first try.</param>
    /// <param name="delay">Constant delay between attempts.</param>
    /// <param name="isTransient">Optional classifier.</param>
    public static RetryPolicy FixedDelay(
        int maxAttempts,
        TimeSpan delay,
        Func<Exception, bool>? isTransient = null) =>
        new(maxAttempts, delay, isTransient, RetryBackoff.Fixed);

    /// <summary>
    /// The delay to wait after <paramref name="attempt"/> failed, before the next
    /// attempt. <paramref name="attempt"/> is 1-based, so the wait after the first
    /// failure is <c>DelayForAttempt(1)</c>.
    ///
    /// This is the one place backoff is computed. Exponential with the default
    /// multiplier of 2 and no cap reproduces exactly what the executor did inline
    /// before the two policy types were merged.
    /// </summary>
    /// <param name="attempt">1-based number of the attempt that just failed.</param>
    public TimeSpan DelayForAttempt(int attempt)
    {
        if (attempt < 1) throw new ArgumentOutOfRangeException(nameof(attempt), attempt, "Attempt is 1-based.");

        var baseMs = BaseDelay.TotalMilliseconds;
        var ms = Backoff switch
        {
            RetryBackoff.Fixed => baseMs,
            RetryBackoff.Linear => baseMs * attempt,
            RetryBackoff.Exponential => baseMs * Math.Pow(BackoffMultiplier, attempt - 1),
            RetryBackoff.JitteredExponential =>
                baseMs * Math.Pow(BackoffMultiplier, attempt - 1) * (0.5 + NextJitter() * 0.5),
            _ => baseMs
        };

        if (MaxDelay is { } cap)
            ms = Math.Min(ms, cap.TotalMilliseconds);

        return TimeSpan.FromMilliseconds(ms);
    }

    // Random.Shared is net6+, and this type is compiled for netstandard2.0 too --
    // the jitter it came from lived in a net8-only project. A per-thread instance
    // is the portable equivalent and also avoids the torn state a single shared
    // Random gives you under concurrency.
    [ThreadStatic]
    private static Random? _jitter;

    private static double NextJitter() =>
        (_jitter ??= new Random(Environment.TickCount ^ Environment.CurrentManagedThreadId)).NextDouble();
}
