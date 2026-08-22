using System.IO.Compression;
using System.Text.Json;
using System.Xml;
using NetTopologySuite.IO.VectorTiles.Mapbox;
using OsmSharp.Complete;
using OsmSharp.IO.Xml;
using OsmSharp.Streams;
using OsmSharp.Streams.Complete;
using VectorTile = NetTopologySuite.IO.VectorTiles.Tiles.Tile;

namespace Ashlar.Commercial.GameDomain.Mapping;
/// <summary>
/// Summarizes common vector map encodings after fetch (GeoJSON, OSM XML, Mapbox MVT).
/// Full geometry/host rendering remains deferred; this only extracts counts and hints.
/// </summary>
public static class VectorMapPayloadSummarizer
{
    /// <summary>
    /// Default zoom used when interpreting MVT tile coordinates (affects lon/lat projection only).
    /// </summary>
    public const int DefaultMvtTileZoom = 14;

    /// <summary>
    /// Builds a short summary; never throws — failures become <see cref="VectorMapParseSummary"/> with
    /// <see cref="VectorMapParseSummary.ParserKind"/> <c>error</c> or <c>skipped</c>.
    /// </summary>
    /// <param name="data">Raw payload bytes.</param>
    /// <param name="inspection">Output from <see cref="VectorMapPayloadInspector"/>.</param>
    /// <param name="contentTypeHint">Optional HTTP content-type.</param>
    /// <param name="mvtTileZoom">Zoom used with explicit tile coordinates or when URL does not specify z/x/y.</param>
    /// <param name="vectorDataUrl">When set, used to infer MVT z/x/y from common tile URL patterns.</param>
    /// <param name="mvtTileX">Optional MVT column index (with <paramref name="mvtTileY"/> and zoom).</param>
    /// <param name="mvtTileY">Optional MVT row index.</param>
    public static VectorMapParseSummary Summarize(
        ReadOnlyMemory<byte> data,
        VectorPayloadInspection inspection,
        string? contentTypeHint,
        int mvtTileZoom = DefaultMvtTileZoom,
        string? vectorDataUrl = null,
        int? mvtTileX = null,
        int? mvtTileY = null)
    {
        if (data.Length == 0)
            return new VectorMapParseSummary("skipped", "empty payload", []);

        var fmt = inspection.FormatGuess;
        try
        {
            if (fmt is "json_vector")
                return SummarizeGeoJson(data);

            if (fmt is "xml_vector")
                return SummarizeOsmXml(data);

            if (fmt is "mapbox_vector_tile_or_binary")
            {
                var (tz, tx, ty) = ResolveMvtTileIndices(vectorDataUrl, mvtTileZoom, mvtTileX, mvtTileY);
                return SummarizeMvt(data, tz, tx, ty);
            }

            // Content-type hints when magic bytes were ambiguous
            var ct = contentTypeHint ?? string.Empty;
            if (ct.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                var g = SummarizeGeoJson(data);
                if (g.ParserKind != "error")
                    return g;
            }

            if (ct.Contains("xml", StringComparison.OrdinalIgnoreCase))
            {
                var x = SummarizeOsmXml(data);
                if (x.ParserKind != "error")
                    return x;
            }

            return new VectorMapParseSummary(
                "skipped",
                $"no concrete parser for format={fmt}",
                inspection.Notes);
        }
        catch (Exception ex)
        {
            return new VectorMapParseSummary(
                "error",
                ex.Message,
                [.. inspection.Notes]);
        }
    }

    private static (int z, int x, int y) ResolveMvtTileIndices(
        string? vectorDataUrl,
        int mvtTileZoom,
        int? mvtTileX,
        int? mvtTileY)
    {
        var fallbackZ = mvtTileZoom < 0 ? DefaultMvtTileZoom : Math.Clamp(mvtTileZoom, 0, 22);
        if (mvtTileX is >= 0 && mvtTileY is >= 0)
        {
            var xi = mvtTileX.Value;
            var yi = mvtTileY.Value;
            var dim = 1 << fallbackZ;
            if ((uint)xi < dim && (uint)yi < dim)
                return (fallbackZ, xi, yi);
        }

        if (VectorTileUrlParser.TryParseTile(vectorDataUrl, out var pz, out var px, out var py))
            return (pz, px, py);

        return (fallbackZ, 0, 0);
    }

    private static VectorMapParseSummary SummarizeGeoJson(ReadOnlyMemory<byte> data)
    {
        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;
        var geomCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var total = 0;

        void CountFeature(JsonElement fe)
        {
            total++;
            if (!fe.TryGetProperty("geometry", out var geom) || geom.ValueKind == JsonValueKind.Null)
            {
                geomCounts.TryGetValue("null", out var n);
                geomCounts["null"] = n + 1;
                return;
            }

            if (!geom.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
            {
                geomCounts.TryGetValue("unknown", out var u);
                geomCounts["unknown"] = u + 1;
                return;
            }

            var t = typeEl.GetString() ?? "unknown";
            geomCounts.TryGetValue(t, out var c);
            geomCounts[t] = c + 1;
        }

        void CountBareGeometry(JsonElement geomEl)
        {
            total++;
            if (!geomEl.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
            {
                geomCounts.TryGetValue("unknown", out var u);
                geomCounts["unknown"] = u + 1;
                return;
            }

            var t = typeEl.GetString() ?? "unknown";
            geomCounts.TryGetValue(t, out var c);
            geomCounts[t] = c + 1;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in root.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;
                if (el.TryGetProperty("geometry", out _))
                    CountFeature(el);
                else if (el.TryGetProperty("type", out var gt) && gt.ValueKind == JsonValueKind.String &&
                         el.TryGetProperty("coordinates", out _))
                    CountBareGeometry(el);
            }
        }
        else if (root.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
        {
            var kind = type.GetString();
            if (kind is null)
                return new VectorMapParseSummary("error", "GeoJSON: missing root type", []);

            if (kind.Equals("FeatureCollection", StringComparison.OrdinalIgnoreCase) &&
                root.TryGetProperty("features", out var features) &&
                features.ValueKind == JsonValueKind.Array)
            {
                foreach (var fe in features.EnumerateArray())
                {
                    if (fe.ValueKind == JsonValueKind.Object)
                        CountFeature(fe);
                }
            }
            else if (kind.Equals("Feature", StringComparison.OrdinalIgnoreCase))
            {
                CountFeature(root);
            }
        }

        if (total == 0)
            return new VectorMapParseSummary("error", "GeoJSON: no features found", []);

        var parts = geomCounts
            .OrderByDescending(kv => kv.Value)
            .Select(kv => $"{kv.Value} {kv.Key}")
            .ToArray();
        var summary = $"GeoJSON: {total} feature(s) — {string.Join(", ", parts)}";
        return new VectorMapParseSummary("geojson", summary, []);
    }

    private static VectorMapParseSummary SummarizeOsmXml(ReadOnlyMemory<byte> data)
    {
        using var ms = new MemoryStream(data.ToArray(), writable: false);
        var nodes = 0L;
        var ways = 0L;
        var relations = 0L;
        var tagKeys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        using (var reader = XmlReader.Create(ms, new XmlReaderSettings { IgnoreComments = true }))
        {
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                    continue;
                switch (reader.Name)
                {
                    case "node":
                        nodes++;
                        break;
                    case "way":
                        ways++;
                        break;
                    case "relation":
                        relations++;
                        break;
                    case "tag" when reader.HasAttributes:
                    {
                        string? k = null;
                        while (reader.MoveToNextAttribute())
                        {
                            if (reader.Name == "k")
                                k = reader.Value;
                        }

                        reader.MoveToElement();
                        if (!string.IsNullOrEmpty(k))
                        {
                            tagKeys.TryGetValue(k, out var n);
                            tagKeys[k] = n + 1;
                        }

                        break;
                    }
                }
            }
        }

        // Resolved elements + tags via OsmSharp complete stream (bounded work — OSM region extracts).
        ms.Position = 0;
        var xmlSource = new XmlOsmStreamSource(ms);
        var complete = new OsmSimpleCompleteStreamSource(xmlSource);
        var waysWithTags = 0;
        foreach (var geo in complete)
        {
            switch (geo)
            {
                case CompleteWay cw when cw.Tags is { Count: > 0 }:
                    waysWithTags++;
                    foreach (var tag in cw.Tags)
                    {
                        tagKeys.TryGetValue(tag.Key, out var tn);
                        tagKeys[tag.Key] = tn + 1;
                    }

                    break;
            }
        }

        var topTags = tagKeys
            .OrderByDescending(kv => kv.Value)
            .Take(8)
            .Select(kv => $"{kv.Key}:{kv.Value}")
            .ToArray();

        var summary =
            $"OSM XML: nodes={nodes}, ways={ways}, relations={relations}, ways-with-tags={waysWithTags}";
        var details = topTags.Length > 0
            ? (IReadOnlyList<string>)[$"top tag keys: {string.Join(", ", topTags)}"]
            : [];

        return new VectorMapParseSummary("osm_xml", summary, details);
    }

    private static VectorMapParseSummary SummarizeMvt(ReadOnlyMemory<byte> data, int z, int x, int y)
    {
        Stream stream;
        if (data.Length >= 2 && data.Span[0] == 0x1f && data.Span[1] == 0x8b)
            stream = new GZipStream(new MemoryStream(data.ToArray(), writable: false), CompressionMode.Decompress);
        else
            stream = new MemoryStream(data.ToArray(), writable: false);

        using (stream)
        {
            z = Math.Clamp(z, 0, 22);
            var tile = new VectorTile(x, y, z);
            var reader = new MapboxTileReader();
            var vt = reader.Read(stream, tile);
            var lines = new List<string>();
            var total = 0;
            foreach (var layer in vt.Layers)
            {
                var c = layer.Features.Count;
                total += c;
                lines.Add($"{layer.Name}:{c}");
            }

            var summary = $"MVT: {total} feature(s) in {vt.Layers.Count} layer(s) [tile z/x/y={z}/{x}/{y}]";
            var detailParts = new List<string> { $"tile_zxy={z}/{x}/{y}" };
            if (lines.Count > 0)
                detailParts.Add($"layers: {string.Join(", ", lines.Take(12))}");

            return new VectorMapParseSummary("mvt", summary, detailParts);
        }
    }
}
