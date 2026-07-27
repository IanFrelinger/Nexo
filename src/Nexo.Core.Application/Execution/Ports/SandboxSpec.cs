namespace Nexo.Core.Application.Execution.Ports;

/// <summary>
/// Network policy for a sandboxed command. Default is <see cref="None"/>
/// (no egress). Profiles opt into broader access explicitly.
/// </summary>
public enum NetworkAccess
{
    /// <summary>No network access (recommended default).</summary>
    None = 0,

    /// <summary>Unrestricted network as provided by the sandbox backend.</summary>
    Unrestricted = 1,
}

/// <summary>Host→container (or host→jail) bind mount.</summary>
/// <param name="HostPath">Path on the host/workspace.</param>
/// <param name="ContainerPath">Path visible inside the sandbox.</param>
/// <param name="ReadOnly">When true, mount is read-only.</param>
public sealed record Mount(string HostPath, string ContainerPath, bool ReadOnly = true);

/// <summary>
/// Resource caps for a sandboxed run. All fields optional — backends apply
/// their own defaults when null.
/// </summary>
/// <param name="Memory">Memory cap (backend-specific encoding, e.g. "2g").</param>
/// <param name="Pids">Maximum process IDs inside the sandbox.</param>
/// <param name="Cpus">CPU cap (backend-specific encoding, e.g. "2").</param>
/// <param name="Timeout">Wall-clock timeout for the run.</param>
public sealed record ResourceLimits(
    string? Memory = null,
    int? Pids = null,
    string? Cpus = null,
    TimeSpan? Timeout = null);

/// <summary>
/// Domain-neutral description of a command to run under isolation.
/// Image/runtime identifiers are opaque strings supplied by concrete profiles —
/// this type never names a container engine or language toolchain.
/// </summary>
/// <param name="Image">Opaque runtime/image identifier required by the backend.</param>
/// <param name="Mounts">Bind mounts into the sandbox.</param>
/// <param name="Network">Network policy (default callers should pass <see cref="NetworkAccess.None"/>).</param>
/// <param name="Command">Argv to execute inside the sandbox (argv[0] is the program).</param>
/// <param name="Limits">Optional resource caps.</param>
/// <param name="Entrypoint">
/// Optional opaque entrypoint override for the sandbox backend (domain-neutral —
/// profiles supply the program name; core never names a toolchain).
/// </param>
public sealed record SandboxSpec(
    string? Image,
    IReadOnlyList<Mount> Mounts,
    NetworkAccess Network,
    IReadOnlyList<string> Command,
    ResourceLimits? Limits = null,
    string? Entrypoint = null);
