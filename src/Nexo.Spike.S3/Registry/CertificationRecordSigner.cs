using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Registry;

/// <summary>
/// Offline deterministic signer for registry certification records (no secrets, no network).
/// </summary>
public static class CertificationRecordSigner
{
    public const string OfflineSigningMaterial = "nexo-spike-s3-offline-registry-v1";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Sign(SignedCertificationRecord record)
    {
        var payload = BuildPayload(record);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload + OfflineSigningMaterial));
        return Convert.ToBase64String(bytes);
    }

    public static bool Verify(SignedCertificationRecord record) =>
        string.Equals(Sign(record), record.Signature, StringComparison.Ordinal);

    public static SignedCertificationRecord WithSignature(SignedCertificationRecord record)
    {
        var unsigned = record with { Signature = string.Empty };
        return unsigned with { Signature = Sign(unsigned) };
    }

    private static string BuildPayload(SignedCertificationRecord record)
    {
        var clone = record with { Signature = string.Empty };
        return JsonSerializer.Serialize(clone, SerializerOptions);
    }
}
