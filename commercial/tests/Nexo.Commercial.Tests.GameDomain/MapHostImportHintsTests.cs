using FluentAssertions;
using Nexo.GameDomain.Mapping;
using Xunit;

namespace Nexo.Tests.GameDomain;

public sealed class MapHostImportHintsTests
{
    [Fact]
    public void FromParseSummary_ReturnsLines_ForMvt()
    {
        var h = MapHostImportHints.FromParseSummary(new VectorMapParseSummary("mvt", "x", []));
        h.Should().NotBeEmpty();
        h.Should().Contain(s => s.Contains("layer", StringComparison.OrdinalIgnoreCase));
    }
}
