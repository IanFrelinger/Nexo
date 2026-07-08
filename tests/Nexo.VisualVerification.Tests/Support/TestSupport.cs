using Nexo.VisualVerification;
using Nexo.VisualVerification.Providers;
using NSec.Cryptography;

namespace Nexo.VisualVerification.Tests.Support;

internal static class TestScenarioFactory
{
    public static ScenarioDefinition CreateDefault() =>
        new(
            Seed: 0xC0FFEEUL,
            FixedTimestepSeconds: 1.0 / 60.0,
            TotalSteps: 8,
            InputScript: new[]
            {
                new ScriptedInputEvent(2, "spawn", string.Empty),
                new ScriptedInputEvent(4, "despawn", string.Empty),
                new ScriptedInputEvent(6, "pause", "on")
            });

    public static ScenarioDefinition CreateAlternate() =>
        CreateDefault() with { Seed = 0xBEEFUL };

    public static BuildArtifactRef CreateBuildRef() =>
        new("toy-renderer", ContentSha256.ComputeHex("nexo-toy-renderer-v1"));
}

internal static class TestKeyMaterial
{
    private static readonly SignatureAlgorithm Algorithm = SignatureAlgorithm.Ed25519;

    public static readonly (byte[] PrivateKey, byte[] PublicKey) Issuer = CreateKeyPair();

    private static (byte[] PrivateKey, byte[] PublicKey) CreateKeyPair()
    {
        using var key = Key.Create(Algorithm, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
        return (key.Export(KeyBlobFormat.RawPrivateKey), key.Export(KeyBlobFormat.RawPublicKey));
    }
}

internal static class HarnessTestSupport
{
    public static async Task<EyeTestSessionRecord> RunCertifiedSessionAsync(
        ScenarioDefinition scenario,
        string storageRoot,
        FrameExpectationSet? expectations = null)
    {
        var display = new SimulatedDisplayProvider();
        var target = new ToyRenderer.ToyRendererTargetAppProvider(display);
        var harness = new EyeTestHarness(display, target, new EyeTestHarnessOptions
        {
            FrameStorageRoot = storageRoot,
            FrameExpectations = expectations
        });

        var record = await harness.RunAsync(scenario, TestScenarioFactory.CreateBuildRef(), 160, 120);
        if (record.Verdict.Tier is VerdictTier.ExactMatch or VerdictTier.PerceptualMatch)
            return EyeTestSessionRecordBuilder.Sign(record, TestKeyMaterial.Issuer.PrivateKey);

        return record;
    }
}
