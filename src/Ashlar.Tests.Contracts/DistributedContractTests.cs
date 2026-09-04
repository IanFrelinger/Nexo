using System.Text.Json;
using FluentAssertions;
using Ashlar.Contracts.Distributed;
using Xunit;

namespace Ashlar.Tests.Contracts;

public sealed class DistributedContractTests
{
    [Fact]
    public void ExecutionEnvelope_create_trims_and_rejects_blank_fields()
    {
        var envelope = ExecutionEnvelope.Create(
            " env-1 ",
            " node-a ",
            ExecutionTarget.Peer,
            " brick.execute ",
            " sha256:abc ",
            " pack-1 ",
            DateTimeOffset.Parse("2026-09-04T12:00:00Z"),
            allowedCapabilities: new[] { "fs.read" },
            maxDuration: TimeSpan.FromSeconds(30));

        envelope.EnvelopeId.Should().Be("env-1");
        envelope.SourceNodeId.Should().Be("node-a");
        envelope.WorkloadKind.Should().Be("brick.execute");
        envelope.PayloadHash.Should().Be("sha256:abc");
        envelope.PolicyPackId.Should().Be("pack-1");
        envelope.AllowedCapabilities.Should().Equal("fs.read");

        var act = () => ExecutionEnvelope.Create(
            "", "n", ExecutionTarget.Local, "w", "h", "p", DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();

        var blankCap = () => ExecutionEnvelope.Create(
            "e", "n", ExecutionTarget.Local, "w", "h", "p", DateTimeOffset.UtcNow,
            allowedCapabilities: new[] { "fs.read", "  " });
        blankCap.Should().Throw<ArgumentException>().WithMessage("*Capability*");

        var missingTime = () => ExecutionEnvelope.Create(
            "e", "n", ExecutionTarget.Local, "w", "h", "p", default);
        missingTime.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ResultEvidence_and_native_manifest_round_trip_json()
    {
        var envelope = ExecutionEnvelope.Create(
            "env-1", "node-a", ExecutionTarget.Cluster, "brick.execute", "sha256:abc", "pack-1",
            DateTimeOffset.Parse("2026-09-04T12:00:00Z"));
        var evidence = ResultEvidence.Create(
            envelope.EnvelopeId, "task-1", ResultEvidenceStatus.Succeeded, "sha256:out",
            DateTimeOffset.Parse("2026-09-04T12:01:00Z"), certificationRecordId: "cert-9");
        var manifest = NativeArtifactManifest.Create(
            "art-1", NativeArtifactFormat.WebAssembly, "sha256:wasm", "_start", new[] { "compute" });

        var envelopeAgain = JsonSerializer.Deserialize<ExecutionEnvelope>(JsonSerializer.Serialize(envelope));
        envelopeAgain!.Target.Should().Be(ExecutionTarget.Cluster);
        envelopeAgain.PayloadHash.Should().Be("sha256:abc");

        var evidenceAgain = JsonSerializer.Deserialize<ResultEvidence>(JsonSerializer.Serialize(evidence));
        evidenceAgain!.CertificationRecordId.Should().Be("cert-9");
        evidenceAgain.Status.Should().Be(ResultEvidenceStatus.Succeeded);

        var manifestAgain = JsonSerializer.Deserialize<NativeArtifactManifest>(JsonSerializer.Serialize(manifest));
        manifestAgain!.Format.Should().Be(NativeArtifactFormat.WebAssembly);
        manifestAgain.AllowedCapabilities.Should().Equal("compute");
    }

    [Fact]
    public void NativeArtifactManifest_create_rejects_blank_hash()
    {
        var act = () => NativeArtifactManifest.Create("art", NativeArtifactFormat.OutOfProcessWorker, " ", "main");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResultEvidence_create_requires_hash_on_success_and_rejects_whitespace()
    {
        var ok = ResultEvidence.Create(
            "env", "task", ResultEvidenceStatus.Rejected, string.Empty,
            DateTimeOffset.Parse("2026-09-04T12:00:00Z"));
        ok.Status.Should().Be(ResultEvidenceStatus.Rejected);

        var missingHash = () => ResultEvidence.Create(
            "env", "task", ResultEvidenceStatus.Succeeded, "  ",
            DateTimeOffset.Parse("2026-09-04T12:00:00Z"));
        missingHash.Should().Throw<ArgumentException>();

        var missingTime = () => ResultEvidence.Create(
            "env", "task", ResultEvidenceStatus.Failed, string.Empty, default);
        missingTime.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Construction_paths_reject_undefined_enums_bad_digests_and_non_positive_duration()
    {
        var issued = DateTimeOffset.Parse("2026-09-04T12:00:00Z");

        var badTarget = () => new ExecutionEnvelope(
            "e", "n", (ExecutionTarget)99, "w", "sha256:abc", "p", issued);
        badTarget.Should().Throw<ArgumentOutOfRangeException>();

        var badHash = () => ExecutionEnvelope.Create(
            "e", "n", ExecutionTarget.Local, "w", "sha256: has space", "p", issued);
        badHash.Should().Throw<ArgumentException>().WithMessage("*Digest*");

        var badDuration = () => ExecutionEnvelope.Create(
            "e", "n", ExecutionTarget.Local, "w", "sha256:abc", "p", issued,
            maxDuration: TimeSpan.Zero);
        badDuration.Should().Throw<ArgumentOutOfRangeException>();

        var badFormat = () => NativeArtifactManifest.Create(
            "art", (NativeArtifactFormat)99, "sha256:wasm", "main");
        badFormat.Should().Throw<ArgumentOutOfRangeException>();

        var badHandle = () => new ScheduledTaskHandle(" ", "env");
        badHandle.Should().Throw<ArgumentException>();

        var viaNew = () => new ExecutionEnvelope(
            " ", "n", ExecutionTarget.Local, "w", "sha256:abc", "p", issued);
        viaNew.Should().Throw<ArgumentException>();
    }
}
