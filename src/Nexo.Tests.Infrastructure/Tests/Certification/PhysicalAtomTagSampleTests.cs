using FluentAssertions;
using Nexo.Certification.Physical.Tagging;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>Tests for physical atom tag sample.</summary>
[Trait("Category", "Certification")]
public sealed class PhysicalAtomTagSampleTests
{
    [Fact]
    public void SampleDesignScopeQr_DecodesToExpectedAtom()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "physical-atom-cert", "design-scope.tag-qr.txt");
        var qr = File.ReadAllText(path).Trim();

        var ok = PhysicalAtomQrTagCodec.TryDecode(qr, out var reference, out var code, out _);

        ok.Should().BeTrue();
        code.Should().BeNull();
        reference!.AtomId.Should().Be(Guid.Parse("550e8400-e29b-41d4-a716-446655440001"));
        reference.Kind.Should().Be(TagReferenceKind.BundleRef);
        Convert.ToHexString(reference.AssetHash).ToLowerInvariant().Should()
            .Be("18301e3630ca2816dcb1e23264aaec41d9ed4108337c9b5a936e39565d01c742");
    }
}
