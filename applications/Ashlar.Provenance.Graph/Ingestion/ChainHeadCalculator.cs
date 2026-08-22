using Ashlar.Provenance.Graph.Hashing;
using Ashlar.Provenance.Graph.Models;

namespace Ashlar.Provenance.Graph.Ingestion;

/// <summary>Computes the trust-chain head hash from a certificate set.</summary>
public static class ChainHeadCalculator
{
    /// <summary>
    /// Chain head is the certificate hash with no successor in the CHAINS_TO relation.
    /// For a single cert or linear chain, this is the terminal certificate.
    /// </summary>
    public static string Compute(IReadOnlyList<VerifiedProvenanceRecord> records)
    {
        if (records.Count == 0)
            return string.Empty;

        var allHashes = records
            .Select(record => record.CertificateHash)
            .ToHashSet(StringComparer.Ordinal);

        var chainedFrom = records
            .Where(record => !string.IsNullOrWhiteSpace(record.PriorCertificateHash))
            .Select(record => record.PriorCertificateHash!)
            .ToHashSet(StringComparer.Ordinal);

        var heads = allHashes.Where(h => !chainedFrom.Contains(h)).ToList();
        if (heads.Count == 1)
            return heads[0];

        throw new ProvenanceChainHeadAmbiguousException(heads);
    }
}

/// <summary>Raised when a verified certificate set has no unique trust-chain head.</summary>
public sealed class ProvenanceChainHeadAmbiguousException : Exception
{
    public ProvenanceChainHeadAmbiguousException(IReadOnlyList<string> candidateHeads)
        : base(candidateHeads.Count == 0
            ? "Certificate chain is cyclic and has no head."
            : $"Certificate set has {candidateHeads.Count} independent chain heads.")
    {
        CandidateHeads = candidateHeads;
    }

    public IReadOnlyList<string> CandidateHeads { get; }
}
