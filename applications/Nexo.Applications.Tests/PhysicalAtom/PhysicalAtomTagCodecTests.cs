using FluentAssertions;
using Nexo.Certification.Physical.Tagging;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Certification;

/// <summary>
/// Phase 2 tag codec refusal/admission tests. Witness payloads are hand-authored.
/// </summary>
[Trait("Category", "Certification")]
public sealed class PhysicalAtomTagCodecTests
{
    private static readonly PhysicalAtomTagReference WitnessReference = new(
        TagReferenceKind.CertRef,
        Guid.Parse("6ba7b812-9dad-11d1-80b4-00c04fd430c8"),
        Convert.FromHexString("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"),
        "1.0.0",
        Convert.FromHexString("deadbeefcafebabe"));

    [Fact]
    public void R1_InvalidQrPrefix_Refuses()
    {
        var result = PhysicalAtomQrTagCodec.TryDecode("https://example.com/not-a-nexo-tag", out _, out var code, out _);

        result.Should().BeFalse();
        code.Should().Be("tag-prefix-invalid");
    }

    [Fact]
    public void R2_CorruptedBase64Payload_Refuses()
    {
        var result = PhysicalAtomQrTagCodec.TryDecode("nexo-atom:v1:!!!not-base64url!!!", out _, out var code, out _);

        result.Should().BeFalse();
        code.Should().Be("tag-payload-malformed");
    }

    [Fact]
    public void R3_TruncatedBinaryPayload_Refuses()
    {
        var truncated = PhysicalAtomTagBinaryCodec.Encode(WitnessReference).AsSpan(0, 10).ToArray();
        var qr = PhysicalAtomQrTagCodec.EncodeBytes(truncated);

        var result = PhysicalAtomQrTagCodec.TryDecode(qr, out _, out var code, out _);

        result.Should().BeFalse();
        code.Should().Be("tag-payload-truncated");
    }

    [Fact]
    public void R4_TamperedIntegrityCrc_Refuses()
    {
        var bytes = PhysicalAtomTagBinaryCodec.Encode(WitnessReference);
        bytes[^1] ^= 0xFF;
        var qr = PhysicalAtomQrTagCodec.EncodeBytes(bytes);

        var result = PhysicalAtomQrTagCodec.TryDecode(qr, out _, out var code, out _);

        result.Should().BeFalse();
        code.Should().Be("tag-integrity-mismatch");
    }

    [Fact]
    public void R5_UnsupportedVersion_Refuses()
    {
        var bytes = PhysicalAtomTagBinaryCodec.Encode(WitnessReference);
        bytes[0] = 99;
        var qr = PhysicalAtomQrTagCodec.EncodeBytes(bytes);

        var result = PhysicalAtomQrTagCodec.TryDecode(qr, out _, out var code, out _);

        result.Should().BeFalse();
        code.Should().Be("tag-version-unsupported");
    }

    [Fact]
    public void R6_NfcRecordTypeMismatch_Refuses()
    {
        var payload = PhysicalAtomTagBinaryCodec.Encode(WitnessReference);
        var ndef = PhysicalAtomNfcNdefCodec.Encode(payload);
        ndef[4] = (byte)'x';

        var result = PhysicalAtomNfcNdefCodec.TryDecode(ndef, out _, out var code, out _);

        result.Should().BeFalse();
        code.Should().Be("ndef-type-mismatch");
    }

    [Fact]
    public void A1_QrRoundTrip_PreservesReference()
    {
        var qr = PhysicalAtomQrTagCodec.Encode(WitnessReference);

        var ok = PhysicalAtomQrTagCodec.TryDecode(qr, out var decoded, out var code, out _);

        ok.Should().BeTrue();
        code.Should().BeNull();
        decoded.Should().Be(WitnessReference);
    }

    [Fact]
    public void A2_NfcRoundTrip_PreservesReference()
    {
        var payload = PhysicalAtomTagBinaryCodec.Encode(WitnessReference);
        var ndef = PhysicalAtomNfcNdefCodec.Encode(payload);

        var ok = PhysicalAtomNfcNdefCodec.TryDecode(ndef, out var decodedBytes, out var code, out _);

        ok.Should().BeTrue();
        code.Should().BeNull();
        PhysicalAtomTagBinaryCodec.TryDecode(decodedBytes, out var decoded, out _, out _).Should().BeTrue();
        decoded.Should().Be(WitnessReference);
    }
}
