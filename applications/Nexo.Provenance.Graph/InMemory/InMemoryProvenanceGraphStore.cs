using Nexo.Provenance.Graph.Hashing;
using Nexo.Provenance.Graph.Models;
using Nexo.Provenance.Graph.Ports;

namespace Nexo.Provenance.Graph.InMemory;

/// <summary>In-memory provenance graph for unit tests (no Neo4j required).</summary>
public sealed class InMemoryProvenanceGraphStore : IProvenanceGraphStore
{
    private readonly object _lock = new();
    private ProvenanceGraphMetadata? _metadata;
    private Dictionary<string, GraphArtifactNode> _artifacts = new(StringComparer.Ordinal);
    private Dictionary<string, GraphCertificateNode> _certificates = new(StringComparer.Ordinal);
    private Dictionary<string, GraphPolicyVersionNode> _policyVersions = new(StringComparer.Ordinal);
    private Dictionary<string, GraphAgentNode> _agents = new(StringComparer.Ordinal);
    private List<GraphEdge> _edges = [];
    private HashSet<string> _edgeKeys = new(StringComparer.Ordinal);

    public bool IsEnabled => true;

    public Task EnsureSchemaAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ProjectBatchAsync(
        IReadOnlyList<VerifiedProvenanceRecord> records,
        ProvenanceGraphMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var artifacts = new Dictionary<string, GraphArtifactNode>(_artifacts, StringComparer.Ordinal);
            var certificates = new Dictionary<string, GraphCertificateNode>(_certificates, StringComparer.Ordinal);
            var policies = new Dictionary<string, GraphPolicyVersionNode>(_policyVersions, StringComparer.Ordinal);
            var agents = new Dictionary<string, GraphAgentNode>(_agents, StringComparer.Ordinal);
            var edges = new List<GraphEdge>(_edges);
            var edgeKeys = new HashSet<string>(_edgeKeys, StringComparer.Ordinal);

            foreach (var record in records)
            {
                artifacts[record.ArtifactId] = new GraphArtifactNode(record.ArtifactId, record.ArtifactKind);
                certificates[record.CertificateHash] = new GraphCertificateNode(
                    record.CertificateHash,
                    record.IssuedAt,
                    record.SignerKeyId);
            }

            foreach (var record in records)
            {
                MergeEdge(edges, edgeKeys, record.ArtifactId, record.CertificateHash, "CERTIFIED_BY");

                if (!string.IsNullOrWhiteSpace(record.PriorCertificateHash))
                {
                    if (!certificates.ContainsKey(record.PriorCertificateHash))
                        throw new InvalidOperationException($"Unknown prior certificate '{record.PriorCertificateHash}'.");
                    MergeEdge(edges, edgeKeys, record.CertificateHash, record.PriorCertificateHash, "CHAINS_TO");
                }

                if (!string.IsNullOrWhiteSpace(record.PolicyName) && !string.IsNullOrWhiteSpace(record.PolicyVersion))
                {
                    var policyId = ProvenanceCertificateHasher.ComputePolicyVersionId(record.PolicyName, record.PolicyVersion);
                    policies[policyId] = new GraphPolicyVersionNode(policyId, record.PolicyName, record.PolicyVersion);
                    MergeEdge(edges, edgeKeys, record.CertificateHash, policyId, "ISSUED_UNDER");
                }

                if (!string.IsNullOrWhiteSpace(record.ProducerAgentId) && record.ProducerAgentKind.HasValue)
                {
                    agents[record.ProducerAgentId] = new GraphAgentNode(record.ProducerAgentId, record.ProducerAgentKind.Value);
                    MergeEdge(edges, edgeKeys, record.ArtifactId, record.ProducerAgentId, "PRODUCED_BY");
                }

                foreach (var dependency in record.DependsOnArtifactIds)
                {
                    if (!artifacts.ContainsKey(dependency))
                        throw new InvalidOperationException($"Unknown dependency artifact '{dependency}'.");
                    MergeEdge(edges, edgeKeys, record.ArtifactId, dependency, "DEPENDS_ON");
                }
            }

            _artifacts = artifacts;
            _certificates = certificates;
            _policyVersions = policies;
            _agents = agents;
            _edges = edges;
            _edgeKeys = edgeKeys;
            _metadata = metadata;
        }

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

    private static void MergeEdge(
        List<GraphEdge> edges,
        HashSet<string> edgeKeys,
        string fromId,
        string toId,
        string relationship)
    {
        var key = $"{fromId}|{relationship}|{toId}";
        if (edgeKeys.Add(key))
            edges.Add(new GraphEdge(fromId, toId, relationship));
    }

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
