using Ashlar.Certification.Contracts;
using Ashlar.Core.Application.Certification.Models;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// A signed FAIL record for a candidate that never reached the gate because the
/// loader or the IL fence refused it. The ledger must still hold evidence: a
/// missing file reads as "no record," which is indistinguishable from uncertified.
/// </summary>
public static class LoadRefusalRecord
{
    /// <summary>Stage written on load/fence refusals.</summary>
    public const string Stage = "load";

    /// <summary>Builds and signs a fail-closed record for <paramref name="brickId"/>.</summary>
    public static CertificationRecord Create(
        CertificationRecordSigner signer,
        string brickId,
        string reason,
        string? contentHash = null)
    {
        ArgumentNullException.ThrowIfNull(signer);
        ArgumentException.ThrowIfNullOrWhiteSpace(brickId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return signer.SignRecord(new CertificationRecord
        {
            Status = "FAIL",
            Stage = Stage,
            Admitted = false,
            Signed = true,
            Timestamp = DateTimeOffset.UtcNow,
            BrickId = brickId,
            ContentHash = contentHash,
            Reason = reason,
            Gate = "Ashlar.Infrastructure.Certification.CertificationGate",
            SchemaVersion = CertificationRecordData.TrustLoopSchemaVersion
        });
    }
}
