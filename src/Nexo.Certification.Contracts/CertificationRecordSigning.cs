using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexo.Certification.Contracts;

/// <summary>
/// Canonical HMAC signing for certification records (shared by gate and external verifier).
/// </summary>
public static class CertificationRecordSigning
{
    public const string DefaultDevKey = "nexo-cert-dev-hmac-v0";

    public static string Sign(CertificationRecordData record, string? hmacKey = null)
    {
        var payload = BuildPayload(record);
        var keyBytes = Encoding.UTF8.GetBytes(ResolveKey(hmacKey));
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

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
