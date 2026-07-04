using System.Buffers.Binary;

namespace Nexo.Commercial.GameDomain.Mapping;
/// <summary>
/// Best-effort terrain raster summaries (PNG IHDR dimensions; other formats stay header-only).
/// </summary>
public static class TerrainPayloadSummarizer
{
    /// <summary>Summarize fetched terrain bytes for pipeline diagnostics.</summary>
    public static TerrainParseSummary Summarize(ReadOnlyMemory<byte> data, string? contentType)
    {
        var span = data.Span;
        var mime = contentType?.Trim().ToLowerInvariant() ?? string.Empty;

        if (span.Length == 0)
            return new TerrainParseSummary("empty", "0 bytes", []);

        var sniff = TerrainPayloadInspector.InspectFormat(span);

        if (sniff == "png")
            return SummarizePng(span, mime);

        if (sniff == "jpeg")
            return new TerrainParseSummary(
                "jpeg",
                "JPEG raster",
                BuildDetails(mime, "Decode dimensions with engine/ImageSharp; common for photo overlays or baked masks."));

        if (sniff == "tiff")
            return new TerrainParseSummary(
                "geotiff_hint",
                "TIFF / GeoTIFF container",
                BuildDetails(mime, "Use GDAL / engine importer for CRS and elevation planes if DEM; verify horizontal units."));

        if (sniff == "webp")
            return new TerrainParseSummary(
                "webp",
                "WebP raster",
                BuildDetails(mime, "Decode in engine; less common for DEM tiles."));

        return new TerrainParseSummary(
            "unknown",
            $"opaque_bytes length={span.Length}",
            BuildDetails(mime, "Treat as height/displacement raw only if your provider documents layout."));
    }

    private static TerrainParseSummary SummarizePng(ReadOnlySpan<byte> span, string mime)
    {
        var details = BuildDetails(mime, "For Mapbox Terrain-RGB, decode RGB → elevation using provider scaling.");

        if (!TryReadPngDimensions(span, out var w, out var h, out var bitDepth, out var colorType))
        {
            return new TerrainParseSummary(
                "png",
                "PNG (IHDR not parsed)",
                details);
        }

        var colour = colorType switch
        {
            0 => "gray",
            2 => "rgb",
            3 => "indexed",
            4 => "gray_alpha",
            6 => "rgba",
            _ => $"colorType={colorType}"
        };

        var summary = $"{w}x{h} PNG; depth={bitDepth}; {colour}";
        var list = new List<string>(details) { $"ihdr: width={w}; height={h}; bitDepth={bitDepth}; colorType={colorType}" };
        return new TerrainParseSummary("png", summary, list);
    }

    private static List<string> BuildDetails(string mime, string hint)
    {
        var list = new List<string> { hint };
        if (!string.IsNullOrEmpty(mime))
            list.Insert(0, $"content-type={mime}");
        return list;
    }

    /// <summary>Reads width/height from the first IHDR chunk without validating CRC.</summary>
    public static bool TryReadPngDimensions(
        ReadOnlySpan<byte> span,
        out uint width,
        out uint height,
        out int bitDepth,
        out int colorType)
    {
        width = height = 0;
        bitDepth = colorType = 0;

        if (span.Length < 8 + 12 + 13)
            return false;

        if (span[0] != 0x89 || span[1] != (byte)'P' || span[2] != (byte)'N' || span[3] != (byte)'G')
            return false;

        var offset = 8;
        while (offset + 12 <= span.Length)
        {
            var chunkLen = (int)BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset, 4));
            if (chunkLen < 0 || offset + 12 + chunkLen > span.Length)
                return false;

            var type = span.Slice(offset + 4, 4);
            var dataOffset = offset + 8;
            if (type[0] == (byte)'I' && type[1] == (byte)'H' && type[2] == (byte)'D' && type[3] == (byte)'R')
            {
                if (chunkLen < 13 || dataOffset + 13 > span.Length)
                    return false;

                width = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(dataOffset, 4));
                height = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(dataOffset + 4, 4));
                bitDepth = span[dataOffset + 8];
                colorType = span[dataOffset + 9];
                return width > 0 && height > 0;
            }

            offset += 12 + chunkLen;
        }

        return false;
    }
}
