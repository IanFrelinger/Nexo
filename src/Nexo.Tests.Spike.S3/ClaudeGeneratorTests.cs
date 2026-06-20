using FluentAssertions;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S3.Certification;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Generation.Claude;
using Nexo.Spike.S3.Loop;
using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Registry;
using Xunit;

namespace Nexo.Tests.Spike.S3;

public sealed class ClaudeGeneratorTests
{
    [Fact]
    public async Task Claude_generator_sets_isolation_enforced_provenance()
    {
        var workRoot = CreateTempWorkRoot();
        try
        {
            var transport = new RequestIsolationTests.FakeS3LlmTransport();
            var generator = new ClaudeSkillGenerator(
                transport,
                () => new ClaudeRuntimeConfig { ApiKey = "test-key", Model = "claude-test-model", Temperature = 0 },
                workRoot);

            var intent = new IntentDescriptor("csv-column-type-inference", ["live"], CapabilityKey: "csv-type-inference");
            var handoff = generator.Describe(intent);
            var candidate = await generator.IngestAsync(handoff);

            candidate.Provenance.Should().NotBeNull();
            candidate.Provenance!.IsolationEnforced.Should().BeTrue();
            candidate.Provenance.GeneratorBackend.Should().Be("claude");
            candidate.Provenance.ModelId.Should().Be("claude-test-model");
            transport.CallCount.Should().Be(1);
        }
        finally
        {
            TryDelete(workRoot);
        }
    }

    [Fact]
    public async Task Claude_loop_reuse_skips_second_transport_call()
    {
        var root = CreateTempRegistryRoot();
        var workRoot = CreateTempWorkRoot();
        try
        {
            var transport = new RequestIsolationTests.FakeS3LlmTransport();
            var generator = new ClaudeSkillGenerator(
                transport,
                () => new ClaudeRuntimeConfig { ApiKey = "test-key", Model = "claude-test-model", Temperature = 0 },
                workRoot);
            var registry = new SkillRegistry(root);
            var certification = new TrackingCertificationHarness();
            var loop = new SkillReuseLoop(registry, generator, certification);

            var intent = new IntentDescriptor("csv-column-type-inference", ["live"], CapabilityKey: "csv-type-inference");
            var first = await loop.EnsureSkillAsync(intent);
            var second = await loop.EnsureSkillAsync(intent);

            first.Outcome.Should().Be(EnsureSkillOutcome.Generated);
            first.IsolationEnforced.Should().BeTrue();
            second.Outcome.Should().Be(EnsureSkillOutcome.Reused);
            second.GenerationRan.Should().BeFalse();
            transport.CallCount.Should().Be(1);
            loop.GenerationCallCount.Should().Be(1);
        }
        finally
        {
            TryDelete(root);
            TryDelete(workRoot);
        }
    }

    [Fact]
    public void Transcript_redacts_api_key()
    {
        const string key = "sk-ant-test-redaction-key";
        var redacted = ClaudeRuntimeConfig.RedactSecrets($"Bearer {key}", new ClaudeRuntimeConfig { ApiKey = key });
        redacted.Should().NotContain(key);
        redacted.Should().Contain("[REDACTED_API_KEY]");
    }

    private static string CreateTempRegistryRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-s3-registry-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateTempWorkRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-s3-work-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    private sealed class TrackingCertificationHarness : ISkillCertificationHarness
    {
        public Task<CertificationResult> CertifyAsync(SkillCandidate candidate, CancellationToken ct = default)
        {
            var unsigned = new SignedCertificationRecord(
                Guid.NewGuid().ToString("N"),
                RedFailureReason.AssertionFailure,
                85,
                80,
                false,
                1.0,
                0,
                true,
                "s1.4-v1",
                "s1.4-v1",
                DateTimeOffset.UtcNow,
                string.Empty);

            return Task.FromResult(new CertificationResult(true, CertificationRecordSigner.WithSignature(unsigned), null));
        }
    }
}
