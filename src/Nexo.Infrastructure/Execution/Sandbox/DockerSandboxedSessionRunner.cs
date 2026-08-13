using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Scaling;

namespace Nexo.Infrastructure.Execution.Sandbox;

/// <summary>
/// Container-engine backend for <see cref="ISandboxedSessionRunner"/> (extension spec
/// Part B): <c>docker run -d</c> starts a named keepalive container, work is submitted
/// with <c>docker exec</c>, and teardown is <c>docker rm -f</c>.
///
/// Every session container carries <see cref="SessionLabel"/> plus a
/// <see cref="DeadlineLabelKey"/> label (unix seconds) computed from the spec's wall-clock
/// timeout — the labels are what <see cref="DockerSandboxSessionReaper"/> sweeps on, so
/// a session leaked by a crashed host process still dies at its deadline.
/// </summary>
public sealed class DockerSandboxedSessionRunner : ISandboxedSessionRunner
{
    /// <summary>Label marking a container as a Nexo sandbox session (reaper filter key).</summary>
    public const string SessionLabel = "nexo.session=1";

    /// <summary>Label key carrying the session's hard deadline in unix seconds.</summary>
    public const string DeadlineLabelKey = "nexo.session.deadline";

    /// <summary>Container-name prefix for session containers.</summary>
    public const string NamePrefix = "nexo-session-";

    /// <summary>
    /// Session lifetime applied when the spec declares no wall-clock timeout. Sessions are
    /// never immortal: an unlabeled or unbounded session is indistinguishable from a leak.
    /// </summary>
    public static readonly TimeSpan DefaultSessionLifetime = TimeSpan.FromMinutes(30);

    private readonly IProcessCommandRunner _processRunner;
    private readonly TimeProvider _clock;
    private readonly ISessionProvenanceSink? _provenance;
    private readonly ILogger<DockerSandboxedSessionRunner>? _logger;

    /// <summary>Creates a Docker-backed session runner.</summary>
    public DockerSandboxedSessionRunner(
        IProcessCommandRunner processRunner,
        TimeProvider? clock = null,
        ILogger<DockerSandboxedSessionRunner>? logger = null,
        ISessionProvenanceSink? provenance = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _clock = clock ?? TimeProvider.System;
        _logger = logger;
        _provenance = provenance;
    }

    /// <inheritdoc />
    public async Task<ISandboxedSession> StartAsync(SandboxSpec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var sessionId = $"{NamePrefix}{Guid.NewGuid():N}";
        var deadline = _clock.GetUtcNow() + (spec.Limits?.Timeout is { } t && t > TimeSpan.Zero ? t : DefaultSessionLifetime);
        var args = BuildStartArguments(spec, sessionId, deadline.ToUnixTimeSeconds());

        _logger?.LogDebug("Sandbox session start via docker: {Args}", string.Join(' ', args));
        var result = await _processRunner.RunAsync("docker", args, cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            // Fail-closed: no session means no work happens — never fall back to the host.
            throw new InvalidOperationException(
                $"Sandbox session '{sessionId}' failed to start (exit {result.ExitCode}): {result.StdErr}");
        }

        RecordProvenance(new SessionProvenanceEvent
        {
            SessionId = sessionId,
            Outcome = SessionProvenanceOutcomes.Started,
            Timestamp = _clock.GetUtcNow(),
            Image = spec.Image,
        });

        return new DockerSandboxedSession(sessionId, spec, _processRunner, _clock, RecordProvenance, _logger);
    }

    private void RecordProvenance(SessionProvenanceEvent provenanceEvent)
    {
        if (_provenance is null)
            return;

        try
        {
            _provenance.Record(provenanceEvent);
        }
        catch (Exception ex)
        {
            // Contract mirror of the hot-swap sinks: a provenance sink failure must never
            // fail a session operation.
            _logger?.LogWarning(ex, "Session provenance sink threw; the session operation proceeds");
        }
    }

    /// <summary>
    /// Builds the <c>docker run -d …</c> argv for a session container. Exposed for unit
    /// tests; not a public contracts surface. <c>--rm</c> is belt-and-braces: if the
    /// keepalive process exits on its own, the engine removes the container without
    /// waiting for teardown or the reaper.
    /// </summary>
    public static IReadOnlyList<string> BuildStartArguments(SandboxSpec spec, string sessionId, long deadlineUnixSeconds)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(spec.Image))
            throw new ArgumentException("SandboxSpec.Image is required for the Docker backend.", nameof(spec));
        if (spec.Command is null || spec.Command.Count == 0)
            throw new ArgumentException(
                "SandboxSpec.Command must be non-empty: sessions need a keepalive process.", nameof(spec));

        var args = new List<string>
        {
            "run",
            "-d",
            "--rm",
            "--name",
            sessionId,
            "--label",
            SessionLabel,
            "--label",
            $"{DeadlineLabelKey}={deadlineUnixSeconds}"
        };

        DockerSandboxedCommandRunner.AppendSpecArguments(args, spec);

        args.Add(spec.Image!);
        args.AddRange(spec.Command);
        return args;
    }

    /// <summary>Builds the <c>docker exec …</c> argv for one command inside a session.</summary>
    public static IReadOnlyList<string> BuildExecArguments(string sessionId, IReadOnlyList<string> command)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        if (command is null || command.Count == 0)
            throw new ArgumentException("Command must be non-empty.", nameof(command));

        var args = new List<string> { "exec", sessionId };
        args.AddRange(command);
        return args;
    }

    /// <summary>
    /// The <c>docker inspect</c> format string attestation reads: image identity plus the
    /// effective memory/pids/cpu caps, tab-separated.
    /// </summary>
    public const string InspectFormat =
        "{{.Image}}\t{{.HostConfig.Memory}}\t{{.HostConfig.PidsLimit}}\t{{.HostConfig.NanoCpus}}";

    /// <summary>
    /// Parses the <see cref="InspectFormat"/> output line. Unparsable numeric fields
    /// (docker renders unset values as <c>&lt;nil&gt;</c>) come back null — which the
    /// attestation's shortfall check treats as unverified, fail-closed.
    /// </summary>
    public static (string? ImageDigest, long? MemoryBytes, long? PidsLimit, long? NanoCpus) ParseInspectLine(string line)
    {
        var parts = (line ?? "").Trim().Split('\t');
        var digest = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0].Trim() : null;
        return (
            digest,
            parts.Length > 1 && long.TryParse(parts[1].Trim(), out var mem) ? mem : null,
            parts.Length > 2 && long.TryParse(parts[2].Trim(), out var pids) ? pids : null,
            parts.Length > 3 && long.TryParse(parts[3].Trim(), out var nano) ? nano : null);
    }

    private sealed class DockerSandboxedSession : ISandboxedSession
    {
        private readonly SandboxSpec _spec;
        private readonly IProcessCommandRunner _processRunner;
        private readonly TimeProvider _clock;
        private readonly Action<SessionProvenanceEvent> _recordProvenance;
        private readonly ILogger? _logger;
        private int _stopped;

        public DockerSandboxedSession(
            string sessionId,
            SandboxSpec spec,
            IProcessCommandRunner processRunner,
            TimeProvider clock,
            Action<SessionProvenanceEvent> recordProvenance,
            ILogger? logger)
        {
            SessionId = sessionId;
            _spec = spec;
            _processRunner = processRunner;
            _clock = clock;
            _recordProvenance = recordProvenance;
            _logger = logger;
        }

        public string SessionId { get; }

        public async Task<SessionAttestation> AttestAsync(CancellationToken cancellationToken = default)
        {
            var inspect = await _processRunner
                .RunAsync("docker", new[] { "inspect", SessionId, "--format", InspectFormat }, cancellationToken)
                .ConfigureAwait(false);
            if (!inspect.Succeeded)
            {
                // Fail-closed: certification must never record an unverified environment.
                throw new InvalidOperationException(
                    $"Sandbox session '{SessionId}' cannot be attested (inspect exit {inspect.ExitCode}): {inspect.StdErr}");
            }

            var version = await _processRunner
                .RunAsync("docker", new[] { "version", "--format", "{{.Server.Version}}" }, cancellationToken)
                .ConfigureAwait(false);

            var (digest, memory, pids, nano) = ParseInspectLine(inspect.StdOut);
            var attestation = new SessionAttestation
            {
                SessionId = SessionId,
                Image = _spec.Image ?? "",
                ImageDigest = digest,
                EngineVersion = version.Succeeded ? version.StdOut.Trim() : null,
                Requested = _spec.Limits ?? new ResourceLimits(),
                EffectiveMemoryBytes = memory,
                EffectivePidsLimit = pids,
                EffectiveNanoCpus = nano,
                AttestedAt = _clock.GetUtcNow(),
            };

            _recordProvenance(new SessionProvenanceEvent
            {
                SessionId = SessionId,
                Outcome = SessionProvenanceOutcomes.Attested,
                Timestamp = attestation.AttestedAt,
                Image = attestation.Image,
                ImageDigest = attestation.ImageDigest,
            });

            return attestation;
        }

        public async Task<ProcessCommandResult> ExecAsync(
            IReadOnlyList<string> command,
            CancellationToken cancellationToken = default)
        {
            if (Volatile.Read(ref _stopped) != 0)
            {
                throw new InvalidOperationException(
                    $"Sandbox session '{SessionId}' has been stopped; executing against a torn-down "
                    + "session must be loud, never silently re-created.");
            }

            var args = BuildExecArguments(SessionId, command);
            return await _processRunner.RunAsync("docker", args, cancellationToken).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _stopped, 1) != 0)
                return;

            try
            {
                var result = await _processRunner
                    .RunAsync("docker", new[] { "rm", "-f", SessionId }, cancellationToken)
                    .ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    // Never throw: teardown failure must not mask the work's own result.
                    // The deadline label + reaper are the backstop for whatever survived.
                    _logger?.LogWarning(
                        "Sandbox session '{SessionId}' teardown returned exit {ExitCode}: {StdErr}",
                        SessionId, result.ExitCode, result.StdErr);
                }

                _recordProvenance(new SessionProvenanceEvent
                {
                    SessionId = SessionId,
                    Outcome = result.Succeeded
                        ? SessionProvenanceOutcomes.Stopped
                        : SessionProvenanceOutcomes.TeardownFailed,
                    Timestamp = _clock.GetUtcNow(),
                    Image = _spec.Image,
                    FailureCode = result.Succeeded ? null : $"exit-{result.ExitCode}",
                    Reason = result.Succeeded ? null : result.StdErr,
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Sandbox session '{SessionId}' teardown failed", SessionId);
                _recordProvenance(new SessionProvenanceEvent
                {
                    SessionId = SessionId,
                    Outcome = SessionProvenanceOutcomes.TeardownFailed,
                    Timestamp = _clock.GetUtcNow(),
                    Image = _spec.Image,
                    FailureCode = "exception",
                    Reason = ex.Message,
                });
            }
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
    }
}
