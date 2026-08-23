using System.Text.Json;
using Ashlar.Core.Application.Mesh;
using Ashlar.Core.Application.Mesh.Models;
using Ashlar.Core.Application.Mesh.Ports;
using Ashlar.Infrastructure.Execution.Routing;

namespace Ashlar.Infrastructure.Mesh;

/// <summary>
/// File-based instance discovery. Reads ~/.ashlar/instances.json or the path in ASHLAR_MESH_INSTANCES_PATH.
/// </summary>
public sealed class FileBasedInstanceDiscovery : IInstanceDiscovery
{
    private const string InstancesPathEnv = "ASHLAR_MESH_INSTANCES_PATH";

    private readonly string _instancesPath;
    private readonly PeerTrustPolicyResolver _trustPolicyResolver;

    /// <summary>Initializes a new file based instance discovery.</summary>
    public FileBasedInstanceDiscovery(
        string? instancesPath = null,
        string? trustedPeerIdsCsv = null,
        string? untrustedPeerIdsCsv = null,
        string? peerTrustPolicy = null)
    {
        _instancesPath = instancesPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ashlar", "instances.json");
        _trustPolicyResolver = new PeerTrustPolicyResolver(peerTrustPolicy ?? "any", trustedPeerIdsCsv, untrustedPeerIdsCsv);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PeerInfo>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var list = new List<PeerInfo>();
        if (!File.Exists(_instancesPath))
            return Task.FromResult<IReadOnlyList<PeerInfo>>(list);

        var discoveryPolicy = MeshTrustPolicyConfiguration.ResolveDiscoveryPolicy();

        try
        {
            var json = File.ReadAllText(_instancesPath);
            var doc = JsonDocument.Parse(json);
            foreach (var peer in doc.RootElement.EnumerateArray())
            {
                var peerId = peer.GetProperty("peerId").GetString() ?? "";
                var endpoint = peer.GetProperty("endpoint").GetString() ?? "";
                var caps = new List<string>();
                if (peer.TryGetProperty("capabilities", out var capArr))
                {
                    foreach (var c in capArr.EnumerateArray())
                        caps.Add(c.GetString() ?? "");
                }
                var trustTier = PeerTrustTier.Unknown;
                if (peer.TryGetProperty("trustTier", out var trustTierElement))
                {
                    if (trustTierElement.ValueKind == JsonValueKind.String)
                    {
                        if (Enum.TryParse<PeerTrustTier>(trustTierElement.GetString(), true, out var tier))
                            trustTier = tier;
                    }
                    else if (trustTierElement.ValueKind == JsonValueKind.Number &&
                             trustTierElement.TryGetInt32(out var numericTier) &&
                             Enum.IsDefined(typeof(PeerTrustTier), numericTier))
                    {
                        trustTier = (PeerTrustTier)numericTier;
                    }
                }

                var admitted = true;
                if (peer.TryGetProperty("admitted", out var admittedEl) && admittedEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    admitted = admittedEl.GetBoolean();

                var drained = false;
                if (peer.TryGetProperty("drained", out var drainedEl) && drainedEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    drained = drainedEl.GetBoolean();

                if (drained)
                    continue;

                if (string.Equals(discoveryPolicy, "allowlist", StringComparison.OrdinalIgnoreCase) && !admitted)
                    continue;

                var effectiveTier = _trustPolicyResolver.ResolveTier(new PeerInfo
                {
                    PeerId = peerId,
                    Endpoint = endpoint,
                    Capabilities = caps,
                    TrustTier = trustTier,
                    Admitted = admitted,
                    Drained = drained
                });
                list.Add(new PeerInfo
                {
                    PeerId = peerId,
                    Endpoint = endpoint,
                    Capabilities = caps,
                    TrustTier = effectiveTier,
                    Admitted = admitted,
                    Drained = false
                });
            }
        }
        catch
        {
            // Return empty on parse error
        }
        return Task.FromResult<IReadOnlyList<PeerInfo>>(list);
    }
}
