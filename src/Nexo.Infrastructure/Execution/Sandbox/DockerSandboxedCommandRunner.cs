using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Infrastructure.Scaling;

namespace Nexo.Infrastructure.Execution.Sandbox;

/// <summary>
/// Container-engine backend for <see cref="ISandboxedCommandRunner"/>.
/// Translates a domain-neutral <see cref="SandboxSpec"/> into a
/// <c>docker run</c> argv and executes it via <see cref="IProcessCommandRunner"/>.
/// </summary>
public sealed class DockerSandboxedCommandRunner : ISandboxedCommandRunner
{
    private readonly IProcessCommandRunner _processRunner;
    private readonly ILogger<DockerSandboxedCommandRunner>? _logger;

    /// <summary>Creates a Docker-backed sandboxed command runner.</summary>
    public DockerSandboxedCommandRunner(
        IProcessCommandRunner processRunner,
        ILogger<DockerSandboxedCommandRunner>? logger = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProcessCommandResult> RunAsync(
        SandboxSpec spec,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var args = BuildDockerArguments(spec);
        _logger?.LogDebug("Sandbox run via docker: {Args}", string.Join(' ', args));

        if (spec.Limits?.Timeout is { } timeout && timeout > TimeSpan.Zero)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(timeout);
            try
            {
                return await _processRunner.RunAsync("docker", args, linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new ProcessCommandResult(
                    -1,
                    "",
                    $"Sandbox run exceeded wall-clock timeout of {timeout.TotalSeconds:0}s.");
            }
        }

        return await _processRunner.RunAsync("docker", args, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the <c>docker run …</c> argument list for <paramref name="spec"/>.
    /// Exposed for unit tests; not a public contracts surface.
    /// </summary>
    public static IReadOnlyList<string> BuildDockerArguments(SandboxSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(spec.Image))
            throw new ArgumentException("SandboxSpec.Image is required for the Docker backend.", nameof(spec));
        if (spec.Command is null || spec.Command.Count == 0)
            throw new ArgumentException("SandboxSpec.Command must be non-empty.", nameof(spec));

        var args = new List<string>
        {
            "run",
            "--rm"
        };

        AppendSpecArguments(args, spec);

        args.Add(spec.Image!);
        args.AddRange(spec.Command);
        return args;
    }

    /// <summary>
    /// Appends the isolation-policy portion of a <c>docker run</c> argv (network, resource
    /// limits, mounts, entrypoint) for <paramref name="spec"/>. Shared with
    /// <see cref="DockerSandboxedSessionRunner"/> so one-shot commands and long-lived
    /// sessions translate a spec identically — two translations would drift.
    /// </summary>
    internal static void AppendSpecArguments(List<string> args, SandboxSpec spec)
    {
        if (spec.Network == NetworkAccess.HostServicesOnly)
        {
            // v1 fail-closed refusal (spec Part B): plain bridge networking cannot express
            // "these host services and nothing else" — realizing it needs a per-session
            // network + egress rules this backend does not manage yet. Degrading to
            // unrestricted egress while the spec promises containment would be the worst
            // possible outcome, so the spec is refused instead.
            throw new NotSupportedException(
                "NetworkAccess.HostServicesOnly is not realizable by the Docker backend in v1; "
                + "refusing fail-closed rather than degrading to unrestricted egress. "
                + "Use NetworkAccess.None, or run the dependent service inside the session image.");
        }

        if (spec.Network == NetworkAccess.None)
            args.Add("--network=none");

        var limits = spec.Limits;
        if (!string.IsNullOrWhiteSpace(limits?.Memory))
        {
            args.Add("--memory");
            args.Add(limits!.Memory!);
        }

        if (!string.IsNullOrWhiteSpace(limits?.Cpus))
        {
            args.Add("--cpus");
            args.Add(limits!.Cpus!);
        }

        if (limits?.Pids is > 0)
        {
            args.Add("--pids-limit");
            args.Add(limits.Pids.Value.ToString());
        }

        foreach (var mount in spec.Mounts ?? Array.Empty<Mount>())
        {
            if (string.IsNullOrWhiteSpace(mount.HostPath) || string.IsNullOrWhiteSpace(mount.ContainerPath))
                continue;

            var host = mount.HostPath.Replace('\\', '/');
            var container = mount.ContainerPath.Replace('\\', '/');
            var suffix = mount.ReadOnly ? ":ro" : "";
            args.Add("-v");
            args.Add($"{host}:{container}{suffix}");
        }

        if (!string.IsNullOrWhiteSpace(spec.Entrypoint))
        {
            args.Add("--entrypoint");
            args.Add(spec.Entrypoint!);
        }
    }
}
