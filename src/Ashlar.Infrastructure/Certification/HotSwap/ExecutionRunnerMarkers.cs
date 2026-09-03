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

    /// <summary>
    /// The prefix stamped on every observation the WALL CLOCK decided: a case that did not
    /// finish inside its budget, or a unit killed by the certifier for making no progress.
    /// </summary>
    /// <remarks>
    /// A timed-out mutant is dead — it can never certify — but the witness did not catch it, and a
    /// certificate that lists it under <c>killedMutants</c> claims teeth the witness never showed.
    /// The mutation engine reads this prefix to file the kill under <c>timedOutMutants</c> instead,
    /// and the correctness leg reads it to say "timed out" rather than "threw". Both runners (the
    /// local replay runner and the in-session runner) emit it — the session runner's literal
    /// <c>"execution timed out after Ns"</c> begins with it by construction.
    /// </remarks>
    public const string ExecutionTimeoutPrefix = "execution timed out";

    /// <summary>
    /// The prefix the LOCAL replay backend stamps on every slot of a unit whose child process
    /// died while — or right after — executing it: a stack overflow, <c>Environment.Exit</c>,
    /// <c>Environment.FailFast</c>, an unhandled exception on a background thread, an
    /// out-of-memory abort.
    /// </summary>
    /// <remarks>
    /// The runner cannot report these itself (there is no process left to report from), so the
    /// certifier synthesises the observation from the exit code and the stderr tail. Filed under
    /// <c>crashedMutants</c>, never <c>killedMutants</c>, for the same reason as the timeout prefix.
    /// </remarks>
    public const string RunnerCrashPrefix = "execution crashed the runner";
}
