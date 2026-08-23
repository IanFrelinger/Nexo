using Ashlar.Certification.Contracts;

namespace Ashlar.Infrastructure.Certification;

/// <summary>
/// R5.4 revocation propagation: any certificate whose <c>inputs</c> include a revoked
/// artifact becomes suspect and must be flagged for re-verification. This scanner is a
/// pure function over record sets — callers (the quarantine flow, the digest job) decide
/// where the records come from and what "flag" means operationally.
/// </summary>
public static class RevocationChainScanner
{
    /// <summary>
    /// Records whose input hashes include <paramref name="revokedHash"/> — directly
    /// suspect — plus, transitively, records whose inputs include a suspect record's own
    /// content hash (a chain of certificates built on the revoked artifact).
    /// </summary>
    public static IReadOnlyList<CertificationRecordData> FindSuspects(
        IReadOnlyCollection<CertificationRecordData> records,
        string revokedHash)
    {
        if (records is null)
            throw new ArgumentNullException(nameof(records));
        if (string.IsNullOrWhiteSpace(revokedHash))
            throw new ArgumentException("Revoked hash is required.", nameof(revokedHash));

        var suspectHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { revokedHash };
        var suspects = new List<CertificationRecordData>();
        var remaining = new List<CertificationRecordData>(records);

        // Fixed-point sweep: each pass promotes records referencing any suspect hash and
        // adds their own content hashes to the suspect set, until nothing new turns up.
        bool grew;
        do
        {
            grew = false;
            for (var i = remaining.Count - 1; i >= 0; i--)
            {
                var record = remaining[i];
                var referencesSuspect = record.Inputs is { Count: > 0 }
                    && record.Inputs.Any(input =>
                        !string.IsNullOrWhiteSpace(input.Hash) && suspectHashes.Contains(input.Hash));
                if (!referencesSuspect)
                    continue;

                suspects.Add(record);
                remaining.RemoveAt(i);
                if (!string.IsNullOrWhiteSpace(record.ContentHash))
                    grew |= suspectHashes.Add(record.ContentHash!);
                grew = true;
            }
        } while (grew && remaining.Count > 0);

        return suspects;
    }
}
