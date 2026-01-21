using System.Text.Json;
using Nexo.Abstractions;

namespace Nexo.Tools.GeoTerrain;

/// <summary>
/// Tool that reads an SRTM .hgt file and returns a small summary (no heavy payload).
/// </summary>
public sealed class SrtmHgtAnalyzeTool : ITool
{
    public string Id => "geoterrain.srtm.hgt.analyze";

    public ToolSchema Schema => new(Id, "Analyze an SRTM .hgt file (size, min/max, nodata)", """
    {"type":"object","required":["root","path"],"properties":{"root":{"type":"string"},"path":{"type":"string"}}}
    """);

    private sealed record Args(string root, string path);

    public Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
    {
        var args = JsonSerializer.Deserialize<Args>(toolCall.Arguments)!;
        var full = Path.Combine(args.root, args.path);
        var bytes = File.ReadAllBytes(full);

        var summary = Nexo.GeoTerrain.SrtmHgtParser.Analyze(bytes);

        var delta = new ActionDelta(s.Tick, s.Tick + 1, new[]
        {
            $"geoterrain:hgt.analyze path={args.path}",
            $"geoterrain:hgt.size={summary.Size}",
            $"geoterrain:hgt.min={summary.MinMeters}",
            $"geoterrain:hgt.max={summary.MaxMeters}",
            $"geoterrain:hgt.nodata={summary.NoDataSamples}"
        });

        return Task.FromResult(new ToolResult(delta, new
        {
            ok = true,
            path = args.path,
            summary.Size,
            summary.SampleCount,
            summary.MinMeters,
            summary.MaxMeters,
            summary.NoDataSamples
        }));
    }
}

