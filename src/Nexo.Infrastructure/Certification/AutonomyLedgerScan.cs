using Nexo.Certification.Contracts;
using Nexo.Core.Application.Autonomy;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// The periodic ledger scan behind the digest (autonomy spec R5.4 + §8): sweeps every
/// verifiable record in the store for (a) certificates transitively built on revoked
/// artifacts — suspects that must be flagged for re-verification — and (b) content
/// hashes certified at contradictory generation depths — the record-store leg of the
/// depth-laundering defense. Pure read path: the scan flags, humans (or the quarantine
/// flow) act.
/// </summary>
public static class AutonomyLedgerScan
{
    /// <summary>One scan's findings. Both lists empty = the ledger is clean.</summary>
    public sealed record Report(
        IReadOnlyList<CertificationRecordData> RevocationSuspects,
        IReadOnlyList<DepthLaunderingDetector.Contradiction> LaunderingContradictions)
    {
        /// <summary>Whether the scan found nothing.</summary>
        public bool IsClean => RevocationSuspects.Count == 0 && LaunderingContradictions.Count == 0;
    }

    /// <summary>Runs the scan over every verifiable record and the full revocation set.</summary>
    public static Report Run(ICertificationRecordStore store, ICertificateRevocationList revocations)
    {
        if (store is null)
            throw new ArgumentNullException(nameof(store));
        if (revocations is null)
            throw new ArgumentNullException(nameof(revocations));

        var data = store.All()
            .Select(CertificationRecordMapper.ToData)
            .ToArray();

        var suspects = revocations.Snapshot()
            .SelectMany(revokedHash => RevocationChainScanner.FindSuspects(data, revokedHash))
            .GroupBy(record => record.ContentHash ?? "", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        var contradictions = DepthLaunderingDetector.FindContradictions(data);

        return new Report(suspects, contradictions);
    }
}
