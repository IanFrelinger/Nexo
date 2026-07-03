using FluentAssertions;
using Nexo.Certification.Physical.Resolution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

[Trait("Category", "Certification")]
public sealed class PhysicalAtomCertBundleManifestTests
{
    private static readonly string SampleManifestPath =
        Path.Combine(AppContext.BaseDirectory, "physical-atom-cert", "design-scope.bundle.json");

    [Fact]
    public void SampleBundleManifest_RoundTripsAndVerifies()
    {
        var json = File.ReadAllText(SampleManifestPath);
        var bundle = PhysicalAtomCertBundleManifest.Deserialize(json);

        var reserialized = PhysicalAtomCertBundleManifest.Serialize(bundle);
        var roundTrip = PhysicalAtomCertBundleManifest.Deserialize(reserialized);

        var result = PhysicalAtomCertBundleVerifier.Verify(roundTrip, bundle.IssuerPublicKey);

        result.Trusted.Should().BeTrue();
    }
}
