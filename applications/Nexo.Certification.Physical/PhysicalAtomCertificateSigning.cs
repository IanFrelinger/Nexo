using System.Text.Json;
using System.Text.Json.Serialization;
using NSec.Cryptography;

namespace Nexo.Certification.Physical;

/// <summary>
/// Canonical Ed25519 signing for physical-atom certificates.
/// </summary>
public static class PhysicalAtomCertificateSigning
{
    private static readonly SignatureAlgorithm Algorithm = SignatureAlgorithm.Ed25519;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public static string Sign(PhysicalAtomCertificate certificate, ReadOnlySpan<byte> issuerPrivateKey)
    {
        var payload = BuildSigningPayload(certificate);
        using var key = Key.Import(Algorithm, issuerPrivateKey, KeyBlobFormat.RawPrivateKey);
        var signature = Algorithm.Sign(key, payload);
        return Convert.ToBase64String(signature);
    }

    public static bool VerifySignature(PhysicalAtomCertificate certificate, ReadOnlySpan<byte> issuerPublicKey)
    {
        if (string.IsNullOrWhiteSpace(certificate.IssuerSignature))
            return false;

        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(certificate.IssuerSignature);
        }
        catch (FormatException)
        {
            return false;
        }

        var payload = BuildSigningPayload(certificate);
        var publicKey = PublicKey.Import(Algorithm, issuerPublicKey, KeyBlobFormat.RawPublicKey);
        return Algorithm.Verify(publicKey, payload, signatureBytes);
    }

    public static byte[] BuildSigningPayload(PhysicalAtomCertificate certificate) =>
        EncodingCompat.Utf8.GetBytes(BuildSigningPayloadJson(certificate));

    internal static string BuildSigningPayloadJson(PhysicalAtomCertificate certificate)
    {
        var extensions = certificate.Extensions
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToDictionary(
                entry => entry.Key,
                entry => Convert.ToBase64String(entry.Value),
                StringComparer.Ordinal);

        var payload = new SigningPayload(
            certificate.SchemaVersion,
            certificate.Maturity.ToString(),
            certificate.AtomId,
            certificate.BindingScope.ToString(),
            certificate.AssetHash,
            certificate.AssetVersion,
            certificate.GeoAnchor,
            certificate.ManufactureMeta,
            extensions);

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private sealed record SigningPayload(
        int SchemaVersion,
        string Maturity,
        Guid AtomId,
        string BindingScope,
        string AssetHash,
        string AssetVersion,
        GeoAnchor? GeoAnchor,
        ManufactureMeta? ManufactureMeta,
        IReadOnlyDictionary<string, string> Extensions);
}
