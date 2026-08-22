using FluentAssertions;
using Ashlar.Commercial.GameDomain.Mapping;
using Xunit;

namespace Ashlar.Commercial.Tests.GameDomain;
/// <summary>Tests for map host import hints.</summary>
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
