using Ashlar.Certification.Physical;

namespace Ashlar.Provenance.Graph.Models;

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

    /// <summary>
    /// Compatibility mirror of the signed claims issuance timestamp.
    /// The projector never trusts this field as an independent source.
    /// </summary>
    public required DateTimeOffset IssuedAt { get; init; }

    /// <summary>Mirror of the signed policy claim; mismatches are rejected.</summary>
    public string? PolicyName { get; init; }

    /// <summary>Mirror of the signed policy-version claim; mismatches are rejected.</summary>
    public string? PolicyVersion { get; init; }

    /// <summary>Mirror of the signed producer claim; mismatches are rejected.</summary>
    public string? ProducerAgentId { get; init; }

    /// <summary>Mirror of the signed producer-kind claim; mismatches are rejected.</summary>
    public AgentKind? ProducerAgentKind { get; init; }

    /// <summary>Mirror of signed dependency claims; mismatches are rejected.</summary>
    public IReadOnlyList<string> DependsOnArtifactIds { get; init; } = Array.Empty<string>();

    /// <summary>Mirror of the signed prior-certificate claim; mismatches are rejected.</summary>
    public string? PriorCertificateHash { get; init; }
}
