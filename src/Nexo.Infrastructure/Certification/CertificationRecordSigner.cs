using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Dev HMAC signer for certification records. Seam for real PKI later.
/// </summary>
public sealed class CertificationRecordSigner
{
    public const string DefaultDevKey = "nexo-cert-dev-hmac-v0";

    private readonly byte[] _keyBytes;

    public CertificationRecordSigner(string? hmacKey = null)
    {
        var key = string.IsNullOrWhiteSpace(hmacKey)
            ? Environment.GetEnvironmentVariable("NEXO_CERT_DEV_HMAC_KEY") ?? DefaultDevKey
            : hmacKey;
        _keyBytes = Encoding.UTF8.GetBytes(key);
    }

    public string Sign(CertificationRecord record)
    {
        var payload = BuildPayload(record);
        using var hmac = new HMACSHA256(_keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    public bool Verify(CertificationRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Signature))
            return false;

        try
        {
            var expected = Sign(record with { Signature = null });
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(record.Signature),
                Convert.FromBase64String(expected));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string BuildPayload(CertificationRecord record)
    {
        var clone = new
        {
            record.Status,
            record.Stage,
            record.Admitted,
            record.Signed,
            Timestamp = record.Timestamp.UtcDateTime.ToString("O"),
            record.BrickId,
            record.EscapeRate,
            record.TotalMutants,
            record.SurvivingMutants,
            KilledMutants = record.KilledMutants.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            SurvivingMutantIds = record.SurvivingMutantIds.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            record.Reason
        };
        return JsonSerializer.Serialize(clone, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
