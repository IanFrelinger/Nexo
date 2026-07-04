using FluentAssertions;
using Nexo.Commercial.GameDomain.Mapping;
using Xunit;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for vector map payload summarizer.</summary>
public sealed class VectorMapPayloadSummarizerTests
{
    [Fact]
    public void Summarize_GeoJSON_CountsGeometryTypes()
    {
        var utf8 = """{"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"Point","coordinates":[1,2]}},{"type":"Feature","geometry":{"type":"LineString","coordinates":[[0,0],[1,1]]}}]}"""u8.ToArray();
        var insp = VectorMapPayloadInspector.Inspect(utf8, "application/geo+json");
        var s = VectorMapPayloadSummarizer.Summarize(utf8, insp, null);
        s.ParserKind.Should().Be("geojson");
        s.Summary.Should().Contain("2 feature");
        s.Summary.Should().Contain("Point");
        s.Summary.Should().Contain("LineString");
    }

    [Fact]
    public void Summarize_OsmXml_CountsElements()
    {
        var xml = """
                   <?xml version="1.0"?>
                   <osm version="0.6">
                     <node id="1" lat="0" lon="0"><tag k="amenity" v="cafe"/></node>
                     <way id="10"><nd ref="1"/><tag k="highway" v="primary"/></way>
                   </osm>
                   """u8.ToArray();
        var insp = VectorMapPayloadInspector.Inspect(xml, null);
        var s = VectorMapPayloadSummarizer.Summarize(xml, insp, null);
        s.ParserKind.Should().Be("osm_xml");
        s.Summary.Should().Contain("nodes=1");
        s.Summary.Should().Contain("ways=1");
        s.Summary.Should().Contain("ways-with-tags=1");
        s.Details.Should().NotBeEmpty();
    }

    [Fact]
    public void Summarize_GeoJsonArray_CountsBareGeometries()
    {
        var utf8 =
            """[{"type":"Point","coordinates":[1,2]},{"type":"LineString","coordinates":[[0,0],[1,1]]}]"""u8.ToArray();
        var insp = VectorMapPayloadInspector.Inspect(utf8, "application/json");
        var s = VectorMapPayloadSummarizer.Summarize(utf8, insp, null);
        s.ParserKind.Should().Be("geojson");
        s.Summary.Should().Contain("Point");
        s.Summary.Should().Contain("LineString");
    }
}
