using Nexo.Core.Application.Mesh;
using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Infrastructure.Execution.Routing;

/// <summary>Resolves peer trust tiers from mesh policy configuration and allow/deny lists.</summary>
internal sealed class PeerTrustPolicyResolver
{
    private readonly HashSet<string> _trustedPeerIds;
    private readonly HashSet<string> _untrustedPeerIds;
    private readonly string _policy;

    /// <summary>Initializes a new peer trust policy resolver.</summary>
    public PeerTrustPolicyResolver(string? policy, string? trustedPeerIdsCsv, string? untrustedPeerIdsCsv)
    {
        _policy = MeshTrustPolicyConfiguration.NormalizePolicy(policy);
        _trustedPeerIds = ParseCsv(trustedPeerIdsCsv);
        _untrustedPeerIds = ParseCsv(untrustedPeerIdsCsv);
    }

    /// <summary>Policy.</summary>
    public string Policy => _policy;

    /// <summary>Resolve tier.</summary>
    public PeerTrustTier ResolveTier(PeerInfo peer)
    {
        if (peer == null || string.IsNullOrWhiteSpace(peer.PeerId))
            return PeerTrustTier.Unknown;

        var peerId = peer.PeerId.Trim();
        if (_trustedPeerIds.Contains(peerId))
            return PeerTrustTier.Trusted;
        if (_untrustedPeerIds.Contains(peerId))
            return PeerTrustTier.Untrusted;
        return peer.TrustTier;
    }

    /// <summary>Whether allowed.</summary>
    public bool IsAllowed(PeerTrustTier tier)
    {
        return _policy switch
        {
            "trusted-only" => tier == PeerTrustTier.Trusted,
            "trusted-preferred" => tier != PeerTrustTier.Untrusted,
            "allowlist" => tier == PeerTrustTier.Trusted,
            _ => true
        };
    }

    /// <summary>Whether allowed.</summary>
    public bool IsAllowed(PeerExecutionCandidate candidate)
    {
        return IsAllowed(candidate.TrustTier);
    }

    private static HashSet<string> ParseCsv(string? csv)
    {
        return (csv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
