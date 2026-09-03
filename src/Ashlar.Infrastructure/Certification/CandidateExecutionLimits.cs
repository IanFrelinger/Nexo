using System.Globalization;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// The wall-clock and memory bounds the certification gate places on every execution of
/// candidate or mutant code when the request names no execution backend of its own.
/// </summary>
/// <remarks>
/// <para>Author code runs in three legs — correctness, determinism, mutation — and until these
/// limits existed nothing bounded any of them. A <c>shift-relational-boundary</c> mutant turning
/// <c>while (n &gt; 0)</c> into <c>while (n &gt;= 0)</c> hung the certifier forever on an honest
/// brick, and an honest recursive helper with one mutated literal took the whole process down
/// with an uncatchable stack overflow. The gate now replays author code in a child process
/// (<see cref="LocalProcessExecutionBackend"/>) under these bounds, and records what the wall
/// clock decided separately from what the witness decided.</para>
///
/// <para>Every value here is recorded on the certificate's gate passes
/// (<c>perCaseTimeoutMs=…</c>), so a reader of the record knows the budget the verdict was
/// reached under. The defaults are deliberately generous relative to a healthy brick — witness
/// cases are millisecond-scale — and deliberately tight relative to "forever".</para>
/// </remarks>
public sealed record CandidateExecutionLimits
{
    /// <summary>The gate's defaults.</summary>
    public static CandidateExecutionLimits Default { get; } = new();

    /// <summary>
    /// The budget for ONE <c>ExecuteAsync</c> invocation (one witness case, one repeat). A case
    /// that does not finish inside it is reported as timed out and the unit is abandoned.
    /// </summary>
    public TimeSpan PerCaseTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The cumulative budget for one unit — the candidate or one mutant — across all of its cases
    /// and repeats. Bounds a mutant that is slow on every case without being slow enough on any
    /// one of them to trip <see cref="PerCaseTimeout"/>.
    /// </summary>
    public TimeSpan PerUnitTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The budget for one whole execution job (the candidate, or every mutant of one
    /// certification). Exhausting it is a HARNESS fault, not a verdict: units that never ran were
    /// never observed, so the gate refuses rather than scoring them either way.
    /// </summary>
    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How much longer than <see cref="PerCaseTimeout"/> the certifier waits for the runner to
    /// report anything before concluding the runner itself is stuck and killing it. The runner
    /// enforces the per-case budget internally; this is the backstop for a runner that cannot —
    /// a brick that starved the thread pool, or hung the process in a way its own timer cannot see.
    /// </summary>
    public TimeSpan ProgressGrace { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// GC heap hard limit for the runner process. A mutant that allocates without bound hits this
    /// and dies with an out-of-memory abort (recorded as a crash) instead of paging the certifying
    /// host to a standstill first.
    /// </summary>
    public long HeapLimitBytes { get; init; } = 1L << 30;

    /// <summary>
    /// The limits as the gate-pass configuration fragment recorded on the certificate, so the
    /// budget a verdict was reached under is part of the signed record.
    /// </summary>
    public string Describe() => string.Create(CultureInfo.InvariantCulture,
        $"perCaseTimeoutMs={(long)PerCaseTimeout.TotalMilliseconds};perUnitTimeoutMs={(long)PerUnitTimeout.TotalMilliseconds};"
        + $"totalTimeoutMs={(long)TotalTimeout.TotalMilliseconds};heapLimitMb={HeapLimitBytes >> 20}");

    /// <summary>Refuses a configuration under which nothing could ever be observed.</summary>
    internal void Validate()
    {
        if (PerCaseTimeout <= TimeSpan.Zero || PerUnitTimeout <= TimeSpan.Zero || TotalTimeout <= TimeSpan.Zero
            || ProgressGrace < TimeSpan.Zero || HeapLimitBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CandidateExecutionLimits),
                "Every execution limit must be positive (the progress grace may be zero); a zero or negative budget "
                + "would report every execution as timed out and score a mutation leg that never ran.");
        }
    }
}
