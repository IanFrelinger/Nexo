using FluentAssertions;
using Nexo.GameDomain.Mapping;
using Xunit;

namespace Nexo.Tests.GameDomain;

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
