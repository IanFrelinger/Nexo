namespace Nexo.VisualVerification;

/// <summary>Builds signed eye-test session records from harness outputs.</summary>
public static class EyeTestSessionRecordBuilder
{
    public static EyeTestSessionRecord BuildUnsigned(
        ScenarioDefinition scenario,
        BuildArtifactRef build,
        DisplayConfig display,
        IReadOnlyList<CapturedFrame> frames,
        VerificationVerdict verdict)
    {
        var unsigned = new EyeTestSessionRecord(
            ScenarioHasher.ComputeHash(scenario),
            build.Sha256,
            display,
            frames.OrderBy(f => f.Step).ToArray(),
            verdict,
            string.Empty,
            Array.Empty<byte>());

        var recordSha256 = EyeTestSessionRecordSigning.ComputeRecordSha256(unsigned);
        return unsigned with { RecordSha256 = recordSha256 };
    }

    public static EyeTestSessionRecord Sign(
        EyeTestSessionRecord unsignedRecord,
        ReadOnlySpan<byte> issuerPrivateKey)
    {
        if (unsignedRecord.Verdict.Tier is not (VerdictTier.ExactMatch or VerdictTier.PerceptualMatch))
            throw new SigningRejectedException(unsignedRecord.Verdict.Tier);

        var signature = EyeTestSessionRecordSigning.Sign(unsignedRecord, issuerPrivateKey);
        return unsignedRecord with { Ed25519Signature = signature };
    }
}
