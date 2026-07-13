namespace Nexo.Provenance.Graph.Models;

/// <summary>In-memory graph snapshot for testing and null-store inspection.</summary>
public sealed record ProvenanceGraphSnapshot
{
    public required ProvenanceGraphMetadata? Metadata { get; init; }
    public required IReadOnlyList<GraphArtifactNode> Artifacts { get; init; }
    public required IReadOnlyList<GraphCertificateNode> Certificates { get; init; }
    public required IReadOnlyList<GraphPolicyVersionNode> PolicyVersions { get; init; }
    public required IReadOnlyList<GraphAgentNode> Agents { get; init; }
    public required IReadOnlyList<GraphEdge> Edges { get; init; }
}

public sealed record GraphArtifactNode(string Id, ArtifactKind Kind);

public sealed record GraphCertificateNode(string Id, DateTimeOffset? IssuedAt, string SignerKeyId);

public sealed record GraphPolicyVersionNode(string Id, string Name, string Version);

public sealed record GraphAgentNode(string Id, AgentKind Kind);

public sealed record GraphEdge(string FromId, string ToId, string Relationship);
