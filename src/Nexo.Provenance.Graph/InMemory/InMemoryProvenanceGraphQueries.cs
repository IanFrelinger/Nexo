using Nexo.Provenance.Graph.Models;
using Nexo.Provenance.Graph.Ports;

namespace Nexo.Provenance.Graph.InMemory;

/// <summary>In-memory query implementation for unit tests.</summary>
public sealed class InMemoryProvenanceGraphQueries : IProvenanceGraphQueries
{
    private readonly InMemoryProvenanceGraphStore _store;

    public InMemoryProvenanceGraphQueries(InMemoryProvenanceGraphStore store) =>
        _store = store ?? throw new ArgumentNullException(nameof(store));

    public async Task<LineageQueryResult> LineageOfAsync(string artifactId, string currentChainHeadHash, CancellationToken cancellationToken = default)
    {
        var metadata = await _store.GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        EnsureFresh(metadata, currentChainHeadHash);

        var snapshot = await _store.GetSnapshotAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Graph snapshot unavailable.");

        var certHashes = snapshot.Edges
            .Where(e => e.Relationship == "CERTIFIED_BY" && e.FromId == artifactId)
            .Select(e => e.ToId)
            .ToList();

        var chain = WalkCertificateChain(snapshot, certHashes.FirstOrDefault() ?? string.Empty);
        var deps = snapshot.Edges
            .Where(e => e.Relationship == "DEPENDS_ON" && e.FromId == artifactId)
            .Select(e => e.ToId)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new LineageQueryResult
        {
            ChainHeadHash = metadata!.ChainHeadHash,
            ArtifactId = artifactId,
            Certificates = chain,
            DependencyArtifactIds = deps
        };
    }

    public async Task<ArtifactsUnderPolicyResult> ArtifactsUnderPolicyAsync(string policyId, string policyVersion, string currentChainHeadHash, CancellationToken cancellationToken = default)
    {
        var metadata = await _store.GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        EnsureFresh(metadata, currentChainHeadHash);

        var snapshot = await _store.GetSnapshotAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Graph snapshot unavailable.");

        var policyNodeId = $"{policyId}@{policyVersion}";
        var certHashes = FindCertsUnderPolicy(snapshot, policyNodeId);
        var artifactIds = certHashes
            .SelectMany(certHash => snapshot.Edges
                .Where(e => e.Relationship == "CERTIFIED_BY" && e.ToId == certHash)
                .Select(e => e.FromId))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return new ArtifactsUnderPolicyResult
        {
            ChainHeadHash = metadata!.ChainHeadHash,
            PolicyId = policyId,
            PolicyVersion = policyVersion,
            ArtifactIds = artifactIds
        };
    }

    public async Task<BlastRadiusResult> BlastRadiusOfAsync(string policyId, string policyVersion, string currentChainHeadHash, CancellationToken cancellationToken = default)
    {
        var underPolicy = await ArtifactsUnderPolicyAsync(policyId, policyVersion, currentChainHeadHash, cancellationToken)
            .ConfigureAwait(false);

        var snapshot = await _store.GetSnapshotAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Graph snapshot unavailable.");

        var affected = new HashSet<string>(underPolicy.ArtifactIds, StringComparer.Ordinal);
        var queue = new Queue<string>(underPolicy.ArtifactIds);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var downstream in snapshot.Edges
                         .Where(e => e.Relationship == "DEPENDS_ON" && e.ToId == current)
                         .Select(e => e.FromId))
            {
                if (affected.Add(downstream))
                    queue.Enqueue(downstream);
            }
        }

        return new BlastRadiusResult
        {
            ChainHeadHash = underPolicy.ChainHeadHash,
            PolicyId = policyId,
            PolicyVersion = policyVersion,
            AffectedArtifactIds = affected.OrderBy(id => id, StringComparer.Ordinal).ToList()
        };
    }

    private static void EnsureFresh(ProvenanceGraphMetadata? metadata, string currentChainHeadHash)
    {
        if (metadata is null)
            throw new ProvenanceGraphStaleException(string.Empty, currentChainHeadHash);

        if (!string.Equals(metadata.ChainHeadHash, currentChainHeadHash, StringComparison.Ordinal))
            throw new ProvenanceGraphStaleException(metadata.ChainHeadHash, currentChainHeadHash);
    }

    private static IReadOnlyList<LineageCertificateEntry> WalkCertificateChain(ProvenanceGraphSnapshot snapshot, string startCertHash)
    {
        if (string.IsNullOrWhiteSpace(startCertHash))
            return Array.Empty<LineageCertificateEntry>();

        var chain = new List<LineageCertificateEntry>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = startCertHash;

        while (!string.IsNullOrWhiteSpace(current) && visited.Add(current))
        {
            if (!snapshot.Certificates.Any(c => c.Id == current))
                break;

            var cert = snapshot.Certificates.First(c => c.Id == current);
            var policyEdge = snapshot.Edges.FirstOrDefault(e => e.Relationship == "ISSUED_UNDER" && e.FromId == current);
            string? policyName = null;
            string? policyVer = null;
            if (policyEdge is not null)
            {
                var policy = snapshot.PolicyVersions.FirstOrDefault(p => p.Id == policyEdge.ToId);
                if (policy is not null)
                {
                    policyName = policy.Name;
                    policyVer = policy.Version;
                }
            }

            chain.Add(new LineageCertificateEntry
            {
                CertificateHash = cert.Id,
                IssuedAt = cert.IssuedAt,
                SignerKeyId = cert.SignerKeyId,
                PolicyId = policyName,
                PolicyVersion = policyVer
            });

            current = snapshot.Edges
                .Where(e => e.Relationship == "CHAINS_TO" && e.FromId == current)
                .Select(e => e.ToId)
                .FirstOrDefault() ?? string.Empty;
        }

        chain.Reverse();
        return chain;
    }

    private static HashSet<string> FindCertsUnderPolicy(ProvenanceGraphSnapshot snapshot, string policyNodeId)
    {
        var direct = snapshot.Edges
            .Where(e => e.Relationship == "ISSUED_UNDER" && e.ToId == policyNodeId)
            .Select(e => e.FromId)
            .ToHashSet(StringComparer.Ordinal);

        var expanded = new HashSet<string>(direct, StringComparer.Ordinal);
        var queue = new Queue<string>(direct);
        while (queue.Count > 0)
        {
            var cert = queue.Dequeue();
            foreach (var chained in snapshot.Edges
                         .Where(e => e.Relationship == "CHAINS_TO" && e.ToId == cert)
                         .Select(e => e.FromId))
            {
                if (expanded.Add(chained))
                    queue.Enqueue(chained);
            }
        }

        return expanded;
    }
}
