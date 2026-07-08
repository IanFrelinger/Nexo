using System.Text.Json;
using NSec.Cryptography;

namespace Nexo.VisualVerification;

/// <summary>Canonical Ed25519 signing for eye-test session records.</summary>
public static class EyeTestSessionRecordSigning
{
    private static readonly SignatureAlgorithm Algorithm = SignatureAlgorithm.Ed25519;

    public static byte[] Sign(EyeTestSessionRecord record, ReadOnlySpan<byte> issuerPrivateKey)
    {
        var payload = BuildSigningPayload(record);
        using var key = Key.Import(Algorithm, issuerPrivateKey, KeyBlobFormat.RawPrivateKey);
        return Algorithm.Sign(key, payload);
    }

    public static bool VerifySignature(EyeTestSessionRecord record, ReadOnlySpan<byte> issuerPublicKey)
    {
        if (record.Ed25519Signature is null || record.Ed25519Signature.Length == 0)
            return false;

        var payload = BuildSigningPayload(record);
        var publicKey = PublicKey.Import(Algorithm, issuerPublicKey, KeyBlobFormat.RawPublicKey);
        return Algorithm.Verify(publicKey, payload, record.Ed25519Signature);
    }

    public static byte[] BuildSigningPayload(EyeTestSessionRecord record) =>
        System.Text.Encoding.UTF8.GetBytes(BuildSigningPayloadJson(record));

    public static string ComputeRecordSha256(EyeTestSessionRecord record) =>
        ContentSha256.ComputeHex(BuildSigningPayloadJson(record));

    internal static string BuildSigningPayloadJson(EyeTestSessionRecord record)
    {
        var frames = record.Frames
            .OrderBy(f => f.Step)
            .Select(f => new SigningFrame(f.Step, f.Sha256, f.PerceptualHash, f.StoragePath))
            .ToArray();

        var payload = new SigningPayload(
            record.ScenarioHash,
            record.BuildArtifactSha256,
            record.Display,
            frames,
            new SigningVerdict(
                record.Verdict.Tier.ToString(),
                record.Verdict.MismatchedSteps.OrderBy(s => s).ToArray(),
                record.Verdict.WorstPerceptualDistance));

        return JsonSerializer.Serialize(payload, CanonicalJson.Options);
    }

    private sealed record SigningPayload(
        string ScenarioHash,
        string BuildArtifactSha256,
        DisplayConfig Display,
        IReadOnlyList<SigningFrame> Frames,
        SigningVerdict Verdict);

    private sealed record SigningFrame(int Step, string Sha256, string PerceptualHash, string StoragePath);

    private sealed record SigningVerdict(
        string Tier,
        IReadOnlyList<int> MismatchedSteps,
        double WorstPerceptualDistance);
}

/// <summary>Thrown when signing is rejected for a non-passing verdict.</summary>
public sealed class SigningRejectedException : Exception
{
    public SigningRejectedException(VerdictTier tier)
        : base($"Eye-test session records with verdict tier '{tier}' must not be signed as passing artifacts.")
    {
        Tier = tier;
    }

    public VerdictTier Tier { get; }
}
