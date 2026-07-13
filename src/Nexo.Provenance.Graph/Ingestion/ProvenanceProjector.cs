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
        var accepted = new List<ProvenanceCertificateBundle>();

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

            if (_store.IsEnabled)
                await _store.ProjectBundleAsync(bundle, verification.CertificateHash, cancellationToken).ConfigureAwait(false);

            accepted.Add(bundle);
        }

        var chainHead = ChainHeadCalculator.Compute(accepted);
        if (_store.IsEnabled && accepted.Count > 0)
        {
            await _store.SetMetadataAsync(
                new ProvenanceGraphMetadata
                {
                    ChainHeadHash = chainHead,
                    ProjectedAt = DateTimeOffset.UtcNow
                },
                cancellationToken).ConfigureAwait(false);
        }

        return new ProvenanceProjectionReport
        {
            AcceptedCount = accepted.Count,
            Rejections = rejections,
            ChainHeadHash = chainHead
        };
    }
}
