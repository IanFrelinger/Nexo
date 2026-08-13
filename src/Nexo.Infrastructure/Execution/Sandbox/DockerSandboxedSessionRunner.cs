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
    private readonly ILogger<DockerSandboxedSessionRunner>? _logger;

    /// <summary>Creates a Docker-backed session runner.</summary>
    public DockerSandboxedSessionRunner(
        IProcessCommandRunner processRunner,
        TimeProvider? clock = null,
        ILogger<DockerSandboxedSessionRunner>? logger = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _clock = clock ?? TimeProvider.System;
        _logger = logger;
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

        return new DockerSandboxedSession(sessionId, _processRunner, _logger);
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

    private sealed class DockerSandboxedSession : ISandboxedSession
    {
        private readonly IProcessCommandRunner _processRunner;
        private readonly ILogger? _logger;
        private int _stopped;

        public DockerSandboxedSession(string sessionId, IProcessCommandRunner processRunner, ILogger? logger)
        {
            SessionId = sessionId;
            _processRunner = processRunner;
            _logger = logger;
        }

        public string SessionId { get; }

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
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Sandbox session '{SessionId}' teardown failed", SessionId);
            }
        }

        public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
    }
}
