using System.Text;

namespace Ashlar.Certification.Physical.Tagging;

/// <summary>
/// Simplified NDEF external-type record for NFC tags (headless byte layout, no device I/O).
/// </summary>
public static class PhysicalAtomNfcNdefCodec
{
    public const string RecordType = "ashlar:atom";

    private const byte TnfExternal = 0x04;
    private const byte FlagsSingleShort = 0xD4; // MB=1, ME=1, SR=1, TNF=External

    public static byte[] Encode(ReadOnlySpan<byte> tagBinaryPayload)
    {
        if (tagBinaryPayload.IsEmpty)
            throw new ArgumentException("Tag binary payload is required.", nameof(tagBinaryPayload));

        var typeBytes = Encoding.UTF8.GetBytes(RecordType);
        if (typeBytes.Length > byte.MaxValue || tagBinaryPayload.Length > byte.MaxValue)
            throw new ArgumentException("NDEF short record limits exceeded.");

        var record = new byte[4 + typeBytes.Length + tagBinaryPayload.Length];
        record[0] = FlagsSingleShort;
        record[1] = (byte)typeBytes.Length;
        record[2] = (byte)tagBinaryPayload.Length;
        record[3] = 0;
        typeBytes.CopyTo(record.AsSpan(4));
        tagBinaryPayload.CopyTo(record.AsSpan(4 + typeBytes.Length));
        return record;
    }

    public static bool TryDecode(
        ReadOnlySpan<byte> ndefRecord,
        out byte[] payload,
        out string? failureCode,
        out string? reason)
    {
        payload = Array.Empty<byte>();

        if (ndefRecord.Length < 4)
        {
            failureCode = "ndef-record-truncated";
            reason = "NDEF record is too short.";
            return false;
        }

        var typeLength = ndefRecord[1];
        var payloadLength = ndefRecord[2];
        var expectedLength = 4 + typeLength + payloadLength;

        if (ndefRecord.Length < expectedLength)
        {
            failureCode = "ndef-record-truncated";
            reason = "NDEF record length does not match header.";
            return false;
        }

        var type = Encoding.UTF8.GetString(ndefRecord.Slice(4, typeLength));
        if (!string.Equals(type, RecordType, StringComparison.Ordinal))
        {
            failureCode = "ndef-type-mismatch";
            reason = $"NDEF type '{type}' is not '{RecordType}'.";
            return false;
        }

        payload = ndefRecord.Slice(4 + typeLength, payloadLength).ToArray();
        failureCode = null;
        reason = null;
        return true;
    }
}
