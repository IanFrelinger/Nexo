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

    private static readonly JsonSerializerOptions PayloadOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Builds the canonical JSON payload used for signing and verification.
    /// Records without a <see cref="CertificationRecordData.SchemaVersion"/> use the
    /// legacy v1 payload byte-for-byte, so pre-trust-loop signatures stay valid.
    /// Versioned records sign the extended payload, which additionally covers
    /// <c>Gate</c>, the trust-loop evidence fields, and the Ed25519 public key.
    /// Both signature fields are structurally excluded. Mutant id lists and input
    /// entries are sorted for deterministic serialization; gate and attempt order
    /// is semantic and preserved.
    /// </summary>
    /// <param name="record">Record to serialize.</param>
    public static string BuildPayload(CertificationRecordData record)
    {
        if (record.SchemaVersion is null)
            return BuildLegacyPayload(record);

        var clone = new VersionedPayload(
            record.SchemaVersion.Value,
            record.Status,
            record.Stage,
            record.Admitted,
            record.Signed,
            record.Timestamp.UtcDateTime.ToString("O"),
            record.BrickId,
            record.ContentHash,
            record.EscapeRate,
            record.TotalMutants,
            record.SurvivingMutants,
            record.KilledMutants.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            record.SurvivingMutantIds.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            record.Reason,
            record.Gate,
            record.GatesPassed.Select(g => new GatePassPayload(g.Name, g.Version, g.Configuration)).ToArray(),
            record.Inputs
                .OrderBy(i => i.Kind, StringComparer.Ordinal)
                .ThenBy(i => i.Id, StringComparer.Ordinal)
                .Select(i => new InputPayload(i.Kind, i.Id, i.Hash))
                .ToArray(),
            record.Proposer is null
                ? null
                : new ProposerPayload(
                    record.Proposer.Identity,
                    record.Proposer.Parameters
                        .OrderBy(p => p.Key, StringComparer.Ordinal)
                        .ToDictionary(p => p.Key, p => p.Value),
                    record.Proposer.Seed),
            record.Attempts.Select(a => new AttemptPayload(a.Index, a.Outcome, a.FailureCategory, a.DurationSeconds)).ToArray(),
            record.Ed25519PublicKey);
        return JsonSerializer.Serialize(clone, PayloadOptions);
    }

    private static string BuildLegacyPayload(CertificationRecordData record)
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
        return JsonSerializer.Serialize(clone, PayloadOptions);
    }

    private sealed record VersionedPayload(
        int SchemaVersion,
        string Status,
        string Stage,
        bool Admitted,
        bool Signed,
        string Timestamp,
        string BrickId,
        string? ContentHash,
        double? EscapeRate,
        int? TotalMutants,
        int? SurvivingMutants,
        string[] KilledMutants,
        string[] SurvivingMutantIds,
        string? Reason,
        string? Gate,
        GatePassPayload[] GatesPassed,
        InputPayload[] Inputs,
        ProposerPayload? Proposer,
        AttemptPayload[] Attempts,
        string? Ed25519PublicKey);

    private sealed record GatePassPayload(string Name, string? Version, string? Configuration);

    private sealed record InputPayload(string Kind, string Id, string Hash);

    private sealed record ProposerPayload(string Identity, Dictionary<string, string> Parameters, string? Seed);

    private sealed record AttemptPayload(int Index, string Outcome, string? FailureCategory, double? DurationSeconds);

    private static string ResolveKey(string? hmacKey)
    {
        if (!string.IsNullOrWhiteSpace(hmacKey))
            return hmacKey!;
        return Environment.GetEnvironmentVariable("NEXO_CERT_DEV_HMAC_KEY") ?? DefaultDevKey;
    }
}
