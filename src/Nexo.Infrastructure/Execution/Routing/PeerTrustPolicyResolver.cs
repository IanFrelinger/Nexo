using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Infrastructure.Execution.Routing;

internal sealed class PeerTrustPolicyResolver
{
    private readonly HashSet<string> _trustedPeerIds;
    private readonly HashSet<string> _untrustedPeerIds;
    private readonly string _policy;

    public PeerTrustPolicyResolver(string? policy, string? trustedPeerIdsCsv, string? untrustedPeerIdsCsv)
    {
        _policy = NormalizePolicy(policy);
        _trustedPeerIds = ParseCsv(trustedPeerIdsCsv);
        _untrustedPeerIds = ParseCsv(untrustedPeerIdsCsv);
    }

    public string Policy => _policy;

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

    public bool IsAllowed(PeerTrustTier tier)
    {
        return _policy switch
        {
            "trusted-only" => tier == PeerTrustTier.Trusted,
            "trusted-preferred" => tier != PeerTrustTier.Untrusted,
            _ => true
        };
    }

    public bool IsAllowed(PeerExecutionCandidate candidate)
    {
        return IsAllowed(candidate.TrustTier);
    }

    private static string NormalizePolicy(string? policy)
    {
        var normalized = (policy ?? "trusted-preferred").Trim().ToLowerInvariant();
        return normalized switch
        {
            "trusted-only" => "trusted-only",
            "trusted-preferred" => "trusted-preferred",
            "any" => "any",
            _ => "trusted-preferred"
        };
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
