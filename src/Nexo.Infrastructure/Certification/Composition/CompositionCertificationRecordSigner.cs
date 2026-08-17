using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Certification.Models;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// HMAC signer for composition certification records. Resolves its key exactly as
/// <see cref="CertificationRecordSigner"/> does (<c>NEXO_CERT_DEV_HMAC_KEY</c>, else the
/// committed dev key) and warns the same way while the dev key is in effect.
/// </summary>
public sealed class CompositionCertificationRecordSigner
{
    private readonly byte[] _keyBytes;

    /// <summary>Initializes a new composition certification record signer.</summary>
    /// <param name="brickSigner">Unused; kept so existing composition wiring compiles unchanged.</param>
    /// <param name="logger">Optional logger; receives the dev-key warning when the committed key is in effect.</param>
    public CompositionCertificationRecordSigner(
        CertificationRecordSigner? brickSigner = null,
        ILogger<CompositionCertificationRecordSigner>? logger = null)
    {
        _ = brickSigner;
        var key = Environment.GetEnvironmentVariable(CertificationRecordSigning.HmacKeyEnvVar)
            ?? CertificationRecordSigner.DefaultDevKey;
        _keyBytes = Encoding.UTF8.GetBytes(key);
        UsesDevKey = CertificationRecordSigning.UsesDevKey();
        if (UsesDevKey)
            CertificationRecordSigner.WarnDevKey(logger, nameof(CompositionCertificationRecordSigner));
    }

    /// <summary>
    /// True when composition records are signed with the committed development key
    /// (<c>NEXO_CERT_DEV_HMAC_KEY</c> unset): every signature this instance mints or accepts is
    /// forgeable by anyone with the source.
    /// </summary>
    public bool UsesDevKey { get; }

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
