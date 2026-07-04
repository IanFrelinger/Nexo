using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>HMAC signer for composition certification records.</summary>
public sealed class CompositionCertificationRecordSigner
{
    private readonly byte[] _keyBytes;

    /// <summary>Initializes a new composition certification record signer.</summary>
    public CompositionCertificationRecordSigner(CertificationRecordSigner? brickSigner = null)
    {
        _ = brickSigner;
        var key = Environment.GetEnvironmentVariable("NEXO_CERT_DEV_HMAC_KEY")
            ?? CertificationRecordSigner.DefaultDevKey;
        _keyBytes = Encoding.UTF8.GetBytes(key);
    }

    /// <summary>Sign.</summary>
    public string Sign(CompositionCertificationRecord record)
    {
        var payload = BuildPayload(record);
        using var hmac = new HMACSHA256(_keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    /// <summary>Verify.</summary>
    public bool Verify(CompositionCertificationRecord record)
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

    private static string BuildPayload(CompositionCertificationRecord record)
    {
        var clone = new
        {
            record.Status,
            record.Stage,
            record.Admitted,
            record.Signed,
            Timestamp = record.Timestamp.UtcDateTime.ToString("O"),
            record.CompositionId,
            record.CompositionEscapeRate,
            record.TotalStructuralMutants,
            record.SurvivingStructuralMutants,
            KilledStructuralMutantIds = record.KilledStructuralMutantIds.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            SurvivingStructuralMutantIds = record.SurvivingStructuralMutantIds.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            record.Reason
        };
        return JsonSerializer.Serialize(clone, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}
