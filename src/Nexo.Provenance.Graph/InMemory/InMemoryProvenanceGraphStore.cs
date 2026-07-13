using Nexo.Provenance.Graph.Hashing;
using Nexo.Provenance.Graph.Models;
using Nexo.Provenance.Graph.Ports;

namespace Nexo.Provenance.Graph.InMemory;

/// <summary>In-memory provenance graph for unit tests (no Neo4j required).</summary>
public sealed class InMemoryProvenanceGraphStore : IProvenanceGraphStore
{
    private readonly object _lock = new();
    private ProvenanceGraphMetadata? _metadata;
    private readonly Dictionary<string, GraphArtifactNode> _artifacts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GraphCertificateNode> _certificates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GraphPolicyVersionNode> _policyVersions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GraphAgentNode> _agents = new(StringComparer.Ordinal);
    private readonly List<GraphEdge> _edges = new();

    public bool IsEnabled => true;

    public Task EnsureSchemaAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ProjectBundleAsync(ProvenanceCertificateBundle bundle, string certificateHash, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _artifacts[bundle.ArtifactId] = new GraphArtifactNode(bundle.ArtifactId, bundle.ArtifactKind);
            _certificates[certificateHash] = new GraphCertificateNode(
                certificateHash,
                bundle.IssuedAt,
                ProvenanceCertificateHasher.ComputeSignerKeyId(bundle.IssuerPublicKey));

            MergeEdge(bundle.ArtifactId, certificateHash, "CERTIFIED_BY");

            if (!string.IsNullOrWhiteSpace(bundle.PriorCertificateHash))
                MergeEdge(certificateHash, bundle.PriorCertificateHash!, "CHAINS_TO");

            if (!string.IsNullOrWhiteSpace(bundle.PolicyName) && !string.IsNullOrWhiteSpace(bundle.PolicyVersion))
            {
                var policyId = ProvenanceCertificateHasher.ComputePolicyVersionId(bundle.PolicyName, bundle.PolicyVersion);
                _policyVersions[policyId] = new GraphPolicyVersionNode(policyId, bundle.PolicyName, bundle.PolicyVersion);
                MergeEdge(certificateHash, policyId, "ISSUED_UNDER");
            }

            if (!string.IsNullOrWhiteSpace(bundle.ProducerAgentId) && bundle.ProducerAgentKind.HasValue)
            {
                _agents[bundle.ProducerAgentId] = new GraphAgentNode(bundle.ProducerAgentId, bundle.ProducerAgentKind.Value);
                MergeEdge(bundle.ArtifactId, bundle.ProducerAgentId, "PRODUCED_BY");
            }

            foreach (var dep in bundle.DependsOnArtifactIds)
                MergeEdge(bundle.ArtifactId, dep, "DEPENDS_ON");
        }

        return Task.CompletedTask;
    }

    public Task SetMetadataAsync(ProvenanceGraphMetadata metadata, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            _metadata = metadata;

        return Task.CompletedTask;
    }

    public Task<ProvenanceGraphMetadata?> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult(_metadata);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _metadata = null;
            _artifacts.Clear();
            _certificates.Clear();
            _policyVersions.Clear();
            _agents.Clear();
            _edges.Clear();
            _edgeKeys.Clear();
        }

        return Task.CompletedTask;
    }

    private void MergeEdge(string fromId, string toId, string relationship)
    {
        var key = $"{fromId}|{relationship}|{toId}";
        if (_edgeKeys.Add(key))
            _edges.Add(new GraphEdge(fromId, toId, relationship));
    }

    private readonly HashSet<string> _edgeKeys = new(StringComparer.Ordinal);

    public Task<ProvenanceGraphSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<ProvenanceGraphSnapshot?>(new ProvenanceGraphSnapshot
            {
                Metadata = _metadata,
                Artifacts = _artifacts.Values.ToList(),
                Certificates = _certificates.Values.ToList(),
                PolicyVersions = _policyVersions.Values.ToList(),
                Agents = _agents.Values.ToList(),
                Edges = _edges.ToList()
            });
        }
    }
}
