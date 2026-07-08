using FluentAssertions;
using Nexo.VisualVerification;
using Nexo.VisualVerification.Providers;
using Nexo.VisualVerification.Tests.Support;
using Xunit;

namespace Nexo.VisualVerification.Tests;

[Trait("Category", "VisualVerification")]
public sealed class EyeTestRejectionTests
{
    [Fact]
    public async Task R1_TamperedFrameBytes_RecordVerificationFails()
    {
        var storageRoot = CreateTempDir();
        var record = await HarnessTestSupport.RunCertifiedSessionAsync(TestScenarioFactory.CreateDefault(), storageRoot);
        var frame = record.Frames[0];
        var bytes = File.ReadAllBytes(frame.StoragePath);
        bytes[0] ^= 0xFF;
        File.WriteAllBytes(frame.StoragePath, bytes);

        var result = SessionRecordVerifier.VerifyFrameBytes(
            record,
            TestKeyMaterial.Issuer.PublicKey,
            new FrameContentStore(storageRoot));

        result.Trusted.Should().BeFalse();
        result.FailureCode.Should().Be("frame-bytes-tampered");
    }

    [Fact]
    public async Task R2_WrongScenarioAgainstGolden_IsMismatchNeverExactMatch()
    {
        var storageRoot = CreateTempDir();
        var goldenScenario = TestScenarioFactory.CreateDefault();
        var goldenRecord = await HarnessTestSupport.RunCertifiedSessionAsync(goldenScenario, storageRoot);
        goldenRecord.Verdict.Tier.Should().Be(VerdictTier.ExactMatch);

        var golden = EyeTestExpectations.FromSessionRecord(goldenRecord);
        var display = new SimulatedDisplayProvider();
        var target = new ToyRenderer.ToyRendererTargetAppProvider(display);
        var harness = new EyeTestHarness(display, target, new EyeTestHarnessOptions
        {
            FrameStorageRoot = storageRoot,
            FrameExpectations = golden.ToExpectations()
        });

        var wrongScenario = TestScenarioFactory.CreateAlternate();
        var result = await harness.RunAsync(wrongScenario, TestScenarioFactory.CreateBuildRef(), 160, 120);

        result.Verdict.Tier.Should().Be(VerdictTier.Mismatch);
        result.Verdict.Tier.Should().NotBe(VerdictTier.ExactMatch);
    }

    [Fact]
    public async Task R3_NondeterministicTarget_VerdictIsNonDeterministicAndSigningThrows()
    {
        var storageRoot = CreateTempDir();
        var display = new SimulatedDisplayProvider();
        var target = new NondeterministicToyRendererTargetAppProvider(display);
        var harness = new EyeTestHarness(display, target, new EyeTestHarnessOptions { FrameStorageRoot = storageRoot });

        var record = await harness.RunAsync(TestScenarioFactory.CreateDefault(), TestScenarioFactory.CreateBuildRef(), 160, 120);

        record.Verdict.Tier.Should().Be(VerdictTier.NonDeterministic);
        var act = () => EyeTestSessionRecordBuilder.Sign(record, TestKeyMaterial.Issuer.PrivateKey);
        act.Should().Throw<SigningRejectedException>();
    }

    [Fact]
    public async Task R4_HollowCapture_AllIdenticalBlankFrames_RejectedAsCaptureFailure()
    {
        var storageRoot = CreateTempDir();
        var display = new HollowCaptureDisplayProvider();
        var target = new NoOpTargetAppProvider();
        var harness = new EyeTestHarness(display, target, new EyeTestHarnessOptions { FrameStorageRoot = storageRoot });

        var record = await harness.RunAsync(TestScenarioFactory.CreateDefault(), TestScenarioFactory.CreateBuildRef(), 64, 64);

        record.Verdict.Tier.Should().Be(VerdictTier.CaptureFailure);
    }

    [Fact]
    public void R5_GoldenIsolation_ProvidersAndToyRendererDoNotReferenceGoldenStore()
    {
        var forbidden = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(GoldenRecordStore),
            nameof(GoldenEyeTestRecord),
            nameof(GoldenFrameFingerprint)
        };

        var assemblies = new[]
        {
            typeof(SimulatedDisplayProvider).Assembly,
            typeof(ToyRenderer.ToyRenderer).Assembly
        };

        var violations = new List<string>();
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (forbidden.Contains(type.Name))
                    violations.Add($"{assembly.GetName().Name}:{type.FullName}");
            }

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name?.Contains("Golden", StringComparison.OrdinalIgnoreCase) == true)
                    violations.Add($"{assembly.GetName().Name} references {reference.Name}");
            }
        }

        var repoRoot = RepoPaths.FindRepositoryRoot();
        var providerSources = Directory.GetFiles(
            Path.Combine(repoRoot, "src", "Nexo.VisualVerification.Providers"),
            "*.cs",
            SearchOption.AllDirectories);
        var toySources = Directory.GetFiles(
            Path.Combine(repoRoot, "tools", "Nexo.ToyRenderer"),
            "*.cs",
            SearchOption.AllDirectories);

        foreach (var source in providerSources.Concat(toySources))
        {
            var text = File.ReadAllText(source);
            foreach (var token in forbidden)
            {
                if (text.Contains(token, StringComparison.Ordinal))
                    violations.Add($"{source} mentions {token}");
            }
        }

        violations.Should().BeEmpty("golden storage must remain witness-only: {0}", string.Join(", ", violations));
    }

    [Fact]
    public async Task R6_SignatureBinding_FieldMutation_InvalidatesSignature()
    {
        var storageRoot = CreateTempDir();
        var record = await HarnessTestSupport.RunCertifiedSessionAsync(TestScenarioFactory.CreateDefault(), storageRoot);

        var tamperedScenario = record with { ScenarioHash = "deadbeef" };
        SessionRecordVerifier.Verify(tamperedScenario, TestKeyMaterial.Issuer.PublicKey).Trusted.Should().BeFalse();

        var tamperedFrame = record with
        {
            Frames = record.Frames.Select((f, i) => i == 0 ? f with { Sha256 = "deadbeef" } : f).ToArray()
        };
        SessionRecordVerifier.Verify(tamperedFrame, TestKeyMaterial.Issuer.PublicKey).Trusted.Should().BeFalse();

        var tamperedDisplay = record with
        {
            Display = record.Display with { Width = record.Display.Width + 1 }
        };
        SessionRecordVerifier.Verify(tamperedDisplay, TestKeyMaterial.Issuer.PublicKey).Trusted.Should().BeFalse();
    }

    [Fact]
    public void R7_PerceptualEpsilonBoundary_AtEpsilonPasses_OverEpsilonMismatch()
    {
        const int epsilon = PerceptualHash.DefaultEpsilon;
        var baseHash = new string('0', 16);
        var flipped = FlipBits(baseHash, epsilon);
        var over = FlipBits(baseHash, epsilon + 1);

        PerceptualHash.HammingDistance(baseHash, flipped).Should().Be(epsilon);
        PerceptualHash.HammingDistance(baseHash, over).Should().Be(epsilon + 1);

        var golden = new GoldenEyeTestRecord(
            "scenario",
            "build",
            new DisplayConfig(1, 1, PixelFormats.Rgba8888, "simulated"),
            new[] { new GoldenFrameFingerprint(0, "a", baseHash) });

        var atEpsilon = new[]
        {
            new CapturedFrame(0, "b", flipped, "path")
        };
        VerdictComputer.CompareToGolden(atEpsilon, golden, epsilon).Tier.Should().Be(VerdictTier.PerceptualMatch);

        var aboveEpsilon = new[]
        {
            new CapturedFrame(0, "c", over, "path")
        };
        VerdictComputer.CompareToGolden(aboveEpsilon, golden, epsilon).Tier.Should().Be(VerdictTier.Mismatch);
    }

    [Fact]
    public void CoreAssembly_ContainsNoEngineSpecificIdentifiers()
    {
        var forbidden = new[] { "Unity", "Unreal", "Godot", "ARKit", "XREAL", "VisionPro" };
        var coreDir = Path.Combine(RepoPaths.FindRepositoryRoot(), "src", "Nexo.VisualVerification");
        var sources = Directory.GetFiles(coreDir, "*.cs", SearchOption.AllDirectories);
        var hits = sources
            .SelectMany(path => forbidden.Select(token => (path, token)))
            .Where(pair => File.ReadAllText(pair.path).Contains(pair.token, StringComparison.Ordinal))
            .Select(pair => $"{pair.path}:{pair.token}")
            .ToArray();

        hits.Should().BeEmpty("core harness must remain engine-agnostic: {0}", string.Join(", ", hits));
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-eye-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string FlipBits(string hex, int count)
    {
        var value = Convert.ToUInt64(hex, 16);
        for (var i = 0; i < count; i++)
            value ^= 1UL << i;
        return value.ToString("x16");
    }
}
