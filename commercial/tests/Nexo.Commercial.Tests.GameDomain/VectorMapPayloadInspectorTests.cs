using FluentAssertions;
using Nexo.GameDomain.Mapping;
using Xunit;

namespace Nexo.Tests.GameDomain;

public sealed class VectorMapPayloadInspectorTests
{
    [Fact]
    public void Inspect_GzipPrefix_ClassifiesAsMvtOrBinary()
    {
        var bytes = new byte[] { 0x1f, 0x8b, 0x00, 0x01 };
        var r = VectorMapPayloadInspector.Inspect(bytes, null);
        r.FormatGuess.Should().Be("mapbox_vector_tile_or_binary");
    }

    [Fact]
    public void Inspect_JsonPrefix()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("  { \"type\": \"FeatureCollection\" }");
        var r = VectorMapPayloadInspector.Inspect(bytes, "application/json");
        r.FormatGuess.Should().Be("json_vector");
    }
}
