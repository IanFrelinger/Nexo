using System.Buffers.Binary;
using System.Text;

namespace Nexo.Certification.Physical.Tagging;

/// <summary>
/// Binary v1 payload for physical atom tag references with CRC32 integrity.
/// </summary>
public static class PhysicalAtomTagBinaryCodec
{
    public const byte CurrentVersion = 1;

    private const int HeaderLength = 2;
    private const int CrcLength = 4;

    public static byte[] Encode(PhysicalAtomTagReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        if (reference.AssetHash.Length != PhysicalAtomTagReference.AssetHashLength)
            throw new ArgumentException($"Asset hash must be {PhysicalAtomTagReference.AssetHashLength} bytes.", nameof(reference));

        if (reference.IssuerFingerprint.Length != PhysicalAtomTagReference.IssuerFingerprintLength)
            throw new ArgumentException($"Issuer fingerprint must be {PhysicalAtomTagReference.IssuerFingerprintLength} bytes.", nameof(reference));

        var versionBytes = Encoding.UTF8.GetBytes(reference.AssetVersion);
        if (versionBytes.Length == 0 || versionBytes.Length > PhysicalAtomTagReference.MaxAssetVersionLength)
            throw new ArgumentException("Asset version length is out of range.", nameof(reference));

        var length = HeaderLength
            + 16
            + PhysicalAtomTagReference.AssetHashLength
            + 1
            + versionBytes.Length
            + PhysicalAtomTagReference.IssuerFingerprintLength
            + CrcLength;

        var buffer = new byte[length];
        var offset = 0;
        buffer[offset++] = CurrentVersion;
        buffer[offset++] = (byte)reference.Kind;
        reference.AtomId.TryWriteBytes(buffer.AsSpan(offset, 16));
        offset += 16;
        reference.AssetHash.CopyTo(buffer.AsSpan(offset));
        offset += PhysicalAtomTagReference.AssetHashLength;
        buffer[offset++] = (byte)versionBytes.Length;
        versionBytes.CopyTo(buffer.AsSpan(offset));
        offset += versionBytes.Length;
        reference.IssuerFingerprint.CopyTo(buffer.AsSpan(offset));
        offset += PhysicalAtomTagReference.IssuerFingerprintLength;

        WriteIntegrityCrc(buffer);
        return buffer;
    }

    public static bool TryDecode(
        ReadOnlySpan<byte> payload,
        out PhysicalAtomTagReference? reference,
        out string? failureCode,
        out string? reason)
    {
        reference = null;
        var minLength = HeaderLength + 16 + PhysicalAtomTagReference.AssetHashLength + 1
            + PhysicalAtomTagReference.IssuerFingerprintLength + CrcLength;

        if (payload.Length < minLength)
        {
            failureCode = "tag-payload-truncated";
            reason = "Tag payload is too short.";
            return false;
        }

        if (payload[0] != CurrentVersion)
        {
            failureCode = "tag-version-unsupported";
            reason = $"Tag payload version {payload[0]} is not supported (expected {CurrentVersion}).";
            return false;
        }

        if (!VerifyIntegrityCrc(payload))
        {
            failureCode = "tag-integrity-mismatch";
            reason = "Tag payload integrity check failed.";
            return false;
        }

        if (!Enum.IsDefined(typeof(TagReferenceKind), payload[1]))
        {
            failureCode = "tag-ref-kind-invalid";
            reason = $"Unknown tag reference kind '{payload[1]}'.";
            return false;
        }

        var kind = (TagReferenceKind)payload[1];
        var atomId = new Guid(payload.Slice(2, 16));
        var assetHash = payload.Slice(18, PhysicalAtomTagReference.AssetHashLength).ToArray();
        var verLen = payload[50];
        var versionOffset = 51;

        if (verLen == 0 || verLen > PhysicalAtomTagReference.MaxAssetVersionLength
            || versionOffset + verLen + PhysicalAtomTagReference.IssuerFingerprintLength + CrcLength != payload.Length)
        {
            failureCode = "tag-payload-malformed";
            reason = "Tag payload field lengths are invalid.";
            return false;
        }

        var assetVersion = Encoding.UTF8.GetString(payload.Slice(versionOffset, verLen));
        var fpOffset = versionOffset + verLen;
        var issuerFp = payload.Slice(fpOffset, PhysicalAtomTagReference.IssuerFingerprintLength).ToArray();

        reference = new PhysicalAtomTagReference(kind, atomId, assetHash, assetVersion, issuerFp);
        failureCode = null;
        reason = null;
        return true;
    }

    internal static void WriteIntegrityCrc(Span<byte> payload)
    {
        var body = payload[..^CrcLength];
        var crc = TagCrc32.Compute(body);
        BinaryPrimitives.WriteUInt32LittleEndian(payload[^CrcLength..], crc);
    }

    private static bool VerifyIntegrityCrc(ReadOnlySpan<byte> payload)
    {
        var body = payload[..^CrcLength];
        var expected = BinaryPrimitives.ReadUInt32LittleEndian(payload[^CrcLength..]);
        return TagCrc32.Compute(body) == expected;
    }
}
