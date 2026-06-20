using FluentAssertions;
using Nexo.Spike.S0.Models;
using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Registry;
using Xunit;

namespace Nexo.Tests.Spike.S3;

public sealed class SkillRegistryAdmissionTests
{
    [Fact]
    public void Admit_rejects_candidate_failing_promotion_contract_and_leaves_registry_unchanged()
    {
        var root = CreateTempRegistryRoot();
        try
        {
            var registry = new SkillRegistry(root);
            var candidate = BuildCandidate();
            var invalidRecord = BuildValidRecord() with
            {
                IntentDensity = 0.5,
                Signature = "invalid-signature"
            };

            var result = registry.Admit(candidate, invalidRecord);

            result.Status.Should().Be(AdmissionStatus.Rejected);
            result.Entry.Should().BeNull();
            result.RejectionReason.Should().NotBeNullOrWhiteSpace();
            registry.ListEntries().Should().BeEmpty();
            Directory.Exists(RegistryPaths.EntryDirectory(root, "csv-type-inference")).Should().BeFalse();
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public void Admit_rejects_record_with_invalid_signature()
    {
        var root = CreateTempRegistryRoot();
        try
        {
            var registry = new SkillRegistry(root);
            var candidate = BuildCandidate();
            var tampered = BuildValidRecord() with { MutationScore = 99 };

            var result = registry.Admit(candidate, tampered);

            result.Status.Should().Be(AdmissionStatus.Rejected);
            result.RejectionReason.Should().Contain("signature");
            registry.ListEntries().Should().BeEmpty();
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public void Admit_writes_entry_when_promotion_contract_satisfied()
    {
        var root = CreateTempRegistryRoot();
        try
        {
            var registry = new SkillRegistry(root);
            var candidate = BuildCandidate();
            var record = BuildValidRecord();

            var result = registry.Admit(candidate, record);

            result.Status.Should().Be(AdmissionStatus.Admitted);
            result.Entry.Should().NotBeNull();
            registry.ListEntries().Should().HaveCount(1);
            File.Exists(RegistryPaths.EntryFilePath(root, "csv-type-inference")).Should().BeTrue();
            File.Exists(RegistryPaths.ImplementationFilePath(root, "csv-type-inference")).Should().BeTrue();
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static SkillCandidate BuildCandidate() =>
        new(
            "test-candidate",
            new IntentDescriptor("csv-column-type-inference", ["core"], CapabilityKey: "csv-type-inference"),
            "/tmp/intent.json",
            "public static class Impl {}",
            "test",
            "test hypothesis");

    private static SignedCertificationRecord BuildValidRecord()
    {
        var unsigned = new SignedCertificationRecord(
            "record-1",
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

        return CertificationRecordSigner.WithSignature(unsigned);
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
}
