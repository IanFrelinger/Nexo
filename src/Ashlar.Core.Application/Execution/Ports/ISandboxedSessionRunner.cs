namespace Ashlar.Core.Application.Execution.Ports;

/// <summary>
/// Starts long-lived isolated sessions (extension spec Part B): one session hosts many
/// commands against the same isolated state, unlike <see cref="ISandboxedCommandRunner"/>
/// where every command gets a fresh throwaway sandbox. A proposer session's builds, tests,
/// and probes all run inside one session so nothing escapes to the host between commands.
///
/// Concrete backends (e.g. a container engine) live in infrastructure; profiles only
/// supply <see cref="SandboxSpec"/>s. The spec's <c>Command</c> is the session's idle
/// keepalive process (backend-neutral — profiles supply it); work is submitted via
/// <see cref="ISandboxedSession.ExecAsync"/>.
/// </summary>
public interface ISandboxedSessionRunner
{
    /// <summary>
    /// Starts a session under the isolation policy of <paramref name="spec"/>.
    /// Fail-closed: a session that cannot be started is an exception, never a
    /// degraded-to-host fallback.
    /// </summary>
    Task<ISandboxedSession> StartAsync(SandboxSpec spec, CancellationToken cancellationToken = default);
}

/// <summary>
/// One live isolated session. Teardown is the disposal contract: <c>await using</c> the
/// session guarantees the isolated environment is destroyed when the harness scope exits,
/// even on exception paths. Backends additionally run a reaper for sessions whose host
/// process died before disposal could run.
/// </summary>
public interface ISandboxedSession : IAsyncDisposable
{
    /// <summary>Stable identifier for this session (used in provenance and teardown attribution).</summary>
    string SessionId { get; }

    /// <summary>
    /// Executes one command inside the running session and returns stdout/stderr/exit code.
    /// Throws <see cref="InvalidOperationException"/> after the session has been stopped —
    /// executing against a torn-down session must be loud, never silently re-created.
    /// </summary>
    Task<ProcessCommandResult> ExecAsync(IReadOnlyList<string> command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attests what the session's environment actually is (resolved image identity, engine
    /// version, effective limits). Fail-closed: a session whose environment cannot be
    /// attested throws — certification must never record an unverified environment.
    /// </summary>
    Task<SessionAttestation> AttestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the session and destroys its isolated environment. Idempotent; disposal calls
    /// this. Teardown failures are logged by backends but never thrown — a failed teardown
    /// must not mask the work's own result (the reaper is the backstop).
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
