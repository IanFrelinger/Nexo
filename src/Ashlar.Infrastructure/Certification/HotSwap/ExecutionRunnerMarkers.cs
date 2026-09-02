namespace Ashlar.Infrastructure.Certification.HotSwap;

/// <summary>
/// The markers the out-of-process execution runner stamps on its raw observations, and the
/// certification gate parses back out of them.
/// </summary>
/// <remarks>
/// Deliberately OUTSIDE the experimental <see cref="SessionExecutionBackend"/> surface: the
/// mutation engine is not part of the experimental autonomy contract and must be able to read
/// these without opting into it.
/// </remarks>
internal static class ExecutionRunnerMarkers
{
    /// <summary>
    /// The prefix stamped on every observation for a unit the runner could not LOAD at all.
    /// </summary>
    /// <remarks>
    /// A unit that fails to load reports every case as thrown, which is shape-identical to a
    /// mutant that throws on every case — and a judge reading only <c>Threw</c> scores both as
    /// killed. One is a witness doing its job; the other is a harness with nothing behind it, and
    /// letting it count is how a mutation leg reports a clean sweep it never ran. This marker is
    /// what lets the gate tell them apart, so the literal inside
    /// <c>SessionExecutionBackend.RunnerSource</c> must keep matching it —
    /// <c>SessionExecutionBackendTests</c> pins that.
    /// </remarks>
    public const string UnitLoadFailurePrefix = "unit load failed: ";
}
