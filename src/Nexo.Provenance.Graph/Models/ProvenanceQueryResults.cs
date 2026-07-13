namespace Nexo.Provenance.Graph.Models;

/// <summary>Base for query results annotated with the chain-head hash they were computed against.</summary>
public abstract record ProvenanceQueryResult
{
    /// <summary>Chain-head hash recorded in graph metadata at query time.</summary>
    public required string ChainHeadHash { get; init; }
}

/// <summary>Full upstream lineage for an artifact.</summary>
public sealed record LineageQueryResult : ProvenanceQueryResult
{
    /// <summary>Artifact id queried.</summary>
    public required string ArtifactId { get; init; }

    /// <summary>Certificates in trust-chain order (root to head).</summary>
    public required IReadOnlyList<LineageCertificateEntry> Certificates { get; init; }

    /// <summary>Upstream dependency artifact ids.</summary>
    public required IReadOnlyList<string> DependencyArtifactIds { get; init; }
}

/// <summary>Certificate entry in a lineage result.</summary>
public sealed record LineageCertificateEntry
{
    public required string CertificateHash { get; init; }
    public DateTimeOffset? IssuedAt { get; init; }
    public required string SignerKeyId { get; init; }
    public string? PolicyId { get; init; }
    public string? PolicyVersion { get; init; }
}

/// <summary>Artifacts whose cert chain passes through a policy version.</summary>
public sealed record ArtifactsUnderPolicyResult : ProvenanceQueryResult
{
    public required string PolicyId { get; init; }
    public required string PolicyVersion { get; init; }
    public required IReadOnlyList<string> ArtifactIds { get; init; }
}

/// <summary>Downstream artifacts affected by revoking a policy version.</summary>
public sealed record BlastRadiusResult : ProvenanceQueryResult
{
    public required string PolicyId { get; init; }
    public required string PolicyVersion { get; init; }
    public required IReadOnlyList<string> AffectedArtifactIds { get; init; }
}
