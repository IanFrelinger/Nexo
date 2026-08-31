namespace Ashlar.CLI.Commands.BackgroundAgent;

/// <summary>
/// The strategy seam of the federated mesh: WHERE peer addresses come from. Auto-pull consumes every
/// registered source each tick and pulls from the union — so how peers are found (a configured list,
/// a LAN multicast beacon, a tailnet enumeration, a rendezvous file) is swappable without touching
/// how packages move or how they are trusted. That separation is safe by construction: a source only
/// NOMINATES addresses to pull from, and pulling is already fail-closed end to end (bounded download,
/// intrinsic Ed25519 verification, the receiver's trust root refusing an untrusted signer before
/// anything parks). A new source is a new class and a DI registration — never a safety review of the
/// admission path.
/// </summary>
public interface IPeerSource
{
    /// <summary>Short human label for logs and `mesh lan` ("configured", "multicast", "tailnet"…).</summary>
    string Describe();

    /// <summary>The peer base URLs this source currently knows (e.g. <c>http://192.168.1.20:7420</c>).
    /// Cheap and non-blocking — called every pull tick. May be empty; peers come and go.</summary>
    IReadOnlyList<string> CurrentPeerBaseUrls();
}

/// <summary>
/// Peers the operator configured by address (<c>ASHLAR_MESH_PEERS</c>). The works-everywhere
/// baseline: a LAN address, a Tailscale/VPN address, or anything routable — the transport does not
/// care, and neither does the trust model.
/// </summary>
public sealed class ConfiguredPeerSource : IPeerSource
{
    private readonly IReadOnlyList<string> _urls;

    /// <summary>Creates the source over a fixed URL list.</summary>
    public ConfiguredPeerSource(IReadOnlyList<string> urls) => _urls = urls ?? [];

    /// <inheritdoc />
    public string Describe() => "configured";

    /// <inheritdoc />
    public IReadOnlyList<string> CurrentPeerBaseUrls() => _urls;
}

/// <summary>
/// Peers heard on the local network by the F3 multicast beacon — the zero-config LAN-party source.
/// Reads the live discovery registry each tick, so guests joining and leaving are reflected without
/// restarts. Discovery is presence, not trust.
/// </summary>
public sealed class MulticastPeerSource : IPeerSource
{
    private readonly MeshDiscoveryRegistry _registry;

    /// <summary>Creates the source over the discovery registry.</summary>
    public MulticastPeerSource(MeshDiscoveryRegistry registry) =>
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    /// <inheritdoc />
    public string Describe() => "multicast";

    /// <inheritdoc />
    public IReadOnlyList<string> CurrentPeerBaseUrls() =>
        _registry.Snapshot(DateTimeOffset.UtcNow).Select(p => p.BaseUrl).ToList();
}
