using Nexo.Certification.Physical;
using Nexo.Certification.Physical.Resolution;
using Nexo.Certification.Physical.Tagging;

namespace Nexo.Infrastructure.Certification.Physical;

/// <summary>
/// Phase 2 deterministic tag issuer: encodes certified atom references for QR and NFC payloads.
/// </summary>
public static class PhysicalAtomTagIssuingBrick
{
    /// <summary>Whether sue from bundle.</summary>
    public static PhysicalAtomTagIssuingResult IssueFromBundle(
        PhysicalAtomCertBundle bundle,
        TagReferenceKind kind)
    {
        if (bundle is null)
            return PhysicalAtomTagIssuingResult.Refused("bundle-missing", "Physical atom cert bundle is required.");

        return IssueFromCert(bundle.Certificate, kind, bundle.IssuerPublicKey);
    }

    /// <summary>Whether sue from cert.</summary>
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
