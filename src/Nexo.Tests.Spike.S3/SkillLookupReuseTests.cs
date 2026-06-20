using FluentAssertions;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S3.Certification;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Loop;
using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Registry;
using Xunit;

namespace Nexo.Tests.Spike.S3;

public sealed class SkillLookupReuseTests
{
    [Fact]
    public async Task Lookup_hit_returns_stored_skill_and_skips_generation_and_certification()
    {
        var root = CreateTempRegistryRoot();
        try
        {
            var registry = new SkillRegistry(root);
            var generator = new TrackingStandInGenerator();
            var certification = new TrackingCertificationHarness();
            var loop = new SkillReuseLoop(registry, generator, certification);

            var intent = new IntentDescriptor("csv-column-type-inference", ["core"], CapabilityKey: "csv-type-inference");
            SeedAdmittedEntry(registry, intent);

            var result = await loop.EnsureSkillAsync(intent);

            result.Outcome.Should().Be(EnsureSkillOutcome.Reused);
            result.Reused.Should().BeTrue();
            result.GenerationRan.Should().BeFalse();
            result.CertificationRan.Should().BeFalse();
            result.Entry.Should().NotBeNull();
            result.Entry!.EntryId.Should().Be("csv-type-inference");
            generator.GenerationCount.Should().Be(0);
            certification.InvocationCount.Should().Be(0);
            loop.GenerationCount.Should().Be(0);
            loop.CertificationInvocationCount.Should().Be(0);
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public async Task Same_intent_second_call_reuses_with_generation_count_one_across_two_calls()
    {
        var root = CreateTempRegistryRoot();
        try
        {
            var registry = new SkillRegistry(root);
            var generator = new TrackingStandInGenerator();
            var certification = new TrackingCertificationHarness();
            var loop = new SkillReuseLoop(registry, generator, certification);

            var intent = new IntentDescriptor("csv-column-type-inference", ["core"], CapabilityKey: "csv-type-inference");

            var first = await loop.EnsureSkillAsync(intent);
            var second = await loop.EnsureSkillAsync(intent);

            first.Outcome.Should().Be(EnsureSkillOutcome.Generated);
            second.Outcome.Should().Be(EnsureSkillOutcome.Reused);
            loop.GenerationCount.Should().Be(1);
            loop.CertificationInvocationCount.Should().Be(1);
            generator.GenerationCount.Should().Be(1);
            certification.InvocationCount.Should().Be(1);
            registry.ListEntries().Should().HaveCount(1);
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public async Task Rejected_candidate_does_not_write_registry_entry()
    {
        var root = CreateTempRegistryRoot();
        try
        {
            var registry = new SkillRegistry(root);
            var generator = new TrackingStandInGenerator();
            var certification = new TrackingCertificationHarness(alwaysPass: false);
            var loop = new SkillReuseLoop(registry, generator, certification);

            var intent = new IntentDescriptor("csv-constant-type-guess", ["constant-guess"], CapabilityKey: "csv-constant-guess");
            var result = await loop.EnsureSkillAsync(intent);

            result.Outcome.Should().Be(EnsureSkillOutcome.Rejected);
            result.GenerationRan.Should().BeTrue();
            result.CertificationRan.Should().BeTrue();
            registry.ListEntries().Should().BeEmpty();
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static void SeedAdmittedEntry(SkillRegistry registry, IntentDescriptor intent)
    {
        var candidate = new SkillCandidate(
            "seed-candidate",
            intent,
            "/tmp/intent.json",
            "namespace CsvColumnInferrer; public static class ColumnTypeInferrer {}",
            ScriptedStandInSkillGenerator.BackendLabel,
            "seed");

        var unsigned = new SignedCertificationRecord(
            "seed-record",
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

        var record = CertificationRecordSigner.WithSignature(unsigned);
        registry.Admit(candidate, record).Status.Should().Be(AdmissionStatus.Admitted);
    }

    private static string CreateTempRegistryRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nexo-s3-registry-{Guid.NewGuid():N}");
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

    private sealed class TrackingStandInGenerator : IStandInSkillGenerator
    {
        private int _count;

        public string BackendName => ScriptedStandInSkillGenerator.BackendLabel;

        public int GenerationCount => _count;

        public SkillCandidate Generate(IntentDescriptor intent)
        {
            _count++;
            return new SkillCandidate(
                "tracking-candidate",
                intent,
                "/tmp/intent.json",
                "namespace CsvColumnInferrer; public static class ColumnTypeInferrer {}",
                BackendName,
                "tracking");
        }
    }

    private sealed class TrackingCertificationHarness : ISkillCertificationHarness
    {
        private readonly bool _alwaysPass;
        private int _count;

        public TrackingCertificationHarness(bool alwaysPass = true) => _alwaysPass = alwaysPass;

        public int InvocationCount => _count;

        public Task<CertificationResult> CertifyAsync(SkillCandidate candidate, CancellationToken ct = default)
        {
            _count++;
            if (!_alwaysPass)
            {
                return Task.FromResult(new CertificationResult(false, null, "forced certification failure"));
            }

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
