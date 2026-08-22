using System.IO;
using System.Text.Json;
using Ashlar.Abstractions;
using Ashlar.Tools.Dev.Deltas;
using OsmSharp.Complete;
using OsmSharp.IO.Xml;
using OsmSharp.Streams;
using OsmSharp.Streams.Complete;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using IoPath = System.IO.Path;

namespace Ashlar.Tools.Dev;

/// <summary>
/// Renders OpenStreetMap XML (<c>.osm</c>) into a PNG preview so agents can visualize
/// street-level detail (roads, rails, water, parks, buildings) similar to stylized map reels.
/// Reads data under the repo root, writes PNG next to the source or to <c>output_path</c>.
/// </summary>
public sealed class TileMapRenderTool : ITool
{
    public string Id => "repo.tile_map.render";

    public ToolSchema Schema => new(Id,
        "Render an OSM XML map file to a PNG image (street-detail cartography preview)",
        """
        {"type":"object","required":["root","osm_path"],"properties":{"root":{"type":"string","description":"Repository root directory"},"osm_path":{"type":"string","description":"Path to .osm file relative to root"},"output_path":{"type":"string","description":"Optional PNG path relative to root (default: same name as osm with .png)"},"width":{"type":"integer","description":"Image width in pixels (default 1200, max 4096)"},"height":{"type":"integer","description":"Image height in pixels (default 1200, max 4096)"},"min_lat":{"type":"number","description":"Optional south bound (WGS84); bbox auto-fits from data if omitted"},"max_lat":{"type":"number","description":"Optional north bound"},"min_lon":{"type":"number","description":"Optional west bound"},"max_lon":{"type":"number","description":"Optional east bound"}}}
        """);

    private sealed record Args(
        string root,
        string osm_path,
        string? output_path,
        int? width,
        int? height,
        double? min_lat,
        double? max_lat,
        double? min_lon,
        double? max_lon);

    public Task<ToolResult> InvokeAsync(ToolCall call, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(call.Arguments)!;
        var delta = new RepoDelta { TickFrom = s.Tick, TickTo = s.Tick + 1 };

        if (!RepoFsReadTool.TryResolveRelative(args.root, args.osm_path, out var osmFull, out var resolveErr))
        {
            delta.AddLog($"tile_map.render:{args.osm_path} REJECTED ({resolveErr})");
            return Task.FromResult(new ToolResult(delta, new { ok = false, error = resolveErr }));
        }

        if (!File.Exists(osmFull))
        {
            delta.AddLog($"tile_map.render:{args.osm_path} not_found");
            return Task.FromResult(new ToolResult(delta, new { ok = false, error = "osm file not found", path = args.osm_path }));
        }

        var w = Math.Clamp(args.width ?? 1200, 64, 4096);
        var h = Math.Clamp(args.height ?? 1200, 64, 4096);

        string outRel;
        string outFull;
        if (!string.IsNullOrWhiteSpace(args.output_path))
        {
            if (!RepoFsReadTool.TryResolveRelative(args.root, args.output_path, out outFull, out resolveErr))
            {
                delta.AddLog($"tile_map.render output REJECTED ({resolveErr})");
                return Task.FromResult(new ToolResult(delta, new { ok = false, error = resolveErr }));
            }
            outRel = args.output_path.Replace('\\', '/').TrimStart('/');
        }
        else
        {
            var baseName = IoPath.ChangeExtension(args.osm_path, ".png");
            if (!RepoFsReadTool.TryResolveRelative(args.root, baseName, out outFull, out resolveErr))
            {
                delta.AddLog($"tile_map.render output REJECTED ({resolveErr})");
                return Task.FromResult(new ToolResult(delta, new { ok = false, error = resolveErr }));
            }
            outRel = baseName.Replace('\\', '/').TrimStart('/');
        }

        List<CompleteWay> ways;
        try
        {
            ways = LoadCompleteWays(osmFull, ct);
        }
        catch (Exception ex)
        {
            delta.AddLog($"tile_map.render parse_error {ex.Message}");
            return Task.FromResult(new ToolResult(delta, new { ok = false, error = "failed to parse OSM XML", detail = ex.Message }));
        }

        if (!TryResolveBounds(ways, args, out var minLat, out var maxLat, out var minLon, out var maxLon, out var boundsErr))
        {
            delta.AddLog($"tile_map.render bounds: {boundsErr}");
            return Task.FromResult(new ToolResult(delta, new { ok = false, error = boundsErr }));
        }

        PadBounds(ref minLat, ref maxLat, ref minLon, ref maxLon);

        try
        {
            using var image = RenderToImage(ways, w, h, minLat, maxLat, minLon, maxLon);
            Directory.CreateDirectory(IoPath.GetDirectoryName(outFull)!);
            image.SaveAsPng(outFull);
        }
        catch (Exception ex)
        {
            delta.AddLog($"tile_map.render render_error {ex.Message}");
            return Task.FromResult(new ToolResult(delta, new { ok = false, error = "failed to render PNG", detail = ex.Message }));
        }

        var bytes = new FileInfo(outFull).Length;
        delta.AddLog($"tile_map.render {outRel} {w}x{h} bytes={bytes}");
        return Task.FromResult(new ToolResult(delta, new
        {
            ok = true,
            output_path = outRel,
            width = w,
            height = h,
            bounds = new { min_lat = minLat, max_lat = maxLat, min_lon = minLon, max_lon = maxLon },
            ways_drawn = ways.Count
        }));
    }

    private static List<CompleteWay> LoadCompleteWays(string osmFull, CancellationToken ct)
    {
        using var fs = File.OpenRead(osmFull);
        var xml = new XmlOsmStreamSource(fs);
        var complete = new OsmSimpleCompleteStreamSource(xml);
        var list = new List<CompleteWay>();
        foreach (var geo in complete)
        {
            ct.ThrowIfCancellationRequested();
            if (geo is CompleteWay cw && cw.Nodes is { Length: >= 2 })
                list.Add(cw);
        }
        return list;
    }

    private static bool TryResolveBounds(
        List<CompleteWay> ways,
        Args args,
        out double minLat,
        out double maxLat,
        out double minLon,
        out double maxLon,
        out string error)
    {
        minLat = maxLat = minLon = maxLon = 0;
        error = "";

        if (args.min_lat is not null && args.max_lat is not null && args.min_lon is not null && args.max_lon is not null)
        {
            minLat = args.min_lat.Value;
            maxLat = args.max_lat.Value;
            minLon = args.min_lon.Value;
            maxLon = args.max_lon.Value;
            if (minLat >= maxLat || minLon >= maxLon)
            {
                error = "invalid bbox: min must be less than max";
                return false;
            }
            return true;
        }

        var has = false;
        minLat = 90;
        maxLat = -90;
        minLon = 180;
        maxLon = -180;

        foreach (var way in ways)
        {
            foreach (var n in way.Nodes)
            {
                if (n.Latitude is null || n.Longitude is null)
                    continue;
                var la = n.Latitude.Value;
                var lo = n.Longitude.Value;
                has = true;
                if (la < minLat) minLat = la;
                if (la > maxLat) maxLat = la;
                if (lo < minLon) minLon = lo;
                if (lo > maxLon) maxLon = lo;
            }
        }

        if (!has)
        {
            error = "no coordinates found in OSM data (need nodes with lat/lon)";
            return false;
        }

        return true;
    }

    private static void PadBounds(ref double minLat, ref double maxLat, ref double minLon, ref double maxLon)
    {
        var latPad = Math.Max((maxLat - minLat) * 0.04, 0.0005);
        var lonPad = Math.Max((maxLon - minLon) * 0.04, 0.0005);
        minLat -= latPad;
        maxLat += latPad;
        minLon -= lonPad;
        maxLon += lonPad;

        if (Math.Abs(maxLon - minLon) < 1e-9)
        {
            minLon -= 0.001;
            maxLon += 0.001;
        }
        if (Math.Abs(maxLat - minLat) < 1e-9)
        {
            minLat -= 0.001;
            maxLat += 0.001;
        }
    }

    private static Image<Rgba32> RenderToImage(
        List<CompleteWay> ways,
        int width,
        int height,
        double minLat,
        double maxLat,
        double minLon,
        double maxLon)
    {
        var img = new Image<Rgba32>(width, height, new Rgba32(245, 245, 240));

        // Area fills first (parks, water, buildings), then linear features on top.
        var fillOps = new List<Action<IImageProcessingContext>>();
        var strokeOps = new List<Action<IImageProcessingContext>>();

        foreach (var way in ways)
        {
            var tags = way.Tags;
            var highway = GetTag(tags, "highway");
            var isClosed = way.Nodes.Length >= 4 &&
                           way.Nodes[0].Id == way.Nodes[^1].Id;

            if (isClosed && TryGetAreaFill(tags, out var fillColor))
            {
                if (!TryBuildPolygonPath(way, width, height, minLat, maxLat, minLon, maxLon, out var path))
                    continue;
                var fill = fillColor;
                fillOps.Add(ctx => ctx.Fill(fill, path));
                continue;
            }

            if (!TryGetStrokeStyle(tags, highway, out var penWidth, out var lineColor))
                continue;

            if (!TryBuildLinePath(way, width, height, minLat, maxLat, minLon, maxLon, out var linePath))
                continue;

            var pw = penWidth;
            var lc = lineColor;
            strokeOps.Add(ctx => ctx.Draw(Pens.Solid(lc, pw), linePath));
        }

        img.Mutate(ctx =>
        {
            foreach (var op in fillOps)
                op(ctx);
            foreach (var op in strokeOps)
                op(ctx);
        });

        return img;
    }

    private static bool TryGetAreaFill(OsmSharp.Tags.TagsCollectionBase tags, out Color color)
    {
        color = default;
        var natural = GetTag(tags, "natural");
        var waterway = GetTag(tags, "waterway");
        var landuse = GetTag(tags, "landuse");
        var leisure = GetTag(tags, "leisure");
        var amenity = GetTag(tags, "amenity");

        if (natural is "water" or "bay" || waterway is "riverbank" || landuse is "reservoir" or "basin")
        {
            color = Color.ParseHex("#aad3df");
            return true;
        }
        if (leisure is "park" or "garden" or "pitch" or "playground" || landuse is "grass" or "forest" or "meadow" or "cemetery")
        {
            color = Color.ParseHex("#d5e8c2");
            return true;
        }
        if (GetTag(tags, "building") is not null || amenity is not null)
        {
            color = Color.ParseHex("#dcdcdc");
            return true;
        }
        return false;
    }

    private static bool TryGetStrokeStyle(OsmSharp.Tags.TagsCollectionBase tags, string? highway, out float strokeWidth, out Color strokeColor)
    {
        strokeWidth = 1f;
        strokeColor = Color.ParseHex("#bbbbbb");

        if (!string.IsNullOrEmpty(highway))
        {
            switch (highway)
            {
                case "motorway":
                case "motorway_link":
                    strokeWidth = 4.5f;
                    strokeColor = Color.ParseHex("#e892a4");
                    return true;
                case "trunk":
                case "trunk_link":
                    strokeWidth = 4f;
                    strokeColor = Color.ParseHex("#f9b29e");
                    return true;
                case "primary":
                case "primary_link":
                    strokeWidth = 3.5f;
                    strokeColor = Color.ParseHex("#fcd6a4");
                    return true;
                case "secondary":
                case "secondary_link":
                    strokeWidth = 3f;
                    strokeColor = Color.ParseHex("#f7fabf");
                    return true;
                case "tertiary":
                case "tertiary_link":
                    strokeWidth = 2.5f;
                    strokeColor = Color.ParseHex("#ffffff");
                    return true;
                case "residential":
                case "living_street":
                    strokeWidth = 2f;
                    strokeColor = Color.ParseHex("#ffffff");
                    return true;
                case "service":
                case "track":
                    strokeWidth = 1.5f;
                    strokeColor = Color.ParseHex("#fefefe");
                    return true;
                case "path":
                case "footway":
                case "cycleway":
                case "pedestrian":
                case "steps":
                    strokeWidth = 1.2f;
                    strokeColor = Color.ParseHex("#d4d4d4");
                    return true;
                case "railway":
                    strokeWidth = 2f;
                    strokeColor = Color.ParseHex("#888888");
                    return true;
                default:
                    strokeWidth = 1.8f;
                    strokeColor = Color.ParseHex("#cccccc");
                    return true;
            }
        }

        if (GetTag(tags, "railway") is not null)
        {
            strokeWidth = 2f;
            strokeColor = Color.ParseHex("#555555");
            return true;
        }

        if (GetTag(tags, "waterway") is "river" or "stream" or "canal")
        {
            strokeWidth = 2f;
            strokeColor = Color.ParseHex("#aad3df");
            return true;
        }

        return false;
    }

    private static bool TryBuildPolygonPath(
        CompleteWay way,
        int width,
        int height,
        double minLat,
        double maxLat,
        double minLon,
        double maxLon,
        out IPath path)
    {
        path = null!;
        var pts = ToPoints(way, width, height, minLat, maxLat, minLon, maxLon, closed: true);
        return pts is not null && TryPolygonPath(pts, out path);
    }

    private static bool TryBuildLinePath(
        CompleteWay way,
        int width,
        int height,
        double minLat,
        double maxLat,
        double minLon,
        double maxLon,
        out IPath path)
    {
        path = null!;
        var pts = ToPoints(way, width, height, minLat, maxLat, minLon, maxLon, closed: false);
        if (pts is null || pts.Length < 2)
            return false;
        var pb = new PathBuilder();
        pb.AddLines(pts);
        path = pb.Build();
        return true;
    }

    private static PointF[]? ToPoints(
        CompleteWay way,
        int width,
        int height,
        double minLat,
        double maxLat,
        double minLon,
        double maxLon,
        bool closed)
    {
        var nodes = way.Nodes;
        var list = new List<PointF>(nodes.Length);
        var end = closed ? nodes.Length - 1 : nodes.Length;
        for (var i = 0; i < end; i++)
        {
            var n = nodes[i];
            if (n.Latitude is null || n.Longitude is null)
                continue;
            list.Add(Project(n.Longitude.Value, n.Latitude.Value, width, height, minLat, maxLat, minLon, maxLon));
        }
        return list.Count >= (closed ? 3 : 2) ? list.ToArray() : null;
    }

    private static bool TryPolygonPath(PointF[] pts, out IPath path)
    {
        path = null!;
        if (pts.Length < 3)
            return false;
        var pb = new PathBuilder();
        pb.MoveTo(pts[0]);
        for (var i = 1; i < pts.Length; i++)
            pb.LineTo(pts[i]);
        pb.CloseFigure();
        path = pb.Build();
        return true;
    }

    private static PointF Project(
        double lon,
        double lat,
        int width,
        int height,
        double minLat,
        double maxLat,
        double minLon,
        double maxLon)
    {
        var x = (float)((lon - minLon) / (maxLon - minLon) * width);
        var y = (float)(height - (lat - minLat) / (maxLat - minLat) * height);
        return new PointF(x, y);
    }

    private static string? GetTag(OsmSharp.Tags.TagsCollectionBase tags, string key) =>
        tags.TryGetValue(key, out var v) ? v : null;
}
