namespace Nexo.GameDomain.Mapping;

/// <summary>
/// Host-facing bullets for terrain raster imports (mirrors <see cref="MapHostImportHints"/> for vectors).
/// </summary>
public static class TerrainHostImportHints
{
    /// <summary>Returns short checklist lines for engine terrain pipelines.</summary>
    public static IReadOnlyList<string> FromTerrainSummary(TerrainParseSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var lines = new List<string>
        {
            $"Terrain payload kind: {summary.ParserKind} — {summary.Summary}"
        };

        foreach (var d in summary.Details)
        {
            if (!string.IsNullOrWhiteSpace(d))
                lines.Add(d);
        }

        lines.Add(summary.ParserKind switch
        {
            "png" =>
                "PNG: sample elevation from RGB or grayscale channel per provider docs; watch gamma/premultiplied alpha.",
            "jpeg" =>
                "JPEG: unsuitable for lossless DEM unless provider guarantees; prefer PNG/TIFF for height.",
            "geotiff_hint" =>
                "GeoTIFF: bind CRS + vertical datum before stitching tiles; reproject in GIS if mixing sources.",
            "webp" =>
                "WebP: decode to linear floats before displacement if used as height.",
            _ =>
                "Unknown terrain bytes: confirm tile format with provider before interpreting as heightfield."
        });

        return lines;
    }
}
