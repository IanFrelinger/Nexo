using Ashlar.Core.Application.Execution.Ports;

namespace Ashlar.Agents.TestKit;

/// <summary>
/// Wraps the SAME <see cref="ISandboxedSessionRunner"/> port production uses, with a
/// scripted run of exec results. Lets a test prove a harness starts its session, runs
/// its commands inside it, and tears it down on every exit path — with no container
/// anywhere in sight.
/// </summary>
public sealed class FakeSandboxedSessionRunner : ISandboxedSessionRunner
{
    private readonly Scripted<ProcessCommandResult> _execResults;

    /// <summary>Creates a runner whose sessions serve execs from a scripted sequence.</summary>
    public FakeSandboxedSessionRunner(Scripted<ProcessCommandResult> execResults) =>
        _execResults = execResults ?? throw new ArgumentNullException(nameof(execResults));

    /// <summary>Creates a runner whose sessions serve execs from an explicit run of results.</summary>
    public FakeSandboxedSessionRunner(params ProcessCommandResult[] execResults)
        : this(new Scripted<ProcessCommandResult>(execResults)) { }

    /// <summary>Every session started, in order — spec, exec log, and teardown state.</summary>
    public List<FakeSandboxedSession> Sessions { get; } = new();

    /// <summary>Sessions started and not yet stopped. A nonzero value at test end is a teardown leak.</summary>
    public int ActiveSessions => Sessions.Count(s => !s.Stopped);

    /// <inheritdoc />
    public Task<ISandboxedSession> StartAsync(SandboxSpec spec, CancellationToken cancellationToken = default)
    {
        var session = new FakeSandboxedSession($"fake-session-{Sessions.Count + 1}", spec, _execResults);
        Sessions.Add(session);
        return Task.FromResult<ISandboxedSession>(session);
    }

    /// <summary>A failing exec result carrying a canned diagnostic log.</summary>
    public static ProcessCommandResult Failure(string log, int exitCode = 1) => new(exitCode, "", log);

    /// <summary>A successful exec result.</summary>
    public static ProcessCommandResult Success(string stdout = "") => new(0, stdout, "");
}

/// <summary>One fake session: records exec commands and enforces the teardown contract.</summary>
public sealed class FakeSandboxedSession : ISandboxedSession
{
    private readonly Scripted<ProcessCommandResult> _execResults;

    internal FakeSandboxedSession(string sessionId, SandboxSpec spec, Scripted<ProcessCommandResult> execResults)
    {
        SessionId = sessionId;
        Spec = spec;
        _execResults = execResults;
    }

    /// <inheritdoc />
    public string SessionId { get; }

    /// <summary>The spec this session was started with.</summary>
    public SandboxSpec Spec { get; }

    /// <summary>Every command executed in this session, in order.</summary>
    public List<IReadOnlyList<string>> ExecCommands { get; } = new();

    /// <summary>Whether the session has been stopped (via <see cref="StopAsync"/> or disposal).</summary>
    public bool Stopped { get; private set; }

    /// <summary>
    /// Attestation returned by <see cref="AttestAsync"/>. Defaults to a faithful environment
    /// (digest resolved, requested limits echoed as effective — zero shortfalls); tests
    /// override it to script a weaker-than-requested environment.
    /// </summary>
    public SessionAttestation? AttestationOverride { get; set; }

    /// <inheritdoc />
    public Task<ProcessCommandResult> ExecAsync(
        IReadOnlyList<string> command,
        CancellationToken cancellationToken = default)
    {
        if (Stopped)
        {
            // Same teeth as the real backend: use-after-teardown is a harness bug and
            // must surface as one, never as a quietly re-created sandbox.
            throw new InvalidOperationException(
                $"Fake sandbox session '{SessionId}' has been stopped; exec after teardown is a harness bug.");
        }

        ExecCommands.Add(command);
        return Task.FromResult(_execResults.Next());
    }

    /// <inheritdoc />
    public Task<SessionAttestation> AttestAsync(CancellationToken cancellationToken = default)
    {
        if (Stopped)
            throw new InvalidOperationException($"Fake sandbox session '{SessionId}' has been stopped.");

        var attestation = AttestationOverride ?? new SessionAttestation
        {
            SessionId = SessionId,
            Image = Spec.Image ?? "",
            ImageDigest = $"sha256:fake-{SessionId}",
            EngineVersion = "fake-engine",
            Requested = Spec.Limits ?? new ResourceLimits(),
            EffectiveMemoryBytes = Spec.Limits?.Memory is { } m ? SessionAttestation.ParseMemoryToBytes(m) : null,
            EffectivePidsLimit = Spec.Limits?.Pids,
            EffectiveNanoCpus = Spec.Limits?.Cpus is { } c ? SessionAttestation.ParseCpusToNano(c) : null,
            AttestedAt = DateTimeOffset.UtcNow,
        };
        return Task.FromResult(attestation);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Stopped = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}
