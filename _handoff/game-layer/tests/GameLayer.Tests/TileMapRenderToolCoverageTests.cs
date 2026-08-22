using System.Text.Json;
using System.Diagnostics;
using FluentAssertions;
using Moq;
using Ashlar.Abstractions;
using Ashlar.Core.Application.Maintenance.Models;
using Ashlar.Core.Application.Maintenance.Ports;
using Ashlar.Tools.Dev;
using Ashlar.Tools.Dev.Deltas;
using Xunit;

namespace Ashlar.Tests.Kernel;

/// <summary>
/// Coverage for TileMapRenderTool, split out of ToolsDevTests.
///
/// TileMapRenderTool is game-specific and leaves the kernel with the game layer
/// (see scripts/handoff/extract-game-layer.sh). These tests were interleaved with
/// 40 unrelated kernel-tool tests in ToolsDevTests, which kept Ashlar.Tests.Kernel
/// referencing the type and blocked the extraction. They move as a unit.
/// </summary>
public class TileMapRenderToolCoverageTests
{
    /// <summary>Call.</summary>
    /// <param name="id">Id.</param>
    /// <param name="args">Args.</param>
    private static ToolCall Call(string id, object args) =>
        new(id, JsonDocument.Parse(JsonSerializer.Serialize(args)).RootElement);

    /// <summary>
    /// True when a file can actually be created in <paramref name="dir"/>, regardless of
    /// what its mode bits claim. Used to detect that we are running with privileges that
    /// bypass permission checks (root), where a "read-only directory" test has no premise.
    /// </summary>
    private static bool CanCreateFileIn(string dir)
    {
        var probe = Path.Combine(dir, ".write-probe-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllText(probe, "x");
            File.Delete(probe);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    [Fact]
    public async Task TileMapRenderTool_rejects_missing_osm_and_invalid_bbox()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var tool = new TileMapRenderTool();
            var missing = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/missing.osm" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            missing.Delta.Log.Should().Contain(l => l.Contains("not_found"));

            var mapsDir = Path.Combine(root, "maps");
            Directory.CreateDirectory(mapsDir);
            await File.WriteAllTextAsync(Path.Combine(mapsDir, "empty.osm"), """
                <?xml version="1.0" encoding="UTF-8"?>
                <osm version="0.6"></osm>
                """);

            var invalidBbox = await tool.InvokeAsync(
                Call("repo.tile_map.render", new
                {
                    root,
                    osm_path = "maps/empty.osm",
                    min_lat = 2.0,
                    max_lat = 1.0,
                    min_lon = -1.0,
                    max_lon = 1.0,
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            invalidBbox.Delta.Log.Should().Contain(l => l.Contains("bounds:"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_renders_osm_with_roads_and_parks()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-ok-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "sample.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="51.500" lon="-0.120"/>
              <node id="2" lat="51.505" lon="-0.115"/>
              <node id="3" lat="51.502" lon="-0.118"/>
              <node id="4" lat="51.500" lon="-0.120"/>
              <way id="10">
                <nd ref="1"/><nd ref="2"/>
                <tag k="highway" v="primary"/>
              </way>
              <way id="11">
                <nd ref="1"/><nd ref="3"/><nd ref="4"/>
                <tag k="leisure" v="park"/>
              </way>
              <way id="12">
                <nd ref="2"/><nd ref="3"/>
                <tag k="waterway" v="river"/>
              </way>
            </osm>
            """);

        try
        {
            var tool = new TileMapRenderTool();
            var result = await tool.InvokeAsync(
                Call("repo.tile_map.render", new
                {
                    root,
                    osm_path = "maps/sample.osm",
                    output_path = "maps/out.png",
                    width = 256,
                    height = 256,
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("tile_map.render maps/out.png"));
            File.Exists(Path.Combine(root, "maps/out.png")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_uses_default_output_and_custom_bbox()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-default-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "city.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="51.500" lon="-0.120"/>
              <node id="2" lat="51.501" lon="-0.119"/>
              <way id="10">
                <nd ref="1"/><nd ref="2"/>
                <tag k="highway" v="motorway"/>
              </way>
              <way id="11">
                <nd ref="1"/><nd ref="2"/>
                <tag k="building" v="yes"/>
              </way>
            </osm>
            """);

        try
        {
            var tool = new TileMapRenderTool();
            var defaultOut = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/city.osm" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            defaultOut.Delta.Log.Should().Contain(l => l.Contains("maps/city.png"));
            File.Exists(Path.Combine(root, "maps/city.png")).Should().BeTrue();

            var bboxOut = await tool.InvokeAsync(
                Call("repo.tile_map.render", new
                {
                    root,
                    osm_path = "maps/city.osm",
                    output_path = "maps/custom.png",
                    min_lat = 51.499,
                    max_lat = 51.502,
                    min_lon = -0.121,
                    max_lon = -0.118,
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            bboxOut.Delta.Log.Should().Contain(l => l.Contains("maps/custom.png"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_reports_empty_coordinates_and_rejects_bad_output_path()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "nodes-only.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="51.500" lon="-0.120"/>
            </osm>
            """);

        try
        {
            var tool = new TileMapRenderTool();
            var empty = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/nodes-only.osm" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            empty.Delta.Log.Should().Contain(l => l.Contains("no coordinates"));

            var badOut = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/nodes-only.osm", output_path = "../outside.png" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            badOut.Delta.Log.Should().Contain(l => l.Contains("REJECTED"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_reports_parse_error_for_invalid_xml()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-bad-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "broken.osm"), "<not-osm");

        try
        {
            var result = await new TileMapRenderTool().InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/broken.osm" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("parse_error"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_draws_highway_matrix_and_area_features()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-rich-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);

        var highwayTypes = new[]
        {
            "motorway", "trunk", "primary", "secondary", "tertiary", "residential",
            "service", "path", "railway", "unclassified",
        };

        var sb = new System.Text.StringBuilder("""
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="51.500" lon="-0.120"/>
              <node id="2" lat="51.501" lon="-0.119"/>
              <node id="3" lat="51.501" lon="-0.120"/>
              <node id="4" lat="51.500" lon="-0.119"/>
            """);

        var wayId = 10;
        foreach (var hw in highwayTypes)
        {
            sb.AppendLine($"""
                  <way id="{wayId}">
                    <nd ref="1"/><nd ref="2"/>
                    <tag k="highway" v="{hw}"/>
                  </way>
                """);
            wayId++;
        }

        sb.AppendLine("""
              <way id="100">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="leisure" v="park"/>
              </way>
              <way id="101">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="natural" v="water"/>
              </way>
              <way id="102">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="building" v="yes"/>
              </way>
              <way id="103">
                <nd ref="1"/><nd ref="2"/>
                <tag k="railway" v="rail"/>
              </way>
              <way id="104">
                <nd ref="1"/><nd ref="2"/>
                <tag k="waterway" v="river"/>
              </way>
            </osm>
            """);

        await File.WriteAllTextAsync(Path.Combine(mapsDir, "rich.osm"), sb.ToString());

        try
        {
            var tool = new TileMapRenderTool();
            var result = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/rich.osm", width = 800, height = 600 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("maps/rich.png"));
            new FileInfo(Path.Combine(root, "maps/rich.png")).Length.Should().BeGreaterThan(100);

            var badBbox = await tool.InvokeAsync(
                Call("repo.tile_map.render", new
                {
                    root,
                    osm_path = "maps/rich.osm",
                    output_path = "maps/bad-bbox.png",
                    min_lat = 51.502,
                    max_lat = 51.500,
                    min_lon = -0.121,
                    max_lon = -0.118,
                }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);
            badBbox.Delta.Log.Should().Contain(l => l.Contains("invalid bbox"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_clamps_dimensions_and_draws_remaining_landcover()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-land-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);

        await File.WriteAllTextAsync(Path.Combine(mapsDir, "land.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="40.0000" lon="-74.0000"/>
              <node id="2" lat="40.0000" lon="-73.9990"/>
              <node id="3" lat="40.0010" lon="-73.9990"/>
              <node id="4" lat="40.0010" lon="-74.0000"/>
              <way id="10">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="landuse" v="forest"/>
              </way>
              <way id="11">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="landuse" v="cemetery"/>
              </way>
              <way id="12">
                <nd ref="1"/><nd ref="2"/><nd ref="3"/><nd ref="4"/><nd ref="1"/>
                <tag k="leisure" v="garden"/>
              </way>
              <way id="13">
                <nd ref="1"/><nd ref="2"/>
                <tag k="waterway" v="stream"/>
              </way>
              <way id="14">
                <nd ref="1"/><nd ref="2"/>
                <tag k="highway" v="living_street"/>
              </way>
            </osm>
            """);

        try
        {
            var tool = new TileMapRenderTool();
            tool.Schema.Id.Should().Be("repo.tile_map.render");

            var result = await tool.InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/land.osm", width = 9000, height = 32 }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("4096x64"));
            new FileInfo(Path.Combine(root, "maps/land.png")).Length.Should().BeGreaterThan(50);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_reports_empty_coordinates()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "empty.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1"/>
              <way id="2"><nd ref="1"/><tag k="highway" v="road"/></way>
            </osm>
            """);

        try
        {
            var result = await new TileMapRenderTool().InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "empty.osm" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("no coordinates"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_honors_cancellation_during_parse()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-cancel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);

        var sb = new System.Text.StringBuilder("""
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
            """);
        for (var i = 0; i < 500; i++)
            sb.AppendLine($"""  <node id="{i}" lat="51.500" lon="-0.120"/>""");
        sb.AppendLine("</osm>");
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "big.osm"), sb.ToString());

        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await new TileMapRenderTool().InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/big.osm" }),
                WorldSnapshot.ForRepo(root),
                cts.Token);

            result.Delta.Log.Should().Contain(l => l.Contains("parse_error"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task TileMapRenderTool_reports_render_error_when_output_directory_not_writable()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
            return;

        var root = Path.Combine(Path.GetTempPath(), "ashlar-tile-ro-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var mapsDir = Path.Combine(root, "maps");
        Directory.CreateDirectory(mapsDir);
        var readOnlyDir = Path.Combine(mapsDir, "readonly");
        Directory.CreateDirectory(readOnlyDir);
        await File.WriteAllTextAsync(Path.Combine(mapsDir, "tiny.osm"), """
            <?xml version="1.0" encoding="UTF-8"?>
            <osm version="0.6">
              <node id="1" lat="51.500" lon="-0.120"/>
              <node id="2" lat="51.501" lon="-0.119"/>
              <way id="10">
                <nd ref="1"/><nd ref="2"/>
                <tag k="highway" v="residential"/>
              </way>
            </osm>
            """);

        try
        {
            File.SetUnixFileMode(readOnlyDir, UnixFileMode.UserRead | UnixFileMode.UserExecute);

            // Root bypasses DAC permission checks, so the directory is still writable
            // and this test's premise cannot hold. The dev container runs as root
            // (scripts/handoff/devbox.sh, deliberately, to avoid a UID mismatch on the
            // bind mount), which made this fail there long before it was moved into
            // this file. Probe for the premise rather than assuming it, so the test
            // still runs wherever it is meaningful — notably CI, which is non-root.
            //
            // NB: this bails with `return`, matching the OS guard at the top of the
            // method, which xUnit reports as PASSED rather than skipped. The assertion
            // below is therefore NOT exercised when running privileged - do not read a
            // green run in the dev container as evidence that this path works.
            if (CanCreateFileIn(readOnlyDir))
            {
                return;
            }

            var result = await new TileMapRenderTool().InvokeAsync(
                Call("repo.tile_map.render", new { root, osm_path = "maps/tiny.osm", output_path = "maps/readonly/out.png" }),
                WorldSnapshot.ForRepo(root),
                CancellationToken.None);

            result.Delta.Log.Should().Contain(l => l.Contains("render_error"));
        }
        finally
        {
            try { File.SetUnixFileMode(readOnlyDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); } catch { /* best effort */ }
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

}
