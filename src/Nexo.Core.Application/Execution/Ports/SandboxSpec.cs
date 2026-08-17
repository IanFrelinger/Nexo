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

    /// <summary>
    /// Reach only services the host explicitly publishes to the sandbox — the endpoints
    /// listed in <see cref="SandboxSpec.AllowedEndpoints"/> (e.g. a local model server) —
    /// with no general egress (extension spec Part B). Backends that cannot actually
    /// realize this containment MUST refuse the spec fail-closed rather than degrade to
    /// <see cref="Unrestricted"/>: a session believing itself air-gapped while holding
    /// open egress is strictly worse than no session.
    ///
    /// <para><b>No shipped backend realizes this mode, and that refusal is now the settled
    /// posture rather than a v1 gap.</b> The one workload that seemed to need it — package
    /// restore during the in-session candidate build — was solved WITHOUT network instead:
    /// the session build restores offline from the SDK's installed packs against cleared
    /// package sources (<c>SessionCandidateBuild</c>). A model server, the other imagined
    /// consumer, belongs on the proposer side of the boundary, not inside a certification
    /// session. The mode stays declared (and <see cref="SandboxSpec.AllowedEndpoints"/>
    /// stays attestation-relevant) for backends that can genuinely realize it; none is
    /// planned.</para>
    /// </summary>
    HostServicesOnly = 2,
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
    string? Entrypoint = null)
{
    /// <summary>
    /// The host services a <see cref="NetworkAccess.HostServicesOnly"/> sandbox may reach
    /// (<c>host:port</c> entries). Part of the spec — and therefore of resource
    /// attestation — even before a backend can enforce it: what a session was ALLOWED to
    /// reach is certificate-relevant either way. Ignored for other network modes.
    /// </summary>
    public IReadOnlyList<string> AllowedEndpoints { get; init; } = Array.Empty<string>();

    /// <summary>
    /// The sandbox paths the workload needs to WRITE — its working directories plus whatever
    /// its toolchain scribbles into (temp, home). Backends that seal the sandbox's root
    /// filesystem read-only back each listed path with ephemeral, size-capped scratch
    /// storage that dies with the sandbox; everything else stays read-only, so a write
    /// outside the declared surface fails loudly instead of landing somewhere unrecorded.
    /// Part of the spec, and therefore of the certificate's <c>sandbox-spec</c> input: what
    /// a session was ALLOWED to write is evidence. Empty declares no write surface at all —
    /// correct for a pure keepalive, and a loud failure for a workload that forgot to
    /// declare its own.
    /// </summary>
    public IReadOnlyList<string> ScratchPaths { get; init; } = Array.Empty<string>();
}
