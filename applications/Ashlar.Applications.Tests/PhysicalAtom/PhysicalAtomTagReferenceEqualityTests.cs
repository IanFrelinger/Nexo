using FluentAssertions;
using Ashlar.Certification.Physical.Tagging;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Value-equality contract of the compact tag reference: byte payloads compare by
/// content (not array identity), hash codes agree for equal references, and null
/// never equals a reference.
/// </summary>
[Trait("Category", "Certification")]
public sealed class PhysicalAtomTagReferenceEqualityTests
{
    private static readonly Guid AtomId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static PhysicalAtomTagReference Reference(
        TagReferenceKind kind = TagReferenceKind.CertRef,
        byte assetHashFill = 0x11,
        string assetVersion = "1.2.3",
        byte fingerprintFill = 0x22)
    {
        var assetHash = new byte[PhysicalAtomTagReference.AssetHashLength];
        Array.Fill(assetHash, assetHashFill);
        var fingerprint = new byte[PhysicalAtomTagReference.IssuerFingerprintLength];
        Array.Fill(fingerprint, fingerprintFill);
        return new PhysicalAtomTagReference(kind, AtomId, assetHash, assetVersion, fingerprint);
    }

    [Fact]
    public void A1_EqualContentDistinctArrays_AreEqualWithSameHashCode()
    {
        var a = Reference();
        var b = Reference();

        a.Equals(b).Should().BeTrue("payload bytes compare by content, not array identity");
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void R1_Null_IsNeverEqual()
    {
        Reference().Equals(null).Should().BeFalse();
    }

    [Fact]
    public void R2_DifferingComponents_AreNotEqual()
    {
        var baseline = Reference();

        Reference(kind: TagReferenceKind.BundleRef).Equals(baseline).Should().BeFalse();
        Reference(assetHashFill: 0x12).Equals(baseline).Should().BeFalse();
        Reference(assetVersion: "1.2.4").Equals(baseline).Should().BeFalse();
        Reference(fingerprintFill: 0x23).Equals(baseline).Should().BeFalse();
    }

    [Fact]
    public void R3_DifferingPayloadBytes_ProduceDifferentHashCodes()
    {
        Reference().GetHashCode().Should().NotBe(Reference(assetHashFill: 0x12).GetHashCode(),
            "payload bytes participate in the hash, so content changes must not silently collide");
    }
}
