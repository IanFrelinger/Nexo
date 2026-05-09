using System.Text.Json;
using FluentAssertions;
using Nexo.Abstractions;
using Nexo.Tools.Dev;
using Xunit;

namespace Nexo.Tests.BackgroundAgents.Tools;

public sealed class TileMapRenderToolTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _repoRoot;

    public TileMapRenderToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "nexo-tile-map-" + Guid.NewGuid().ToString("N"));
        _repoRoot = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(Path.Combine(_repoRoot, "maps"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    [Fact]
    public async Task Render_writes_png_and_reports_bounds()
    {
        var osmRel = "maps/sample.osm";
        var osmPath = Path.Combine(_repoRoot, osmRel);
        await File.WriteAllTextAsync(osmPath, MinimalOsmXml);

        var snap = new WorldSnapshot(0, new Dictionary<string, object?> { ["RepoRoot"] = _repoRoot });
        var tool = new TileMapRenderTool();
        var args = JsonSerializer.SerializeToElement(new
        {
            root = _repoRoot,
            osm_path = osmRel,
            width = 400,
            height = 400
        });

        var result = await tool.InvokeAsync(new ToolCall(tool.Id, args), snap, default);

        var json = JsonSerializer.Serialize(result.Payload);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("ok").GetBoolean().Should().BeTrue();
        var outPath = doc.RootElement.GetProperty("output_path").GetString()!;
        outPath.Should().Be("maps/sample.png");

        var pngFull = Path.Combine(_repoRoot, outPath);
        File.Exists(pngFull).Should().BeTrue();
        new FileInfo(pngFull).Length.Should().BeGreaterThan(100);
    }

    private const string MinimalOsmXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <osm version="0.6">
          <node id="1" lat="51.0" lon="7.0"/>
          <node id="2" lat="51.001" lon="7.001"/>
          <way id="10">
            <nd ref="1"/><nd ref="2"/>
            <tag k="highway" v="primary"/>
          </way>
        </osm>
        """;
}
