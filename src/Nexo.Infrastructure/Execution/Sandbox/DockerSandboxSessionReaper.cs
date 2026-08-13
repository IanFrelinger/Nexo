using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Scaling;

namespace Nexo.Infrastructure.Execution.Sandbox;

/// <summary>
/// Sweeps leaked sandbox session containers (extension spec Part B teardown guarantee):
/// <see cref="DockerSandboxedSessionRunner"/> guarantees teardown on every disposal path
/// inside a live host process, but a host that crashes or is killed leaves its sessions
/// running. The reaper lists containers carrying the session label and force-removes any
/// whose deadline label has passed — or is missing/garbled, because a session container
/// without a readable deadline is indistinguishable from a leak and fail-closed means it
/// dies (its own harness would have re-created it if it were still wanted).
///
/// Hosts call <see cref="SweepAsync"/> periodically (e.g. from a hosted background
/// service); the class itself stays scheduler-free so tests drive it directly.
/// </summary>
public sealed class DockerSandboxSessionReaper
{
    private readonly IProcessCommandRunner _processRunner;
    private readonly TimeProvider _clock;
    private readonly ISessionProvenanceSink? _provenance;
    private readonly ILogger<DockerSandboxSessionReaper>? _logger;

    /// <summary>Creates the reaper.</summary>
    public DockerSandboxSessionReaper(
        IProcessCommandRunner processRunner,
        TimeProvider? clock = null,
        ILogger<DockerSandboxSessionReaper>? logger = null,
        ISessionProvenanceSink? provenance = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _clock = clock ?? TimeProvider.System;
        _logger = logger;
        _provenance = provenance;
    }

    /// <summary>
    /// The <c>docker ps</c> argv the sweep lists sessions with: one line per running
    /// session container, <c>name\tdeadline</c>.
    /// </summary>
    public static IReadOnlyList<string> ListArguments { get; } = new[]
    {
        "ps",
        "--filter",
        $"label={DockerSandboxedSessionRunner.SessionLabel}",
        "--format",
        "{{.Names}}\t{{.Label \"" + DockerSandboxedSessionRunner.DeadlineLabelKey + "\"}}"
    };

    /// <summary>
    /// Lists running session containers and force-removes every expired one.
    /// Returns the names of the sessions reaped. A failed listing reaps nothing
    /// (and logs) — the sweep is periodic, so the next pass retries.
    /// </summary>
    public async Task<IReadOnlyList<string>> SweepAsync(CancellationToken cancellationToken = default)
    {
        var listing = await _processRunner.RunAsync("docker", ListArguments, cancellationToken).ConfigureAwait(false);
        if (!listing.Succeeded)
        {
            _logger?.LogWarning(
                "Sandbox session sweep could not list containers (exit {ExitCode}): {StdErr}",
                listing.ExitCode, listing.StdErr);
            return Array.Empty<string>();
        }

        var now = _clock.GetUtcNow();
        var reaped = new List<string>();
        foreach (var line in listing.StdOut.Split('\n'))
        {
            var session = ParseSessionLine(line);
            if (session is null || !ShouldReap(session.Value.DeadlineUnixSeconds, now))
                continue;

            var name = session.Value.Name;
            var removal = await _processRunner
                .RunAsync("docker", new[] { "rm", "-f", name }, cancellationToken)
                .ConfigureAwait(false);
            if (removal.Succeeded)
            {
                reaped.Add(name);
                _logger?.LogWarning("Reaped expired sandbox session '{SessionId}'.", name);
                try
                {
                    _provenance?.Record(new SessionProvenanceEvent
                    {
                        SessionId = name,
                        Outcome = SessionProvenanceOutcomes.Reaped,
                        Timestamp = now,
                        Reason = session.Value.DeadlineUnixSeconds is null
                            ? "deadline label missing or unreadable"
                            : "deadline passed",
                    });
                }
                catch (Exception ex)
                {
                    // A provenance sink failure must never fail a sweep.
                    _logger?.LogWarning(ex, "Session provenance sink threw during reap; sweep proceeds");
                }
            }
            else
            {
                _logger?.LogWarning(
                    "Failed to reap sandbox session '{SessionId}' (exit {ExitCode}): {StdErr}",
                    name, removal.ExitCode, removal.StdErr);
            }
        }

        return reaped;
    }

    /// <summary>
    /// Parses one <c>docker ps</c> output line (<c>name\tdeadline</c>) into a session entry.
    /// Null for blank lines. A present name with an unparsable deadline yields a null
    /// deadline — which <see cref="ShouldReap"/> treats as expired.
    /// </summary>
    public static (string Name, long? DeadlineUnixSeconds)? ParseSessionLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var parts = line.Trim().Split('\t');
        var name = parts[0].Trim();
        if (name.Length == 0)
            return null;

        if (parts.Length > 1 && long.TryParse(parts[1].Trim(), out var deadline))
            return (name, deadline);

        return (name, null);
    }

    /// <summary>
    /// Whether a session with the given deadline is expired at <paramref name="now"/>.
    /// A missing deadline is expired: an unattributable session is a leak by definition.
    /// </summary>
    public static bool ShouldReap(long? deadlineUnixSeconds, DateTimeOffset now) =>
        deadlineUnixSeconds is null || deadlineUnixSeconds.Value <= now.ToUnixTimeSeconds();
}
