using Nexo.Provenance.Graph.Hashing;
using Nexo.Provenance.Graph.Models;

namespace Nexo.Provenance.Graph.Ingestion;

/// <summary>Computes the trust-chain head hash from a certificate set.</summary>
public static class ChainHeadCalculator
{
    /// <summary>
    /// Chain head is the certificate hash with no successor in the CHAINS_TO relation.
    /// For a single cert or linear chain, this is the terminal certificate.
    /// </summary>
    public static string Compute(IReadOnlyList<ProvenanceCertificateBundle> acceptedBundles)
    {
        if (acceptedBundles.Count == 0)
            return string.Empty;

        var allHashes = acceptedBundles
            .Select(b => ProvenanceCertificateHasher.ComputeCertificateHash(b.Certificate))
            .ToHashSet(StringComparer.Ordinal);

        var chainedFrom = acceptedBundles
            .Where(b => !string.IsNullOrWhiteSpace(b.PriorCertificateHash))
            .Select(b => b.PriorCertificateHash!)
            .ToHashSet(StringComparer.Ordinal);

        var heads = allHashes.Where(h => !chainedFrom.Contains(h)).ToList();
        if (heads.Count == 1)
            return heads[0];

        if (heads.Count == 0)
            return allHashes.OrderBy(h => h, StringComparer.Ordinal).Last();

        return heads.OrderBy(h => h, StringComparer.Ordinal).Last();
    }
}
