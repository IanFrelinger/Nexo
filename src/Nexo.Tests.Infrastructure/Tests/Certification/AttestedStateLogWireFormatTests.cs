using FluentAssertions;
using Nexo.Certification.State;
using Nexo.Infrastructure.Certification;
using Nexo.Tests.Infrastructure.Certification.Reuse;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class AttestedStateLogWireFormatTests
{
    private const string HmacKey = "attested-state-log-test-hmac";

    [Fact]
    public void BundledSample_DeserializesAndTrustsTier1()
    {
        var artifactRoot = PhaseWitnessPaths.ArtifactRoot();
        var schema = AttestedStateLogWireFormat.DeserializeSchema(File.ReadAllText(PhaseWitnessPaths.SchemaPath(artifactRoot)));
        var log = AttestedStateLogWireFormat.DeserializeLog(File.ReadAllText(PhaseWitnessPaths.LogPath(artifactRoot)));
        var catalog = new FileCertifiedBehaviorCatalog(PhaseWitnessPaths.BehaviorRoot(artifactRoot));
        var resolver = new CertifiedBehaviorCertificateResolver(catalog);

        var result = StateLogVerifier.Verify(log, schema, resolver, HmacKey);

        result.Trusted.Should().BeTrue($"expected TRUSTED, got {result.FailureCode}: {result.Reason}");
        result.VerifiedTransitions.Should().Be(3);
    }

    [Fact]
    public void RoundTrip_PreservesTier1TrustVerdict()
    {
        var artifactRoot = PhaseWitnessPaths.ArtifactRoot();
        var schema = AttestedStateLogWireFormat.DeserializeSchema(File.ReadAllText(PhaseWitnessPaths.SchemaPath(artifactRoot)));
        var original = AttestedStateLogWireFormat.DeserializeLog(File.ReadAllText(PhaseWitnessPaths.LogPath(artifactRoot)));
        var catalog = new FileCertifiedBehaviorCatalog(PhaseWitnessPaths.BehaviorRoot(artifactRoot));
        var resolver = new CertifiedBehaviorCertificateResolver(catalog);

        var json = AttestedStateLogWireFormat.SerializeLog(original);
        var roundTripped = AttestedStateLogWireFormat.DeserializeLog(json);

        var before = StateLogVerifier.Verify(original, schema, resolver, HmacKey);
        var after = StateLogVerifier.Verify(roundTripped, schema, resolver, HmacKey);

        after.Trusted.Should().Be(before.Trusted);
        after.VerifiedTransitions.Should().Be(before.VerifiedTransitions);
    }

    [Fact]
    public void LiveStateSidecar_RoundTrip_PreservesHash()
    {
        var artifactRoot = PhaseWitnessPaths.ArtifactRoot();
        var originalJson = File.ReadAllText(PhaseWitnessPaths.LiveStatePath(artifactRoot));
        var original = AttestedStateLogWireFormat.DeserializeLiveState(originalJson);

        var roundTripped = AttestedStateLogWireFormat.DeserializeLiveState(
            AttestedStateLogWireFormat.SerializeLiveState(original));

        roundTripped.CurrentStateHash.Should().Be(original.CurrentStateHash);
    }
}
