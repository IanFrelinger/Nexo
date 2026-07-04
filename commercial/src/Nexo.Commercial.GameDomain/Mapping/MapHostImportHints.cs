namespace Nexo.Commercial.GameDomain.Mapping;
/// <summary>
/// Human-readable suggested host/engine actions after the reference pipeline has fetched and summarized bytes (M1–M3 bridge).
/// </summary>
public static class MapHostImportHints
{
    /// <summary>Returns bullet hints for engine importers based on parse output.</summary>
    public static IReadOnlyList<string> FromParseSummary(VectorMapParseSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new List<string>();
        switch (summary.ParserKind)
        {
            case "geojson":
                lines.Add("Bind GeoJSON features to LineRenderer / mesh strips or extruded polygons per geometry type.");
                lines.Add("Use palette colors from the aesthetic manifest for fill/stroke before replacing with textures.");
                break;
            case "osm_xml":
                lines.Add("Prefer ways with highway/waterway/building tags for roads and water; relations for multipolygons.");
                lines.Add("Complete-Ways stream already resolved nodes for ways in regional extracts.");
                break;
            case "mvt":
                lines.Add("Decode layers from MVT into materials keyed by layer name + feature tags.");
                lines.Add("Align tile transform using the reported z/x/y so geometry sits on the terrain chunk.");
                break;
            default:
                lines.Add("Inspect VectorMapPayloadInspector format guess and attach host-specific decoders.");
                break;
        }

        return lines;
    }
}
