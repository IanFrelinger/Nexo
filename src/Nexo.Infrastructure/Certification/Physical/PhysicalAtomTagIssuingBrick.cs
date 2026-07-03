using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Nexo.Certification.Physical.Tagging;

namespace Nexo.Infrastructure.Certification.Physical;

public sealed record PhysicalAtomTagIssuingResult(
    bool Succeeded,
    PhysicalAtomTagReference? Reference,
    string? QrPayload,
    byte[]? NdefRecord,
    string? FailureCode,
    string? Reason)
{
    public static PhysicalAtomTagIssuingResult Ok(
        PhysicalAtomTagReference reference,
        string qrPayload,
        byte[] ndefRecord) =>
        new(true, reference, qrPayload, ndefRecord, null, null);

    public static PhysicalAtomTagIssuingResult Refused(string code, string reason) =>
        new(false, null, null, null, code, reason);
}

/// <summary>
/// Phase 2 deterministic tag issuer: encodes certified atom references for QR and NFC payloads.
/// </summary>
public static class PhysicalAtomTagIssuingBrick
{
    public static PhysicalAtomTagIssuingResult IssueFromBundle(
        PhysicalAtomCertBundle bundle,
        TagReferenceKind kind)
    {
        if (bundle is null)
            return PhysicalAtomTagIssuingResult.Refused("bundle-missing", "Physical atom cert bundle is required.");

        return IssueFromCert(bundle.Certificate, kind, bundle.IssuerPublicKey);
    }

    public static PhysicalAtomTagIssuingResult IssueFromCert(
        PhysicalAtomCertificate certificate,
        TagReferenceKind kind,
        ReadOnlySpan<byte> issuerPublicKey)
    {
        if (certificate is null)
            return PhysicalAtomTagIssuingResult.Refused("certificate-missing", "Physical atom certificate is required.");

        if (issuerPublicKey.IsEmpty)
            return PhysicalAtomTagIssuingResult.Refused("issuer-public-key-missing", "Issuer public key is required for tag fingerprint.");

        if (string.IsNullOrWhiteSpace(certificate.AssetHash) || certificate.AssetHash.Length != 64)
            return PhysicalAtomTagIssuingResult.Refused("asset-hash-invalid", "Certificate asset_hash must be a 64-character hex digest.");

        byte[] assetHashBytes;
        try
        {
            assetHashBytes = Convert.FromHexString(certificate.AssetHash);
        }
        catch (FormatException)
        {
            return PhysicalAtomTagIssuingResult.Refused("asset-hash-invalid", "Certificate asset_hash must be valid hex.");
        }

        if (assetHashBytes.Length != PhysicalAtomTagReference.AssetHashLength)
            return PhysicalAtomTagIssuingResult.Refused("asset-hash-invalid", "Certificate asset_hash must decode to 32 bytes.");

        var reference = new PhysicalAtomTagReference(
            kind,
            certificate.AtomId,
            assetHashBytes,
            certificate.AssetVersion,
            IssuerFingerprint.Compute(issuerPublicKey));

        var binary = PhysicalAtomTagBinaryCodec.Encode(reference);
        var qr = PhysicalAtomQrTagCodec.Encode(reference);
        var ndef = PhysicalAtomNfcNdefCodec.Encode(binary);

        return PhysicalAtomTagIssuingResult.Ok(reference, qr, ndef);
    }
}
