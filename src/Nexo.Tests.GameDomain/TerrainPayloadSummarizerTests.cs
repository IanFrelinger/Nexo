using FluentAssertions;
using Nexo.GameDomain.Mapping;
using Xunit;

namespace Nexo.Tests.GameDomain;

public sealed class TerrainPayloadSummarizerTests
{
    private const string Png1x1B64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    [Fact]
    public void Summarize_Png1x1_YieldsDimensions()
    {
        var bytes = Convert.FromBase64String(Png1x1B64);
        var s = TerrainPayloadSummarizer.Summarize(bytes, "image/png");
        s.ParserKind.Should().Be("png");
        s.Summary.Should().Contain("1x1");
        s.Summary.Should().Contain("PNG");
    }

    [Fact]
    public void TryReadPngDimensions_CanRead1x1()
    {
        var bytes = Convert.FromBase64String(Png1x1B64);
        var ok = TerrainPayloadSummarizer.TryReadPngDimensions(bytes, out var w, out var h, out var bd, out var ct);
        ok.Should().BeTrue();
        w.Should().Be(1);
        h.Should().Be(1);
        bd.Should().Be(8);
    }
}
