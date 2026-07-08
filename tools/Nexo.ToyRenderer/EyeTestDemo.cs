using System.Text.Json;
using Nexo.VisualVerification;
using Nexo.VisualVerification.Providers;
using NSec.Cryptography;

namespace Nexo.ToyRenderer;

public static class EyeTestDemo
{
    public static async Task<int> RunAsync(string[] args)
    {
        var root = args.Length > 0
            ? args[0]
            : Path.Combine(Path.GetTempPath(), "nexo-eye-demo");

        Directory.CreateDirectory(root);
        var frameRoot = Path.Combine(root, "frames");
        var goldenRoot = Path.Combine(root, "goldens");
        Directory.CreateDirectory(frameRoot);
        Directory.CreateDirectory(goldenRoot);

        var scenario = DemoScenario();
        var build = new BuildArtifactRef("toy-renderer", ContentSha256.ComputeHex("nexo-toy-renderer-v1"));

        Console.WriteLine("=== Nexo Eye-Test Demo ===");
        Console.WriteLine($"Workspace: {root}");

        var display = new SimulatedDisplayProvider();
        var target = new ToyRendererTargetAppProvider(display);
        var harness = new EyeTestHarness(display, target, new EyeTestHarnessOptions { FrameStorageRoot = frameRoot });

        Console.WriteLine("Generating certified session (run A)...");
        var record = await harness.RunAsync(scenario, build, 160, 120).ConfigureAwait(false);
        Console.WriteLine($"Verdict: {record.Verdict.Tier}");

        var expectations = record.ToExpectations();
        var goldenPath = Path.Combine(goldenRoot, "demo.expectations.json");
        await File.WriteAllTextAsync(goldenPath, JsonSerializer.Serialize(expectations, CanonicalJson.Options)).ConfigureAwait(false);
        Console.WriteLine($"Expectations saved to {goldenPath}");

        var signingKey = CreateIssuerKey();
        var signed = EyeTestSessionRecordBuilder.Sign(record, signingKey.PrivateKey);
        Console.WriteLine($"RecordSha256: {signed.RecordSha256}");

        Console.WriteLine("Re-verifying against expectations...");
        var loaded = JsonSerializer.Deserialize<FrameExpectationSet>(
            await File.ReadAllTextAsync(goldenPath).ConfigureAwait(false),
            CanonicalJson.Options)
            ?? throw new InvalidOperationException("Failed to load expectations.");

        var verifyHarness = new EyeTestHarness(display, target, new EyeTestHarnessOptions
        {
            FrameStorageRoot = frameRoot,
            FrameExpectations = loaded
        });
        var reverified = await verifyHarness.RunAsync(scenario, build, 160, 120).ConfigureAwait(false);
        Console.WriteLine($"Re-verify verdict: {reverified.Verdict.Tier}");

        Console.WriteLine("Tampered verification (expected failure)...");
        var tampered = signed with { ScenarioHash = "tampered" };
        var trusted = SessionRecordVerifier.Verify(tampered, signingKey.PublicKey).Trusted;
        Console.WriteLine($"Tampered signature valid: {trusted}");

        var success = record.Verdict.Tier == VerdictTier.ExactMatch
            && reverified.Verdict.Tier == VerdictTier.ExactMatch
            && !trusted;

        Console.WriteLine(success ? "DEMO PASS" : "DEMO FAIL");
        return success ? 0 : 1;
    }

    private static ScenarioDefinition DemoScenario() =>
        new(
            Seed: 0xE7E57AUL,
            FixedTimestepSeconds: 1.0 / 60.0,
            TotalSteps: 10,
            InputScript: new[]
            {
                new ScriptedInputEvent(1, "spawn", string.Empty),
                new ScriptedInputEvent(5, "despawn", string.Empty),
                new ScriptedInputEvent(8, "pause", "on")
            });

    private static (byte[] PrivateKey, byte[] PublicKey) CreateIssuerKey()
    {
        using var key = Key.Create(
            SignatureAlgorithm.Ed25519,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
        return (key.Export(KeyBlobFormat.RawPrivateKey), key.Export(KeyBlobFormat.RawPublicKey));
    }
}
