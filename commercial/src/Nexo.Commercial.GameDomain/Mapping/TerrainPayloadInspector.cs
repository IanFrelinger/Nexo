namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Magic-byte terrain payload sniffing for HTTP fetch diagnostics.
/// </summary>
public static class TerrainPayloadInspector
{
    /// <summary>Returns a short format label for logging / pipeline detail.</summary>
    public static string InspectFormat(ReadOnlySpan<byte> span)
    {
        if (span.Length == 0)
            return "empty";

        // PNG
        if (span.Length >= 8 &&
            span[0] == 0x89 && span[1] == (byte)'P' && span[2] == (byte)'N' && span[3] == (byte)'G' &&
            span[4] == 0x0D && span[5] == 0x0A && span[6] == 0x1A && span[7] == 0x0A)
            return "png";

        // JPEG
        if (span.Length >= 3 && span[0] == 0xFF && span[1] == 0xD8 && span[2] == 0xFF)
            return "jpeg";

        // TIFF little-endian (II + 0x002A) / big-endian (MM + 0x002A) classic; BigTIFF uses 43.
        if (span.Length >= 4 &&
            ((span[0] == (byte)'I' && span[1] == (byte)'I') || (span[0] == (byte)'M' && span[1] == (byte)'M')))
        {
            ushort version = span[0] == (byte)'I'
                ? System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(2, 2))
                : System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2));

            if (version == 42 || version == 43)
                return "tiff";
        }

        // WebP (RIFF .... WEBP)
        if (span.Length >= 12 &&
            span[0] == (byte)'R' && span[1] == (byte)'I' && span[2] == (byte)'F' && span[3] == (byte)'F' &&
            span[8] == (byte)'W' && span[9] == (byte)'E' && span[10] == (byte)'B' && span[11] == (byte)'P')
            return "webp";

        return "unknown";
    }
}
