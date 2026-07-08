namespace Nexo.VisualVerification;

/// <summary>Verifies record integrity and Ed25519 binding.</summary>
public static class SessionRecordVerifier
{
    public sealed record VerifyResult(bool Trusted, string? FailureCode);

    public static VerifyResult Verify(EyeTestSessionRecord record, ReadOnlySpan<byte> issuerPublicKey)
    {
        var expectedRecordSha256 = EyeTestSessionRecordSigning.ComputeRecordSha256(record);
        if (!string.Equals(record.RecordSha256, expectedRecordSha256, StringComparison.Ordinal))
            return new VerifyResult(false, "record-sha256-mismatch");

        if (!EyeTestSessionRecordSigning.VerifySignature(record, issuerPublicKey))
            return new VerifyResult(false, "signature-invalid");

        return new VerifyResult(true, null);
    }

    public static VerifyResult VerifyFrameBytes(
        EyeTestSessionRecord record,
        ReadOnlySpan<byte> issuerPublicKey,
        FrameContentStore frameStore)
    {
        var baseResult = Verify(record, issuerPublicKey);
        if (!baseResult.Trusted)
            return baseResult;

        foreach (var frame in record.Frames)
        {
            var bytes = frameStore.Load(frame.StoragePath);
            var actualHash = ContentSha256.ComputeHex(bytes);
            if (!string.Equals(actualHash, frame.Sha256, StringComparison.Ordinal))
                return new VerifyResult(false, "frame-bytes-tampered");
        }

        return new VerifyResult(true, null);
    }
}
