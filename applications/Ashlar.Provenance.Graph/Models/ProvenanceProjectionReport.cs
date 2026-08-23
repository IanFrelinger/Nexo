namespace Ashlar.Provenance.Graph.Models;

/// <summary>Result of projecting certificate artifacts into the provenance graph.</summary>
public sealed record ProvenanceProjectionReport
{
    /// <summary>Number of certificates successfully projected.</summary>
    public required int AcceptedCount { get; init; }

    /// <summary>Rejected certificates with reasons.</summary>
    public required IReadOnlyList<ProvenanceRejection> Rejections { get; init; }

    /// <summary>Chain-head hash the projection was built from.</summary>
    public required string ChainHeadHash { get; init; }
}

/// <summary>A certificate that failed verification and was not projected.</summary>
public sealed record ProvenanceRejection
{
    /// <summary>Certificate hash that was rejected.</summary>
    public required string CertificateHash { get; init; }

    /// <summary>Machine-readable failure code.</summary>
    public required string FailureCode { get; init; }

    /// <summary>Human-readable rejection reason.</summary>
    public required string Reason { get; init; }
}
