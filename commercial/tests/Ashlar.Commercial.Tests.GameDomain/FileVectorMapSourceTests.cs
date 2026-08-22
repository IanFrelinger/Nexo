using FluentAssertions;
using Ashlar.Commercial.GameDomain.Mapping;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for file vector map source.</summary>
public class FileVectorMapSourceTests
{
    [Fact]
    public async Task InspectAsync_GeoJSON_SniffsFormat()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-map-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var rel = "sample.geojson";
            var geo = """{"type":"FeatureCollection","features":[]}""";
            await File.WriteAllTextAsync(Path.Combine(root, rel), geo);

            var r = await FileVectorMapSource.InspectAsync(root, rel, CancellationToken.None);
            r.Ok.Should().BeTrue();
            r.FormatHint.Should().Be("geojson");
            r.ByteLength.Should().BeGreaterThan(0);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task InspectAsync_MinimalOsm_SniffsXml()
    {
        var root = Path.Combine(Path.GetTempPath(), "ashlar-osm-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var rel = "mini.osm";
            var osm = """<?xml version="1.0"?><osm version="0.6"></osm>""";
            await File.WriteAllTextAsync(Path.Combine(root, rel), osm);

            var r = await FileVectorMapSource.InspectAsync(root, rel, CancellationToken.None);
            r.Ok.Should().BeTrue();
            r.FormatHint.Should().Be("osm_xml");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void TryResolveUnderRoot_RejectsTraversal()
    {
        var root = Path.GetTempPath();
        FileVectorMapSource.TryResolveUnderRoot(root, "../secret", out _, out var err).Should().BeFalse();
        err.Should().Contain("traversal");
    }

    [Fact]
    public void TryResolveUnderRoot_RejectsUnixAbsolutePath()
    {
        var root = Path.GetTempPath();
        FileVectorMapSource.TryResolveUnderRoot(root, "/etc/passwd", out _, out var err).Should().BeFalse();
        err.Should().Contain("absolute");
    }
}
