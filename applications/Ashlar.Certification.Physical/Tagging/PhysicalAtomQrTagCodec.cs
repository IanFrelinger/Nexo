namespace Ashlar.Certification.Physical.Tagging;

/// <summary>
/// QR payload encoding: ashlar-atom:v1:&lt;base64url(binary)&gt;
/// </summary>
public static class PhysicalAtomQrTagCodec
{
    public const string Prefix = "ashlar-atom:v1:";

    public static string Encode(PhysicalAtomTagReference reference) =>
        EncodeBytes(PhysicalAtomTagBinaryCodec.Encode(reference));

    public static string EncodeBytes(byte[] payload) =>
        Prefix + ToBase64Url(payload);

    public static bool TryDecode(
        string qrPayload,
        out PhysicalAtomTagReference? reference,
        out string? failureCode,
        out string? reason)
    {
        reference = null;

        if (string.IsNullOrWhiteSpace(qrPayload) || !qrPayload.StartsWith(Prefix, StringComparison.Ordinal))
        {
            failureCode = "tag-prefix-invalid";
            reason = $"QR payload must start with '{Prefix}'.";
            return false;
        }

        var encoded = qrPayload[Prefix.Length..];
        byte[] bytes;
        try
        {
            bytes = FromBase64Url(encoded);
        }
        catch (FormatException)
        {
            failureCode = "tag-payload-malformed";
            reason = "QR payload base64url segment is malformed.";
            return false;
        }

        return PhysicalAtomTagBinaryCodec.TryDecode(bytes, out reference, out failureCode, out reason);
    }

    private static string ToBase64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string encoded)
    {
        var padded = encoded.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
