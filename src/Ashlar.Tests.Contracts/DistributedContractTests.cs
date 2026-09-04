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
}
