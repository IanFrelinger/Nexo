using Microsoft.Extensions.Logging;
using Nexo.Provenance.Graph.Hashing;
using Nexo.Provenance.Graph.Models;
using Nexo.Provenance.Graph.Ports;
using Nexo.Provenance.Graph.Verification;

namespace Nexo.Provenance.Graph.Ingestion;

/// <summary>
/// Walks certificate artifacts, verifies Ed25519 signatures and content binding, and projects into the graph.
/// </summary>
public sealed class ProvenanceProjector
{
    private readonly IProvenanceGraphStore _store;
    private readonly ILogger<ProvenanceProjector> _logger;

    public ProvenanceProjector(IProvenanceGraphStore store, ILogger<ProvenanceProjector> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Project verified bundles; reject invalid certs with a report.</summary>
    public async Task<ProvenanceProjectionReport> ProjectAsync(
        IEnumerable<ProvenanceCertificateBundle> bundles,
        CancellationToken cancellationToken = default)
    {
        var rejections = new List<ProvenanceRejection>();
        var verified = new List<VerifiedProvenanceRecord>();

        foreach (var bundle in bundles)
        {
            var verification = ProvenanceCertificateVerifier.Verify(bundle);
            if (!verification.Trusted)
            {
                _logger.LogWarning(
                    "Provenance projection rejected cert {CertHash}: {Code} — {Reason}",
                    verification.CertificateHash,
                    verification.FailureCode,
                    verification.Reason);

                rejections.Add(new ProvenanceRejection
                {
                    CertificateHash = verification.CertificateHash,
                    FailureCode = verification.FailureCode ?? "verification-failed",
                    Reason = verification.Reason ?? "Verification failed."
                });
                continue;
            }

            verified.Add(verification.Record!);
        }

        if (rejections.Count > 0)
            return RejectedBatch(rejections);

        verified = verified
            .GroupBy(record => record.CertificateHash, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        var certificateHashes = verified
            .Select(record => record.CertificateHash)
            .ToHashSet(StringComparer.Ordinal);
        var artifactIds = verified
            .Select(record => record.ArtifactId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var record in verified)
        {
            if (record.PriorCertificateHash is not null
                && !certificateHashes.Contains(record.PriorCertificateHash))
            {
                rejections.Add(new ProvenanceRejection
                {
                    CertificateHash = record.CertificateHash,
                    FailureCode = "chain-reference-missing",
                    Reason = $"Signed prior certificate '{record.PriorCertificateHash}' is not present in the verified source set."
                });
            }

            foreach (var dependency in record.DependsOnArtifactIds.Where(id => !artifactIds.Contains(id)))
            {
                rejections.Add(new ProvenanceRejection
                {
                    CertificateHash = record.CertificateHash,
                    FailureCode = "dependency-reference-missing",
                    Reason = $"Signed dependency artifact '{dependency}' is not present in the verified source set."
                });
            }
        }

        if (rejections.Count > 0)
            return RejectedBatch(rejections);

        string chainHead;
        try
        {
            chainHead = ChainHeadCalculator.Compute(verified);
        }
        catch (ProvenanceChainHeadAmbiguousException ex)
        {
            rejections.Add(new ProvenanceRejection
            {
                CertificateHash = string.Empty,
                FailureCode = "chain-head-ambiguous",
                Reason = ex.Message
            });
            return RejectedBatch(rejections);
        }

        if (_store.IsEnabled && verified.Count > 0)
        {
            await _store.ProjectBatchAsync(
                verified,
                new ProvenanceGraphMetadata
                {
                    ChainHeadHash = chainHead,
                    ProjectedAt = DateTimeOffset.UtcNow
                },
                cancellationToken).ConfigureAwait(false);
        }

        return new ProvenanceProjectionReport
        {
            AcceptedCount = verified.Count,
            Rejections = rejections,
            ChainHeadHash = chainHead
        };
    }

    private static ProvenanceProjectionReport RejectedBatch(IReadOnlyList<ProvenanceRejection> rejections) =>
        new()
        {
            AcceptedCount = 0,
            Rejections = rejections,
            ChainHeadHash = string.Empty
        };
}
