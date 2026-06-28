using FluentAssertions;
using Nexo.Certification.State;
using Nexo.Infrastructure.Certification;
using Nexo.Tests.Infrastructure.Certification.Reuse;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class CrossProjectStateLogReuseTests
{
    private const string HmacKey = "attested-state-log-test-hmac";

    [Fact]
    public void HonestAttestedStateLog_ProjectC_TrustsTier1()
    {
        var projectCcsproj = Path.Combine(
            RepoPaths.FindRepoRoot(),
            "samples", "certified-state-log-reuse", "ProjectC", "ProjectC.csproj");
        ProjectCStateLogConsumer.AssertProjectCProjectHasNoGateOrInfrastructureReferences(projectCcsproj);

        var result = ProjectCStateLogConsumer.VerifyBundledArtifacts(PhaseWitnessPaths.ArtifactRoot(), HmacKey);

        result.Trusted.Should().BeTrue($"expected TRUSTED, got {result.FailureCode}: {result.Reason}");
        result.VerifiedTransitions.Should().Be(3);
    }

    [Fact]
    public void TamperedAttestedStateLog_ProjectC_RefusesChainBreak()
    {
        var artifactRoot = PhaseWitnessPaths.ArtifactRoot();
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-state-log-tamper-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            CopyDirectory(artifactRoot, tempDir);
            var logPath = PhaseWitnessPaths.LogPath(tempDir);
            var json = File.ReadAllText(logPath).Replace(
                "\"prevEntryHash\": \"\"",
                "\"prevEntryHash\": \"tampered-prev-entry\"",
                StringComparison.Ordinal);

            File.WriteAllText(logPath, json);

            var result = ProjectCStateLogConsumer.VerifyBundledArtifacts(tempDir, HmacKey);

            result.Trusted.Should().BeFalse();
            result.FailureCode.Should().Be("chain-prev-entry-break");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ForgedBehaviorCert_ProjectC_Refuses()
    {
        var artifactRoot = PhaseWitnessPaths.ArtifactRoot();
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-state-log-forged-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            CopyDirectory(artifactRoot, tempDir);
            var behaviorRoot = PhaseWitnessPaths.BehaviorRoot(tempDir);
            var recordPath = Path.Combine(Directory.GetDirectories(behaviorRoot).First(), "record.json");
            var record = System.Text.Json.JsonSerializer.Deserialize<Nexo.Certification.Contracts.CertificationRecordData>(
                File.ReadAllText(recordPath),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            record = record with { Signature = Convert.ToBase64String(new byte[32]) };
            File.WriteAllText(
                recordPath,
                System.Text.Json.JsonSerializer.Serialize(record, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                }));

            var result = ProjectCStateLogConsumer.VerifyBundledArtifacts(tempDir, HmacKey);

            result.Trusted.Should().BeFalse();
            result.FailureCode.Should().Be("behavior-cert-untrusted");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void HonestAttestedStateLog_ProjectC_TrustsTier3WithLiveSidecar()
    {
        var result = ProjectCStateLogConsumer.VerifyBundledArtifactsWithLiveSidecar(
            PhaseWitnessPaths.ArtifactRoot(),
            HmacKey);

        result.Trusted.Should().BeTrue($"expected TRUSTED, got {result.FailureCode}: {result.Reason}");
        result.VerifiedTransitions.Should().Be(3);
    }

    [Fact]
    public void TamperedLiveStateSidecar_ProjectC_RefusesLiveMismatch()
    {
        var artifactRoot = PhaseWitnessPaths.ArtifactRoot();
        var tempDir = Path.Combine(Path.GetTempPath(), "nexo-live-state-tamper-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            CopyDirectory(artifactRoot, tempDir);
            var schema = AttestedStateLogWireFormat.DeserializeSchema(
                File.ReadAllText(PhaseWitnessPaths.SchemaPath(tempDir)));
            var wrongHash = schema.ComputeBoundStateHash("phase:armed");
            File.WriteAllText(
                PhaseWitnessPaths.LiveStatePath(tempDir),
                AttestedStateLogWireFormat.SerializeLiveState(new LiveStateSnapshot(wrongHash)));

            var result = ProjectCStateLogConsumer.VerifyBundledArtifactsWithLiveSidecar(tempDir, HmacKey);

            result.Trusted.Should().BeFalse();
            result.FailureCode.Should().Be("live-state-mismatch");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = file.Replace(source, destination, StringComparison.Ordinal);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
