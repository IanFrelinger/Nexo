using FluentAssertions;
using Ashlar.Commercial.GameDomain.Mapping;
using Xunit;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for terrain host import hints.</summary>
public sealed class TerrainHostImportHintsTests
{
    [Fact]
    public void FromTerrainSummary_ReturnsLines()
    {
        var s = new TerrainParseSummary("png", "1x1 PNG", ["content-type=image/png"]);
        var lines = TerrainHostImportHints.FromTerrainSummary(s);
        lines.Should().HaveCountGreaterThan(2);
        lines[0].Should().Contain("png");
    }
}
