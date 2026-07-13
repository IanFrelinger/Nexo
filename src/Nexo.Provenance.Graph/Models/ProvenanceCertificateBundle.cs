using Nexo.Certification.Physical;

namespace Nexo.Provenance.Graph.Models;

/// <summary>
/// Portable certificate artifact bundle for provenance graph ingestion.
/// Carries the Ed25519-signed certificate, bound artifact bytes, and graph metadata.
/// </summary>
public sealed record ProvenanceCertificateBundle
{
    /// <summary>Content hash of the bound artifact (artifact node id).</summary>
    public required string ArtifactId { get; init; }

    /// <summary>Kind of artifact being certified.</summary>
    public required ArtifactKind ArtifactKind { get; init; }

    /// <summary>Ed25519-signed certificate payload.</summary>
    public required PhysicalAtomCertificate Certificate { get; init; }

    /// <summary>Bound artifact bytes used for content-hash verification.</summary>
    public required byte[] ArtifactContent { get; init; }

    /// <summary>Issuer Ed25519 public key (32 bytes).</summary>
    public required byte[] IssuerPublicKey { get; init; }

    /// <summary>UTC issuance timestamp recorded in the graph.</summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Policy name for <c>ISSUED_UNDER</c> edge (optional).</summary>
    public string? PolicyName { get; init; }

    /// <summary>Policy version for <c>ISSUED_UNDER</c> edge (optional).</summary>
    public string? PolicyVersion { get; init; }

    /// <summary>Producer agent id for <c>PRODUCED_BY</c> edge (optional).</summary>
    public string? ProducerAgentId { get; init; }

    /// <summary>Producer agent kind for <c>PRODUCED_BY</c> edge (optional).</summary>
    public AgentKind? ProducerAgentKind { get; init; }

    /// <summary>Upstream artifact content hashes for composition <c>DEPENDS_ON</c> edges.</summary>
    public IReadOnlyList<string> DependsOnArtifactIds { get; init; } = Array.Empty<string>();

    /// <summary>Prior certificate hash for <c>CHAINS_TO</c> edge (optional).</summary>
    public string? PriorCertificateHash { get; init; }
}
