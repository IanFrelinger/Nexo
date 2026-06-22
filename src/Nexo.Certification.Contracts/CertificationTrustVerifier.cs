namespace Nexo.Certification.Contracts;

/// <summary>
/// External consumer verifier: signature + content binding. No gate or generator required.
/// </summary>
public static class CertificationTrustVerifier
{
    public static CertificationTrustResult Verify(
        CertificationRecordData record,
        string brickSource,
        string? hmacKey = null)
    {
        if (!record.Admitted || !string.Equals(record.Status, "PASS", StringComparison.Ordinal))
            return Untrusted("record-not-admitted", "Certification record is not an admitted PASS.");

        if (!record.Signed)
            return Untrusted("record-unsigned", "Certification record is not signed.");

        if (string.IsNullOrWhiteSpace(record.ContentHash))
            return Untrusted("content-hash-missing", "Certification record has no content hash.");

        if (!CertificationRecordSigning.VerifySignature(record, hmacKey))
            return Untrusted("signature-invalid", "Certification record signature is invalid.");

        var actualHash = BrickContentHasher.ComputeSha256(brickSource);
        if (!string.Equals(actualHash, record.ContentHash, StringComparison.Ordinal))
        {
            return Untrusted(
                "content-hash-mismatch",
                $"Brick source hash does not match certified content (expected {record.ContentHash}, got {actualHash}).");
        }

        return new CertificationTrustResult(true, null, null);
    }

    private static CertificationTrustResult Untrusted(string code, string reason) =>
        new(false, code, reason);
}

public sealed record CertificationTrustResult(bool Trusted, string? FailureCode, string? Reason);
