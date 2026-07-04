using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexo.Certification.Contracts;

/// <summary>
/// Canonical HMAC signing for certification records (shared by gate and external verifier).
/// </summary>
public static class CertificationRecordSigning
{
    /// <summary>Development-only default HMAC key; override via <c>NEXO_CERT_DEV_HMAC_KEY</c> in production.</summary>
    public const string DefaultDevKey = "nexo-cert-dev-hmac-v0";

    /// <summary>
    /// Computes the Base64 HMAC-SHA256 signature for a certification record.
    /// The signature field on <paramref name="record"/> is excluded from the payload.
    /// </summary>
    /// <param name="record">Record to sign.</param>
    /// <param name="hmacKey">Optional explicit key; falls back to environment or <see cref="DefaultDevKey"/>.</param>
    public static string Sign(CertificationRecordData record, string? hmacKey = null)
    {
        var payload = BuildPayload(record);
        var keyBytes = Encoding.UTF8.GetBytes(ResolveKey(hmacKey));
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifies the record's <see cref="CertificationRecordData.Signature"/> against the canonical payload.
    /// Returns false when the signature is missing, malformed, or does not match.
    /// </summary>
    /// <param name="record">Record containing the signature to verify.</param>
    /// <param name="hmacKey">Optional explicit key; falls back to environment or <see cref="DefaultDevKey"/>.</param>
    public static bool VerifySignature(CertificationRecordData record, string? hmacKey = null)
    {
        if (string.IsNullOrWhiteSpace(record.Signature))
            return false;

        try
        {
            var expected = Sign(record with { Signature = null }, hmacKey);
            return FixedTimeEquals(
                Convert.FromBase64String(record.Signature),
                Convert.FromBase64String(expected));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
#if NET5_0_OR_GREATER
        return CryptographicOperations.FixedTimeEquals(left, right);
#else
        if (left.Length != right.Length)
            return false;
        var diff = 0;
        for (var i = 0; i < left.Length; i++)
            diff |= left[i] ^ right[i];
        return diff == 0;
#endif
    }

    /// <summary>
    /// Builds the canonical JSON payload used for signing and verification.
    /// Mutant id lists are sorted for deterministic serialization.
    /// </summary>
    /// <param name="record">Record to serialize.</param>
    public static string BuildPayload(CertificationRecordData record)
    {
        var clone = new
        {
            record.Status,
            record.Stage,
            record.Admitted,
            record.Signed,
            Timestamp = record.Timestamp.UtcDateTime.ToString("O"),
            record.BrickId,
            record.ContentHash,
            record.EscapeRate,
            record.TotalMutants,
            record.SurvivingMutants,
            KilledMutants = record.KilledMutants.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            SurvivingMutantIds = record.SurvivingMutantIds.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            record.Reason
        };
        return JsonSerializer.Serialize(clone, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private static string ResolveKey(string? hmacKey)
    {
        if (!string.IsNullOrWhiteSpace(hmacKey))
            return hmacKey!;
        return Environment.GetEnvironmentVariable("NEXO_CERT_DEV_HMAC_KEY") ?? DefaultDevKey;
    }
}
