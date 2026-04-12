using System.Text.Json;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Mesh.Ports;
using Nexo.Infrastructure.Execution.Routing;

namespace Nexo.Infrastructure.Mesh;

/// <summary>
/// File-based instance discovery. Reads ~/.nexo/instances.json.
/// </summary>
public sealed class FileBasedInstanceDiscovery : IInstanceDiscovery
{
    private readonly string _instancesPath;
    private readonly PeerTrustPolicyResolver _trustPolicyResolver;

    public FileBasedInstanceDiscovery(
        string? instancesPath = null,
        string? trustedPeerIdsCsv = null,
        string? untrustedPeerIdsCsv = null,
        string? peerTrustPolicy = null)
    {
        _instancesPath = instancesPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nexo", "instances.json");
        _trustPolicyResolver = new PeerTrustPolicyResolver(peerTrustPolicy ?? "any", trustedPeerIdsCsv, untrustedPeerIdsCsv);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PeerInfo>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var list = new List<PeerInfo>();
        if (!File.Exists(_instancesPath))
            return Task.FromResult<IReadOnlyList<PeerInfo>>(list);

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
                var effectiveTier = _trustPolicyResolver.ResolveTier(new PeerInfo
                {
                    PeerId = peerId,
                    Endpoint = endpoint,
                    Capabilities = caps,
                    TrustTier = trustTier
                });
                list.Add(new PeerInfo { PeerId = peerId, Endpoint = endpoint, Capabilities = caps, TrustTier = effectiveTier });
            }
        }
        catch
        {
            // Return empty on parse error
        }
        return Task.FromResult<IReadOnlyList<PeerInfo>>(list);
    }
}
