using System.Text;

namespace Nexo.Adapters.GeoTerrain.Export;

public static class GlbWriter
{
    public static byte[] Write(string gltfJson, ReadOnlyMemory<byte> binBytes)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(gltfJson);
#else
        if (gltfJson is null) throw new ArgumentNullException(nameof(gltfJson));
#endif
        var jsonBytes = Encoding.UTF8.GetBytes(gltfJson);

        // JSON chunk must be padded to 4 bytes with spaces (0x20).
        var jsonPaddedLen = Align4(jsonBytes.Length);
        var binPaddedLen = Align4(binBytes.Length);

        var totalLen = 12 + 8 + jsonPaddedLen + 8 + binPaddedLen;
        var outBytes = new byte[totalLen];

        // Header
        WriteU32(outBytes, 0, 0x46546C67); // 'glTF'
        WriteU32(outBytes, 4, 2);          // version
        WriteU32(outBytes, 8, (uint)totalLen);

        var offset = 12;

        // JSON chunk header
        WriteU32(outBytes, offset + 0, (uint)jsonPaddedLen);
        WriteU32(outBytes, offset + 4, 0x4E4F534A); // 'JSON'
        offset += 8;

        Buffer.BlockCopy(jsonBytes, 0, outBytes, offset, jsonBytes.Length);
        for (var i = jsonBytes.Length; i < jsonPaddedLen; i++) outBytes[offset + i] = 0x20;
        offset += jsonPaddedLen;

        // BIN chunk header
        WriteU32(outBytes, offset + 0, (uint)binPaddedLen);
        WriteU32(outBytes, offset + 4, 0x004E4942); // 'BIN\0'
        offset += 8;

        var binSpan = binBytes.Span;
        binSpan.CopyTo(outBytes.AsSpan(offset, binSpan.Length));
        // Remaining pad bytes are already 0.
        return outBytes;
    }

    private static int Align4(int n) => (n + 3) & ~3;

    private static void WriteU32(byte[] dst, int offset, uint v)
    {
        dst[offset + 0] = (byte)(v & 0xFF);
        dst[offset + 1] = (byte)((v >> 8) & 0xFF);
        dst[offset + 2] = (byte)((v >> 16) & 0xFF);
        dst[offset + 3] = (byte)((v >> 24) & 0xFF);
    }
}

