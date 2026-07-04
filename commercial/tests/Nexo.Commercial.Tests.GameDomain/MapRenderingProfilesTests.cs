using FluentAssertions;
using Nexo.Commercial.GameDomain.Aesthetics;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>
/// Guards stable string identifiers used by hosts to branch map pipelines.
/// </summary>
public class MapRenderingProfilesTests
{
    [Fact]
    public void KnownProfiles_AreDistinctSnakeCaseStrings()
    {
        var profiles = new[]
        {
            MapRenderingProfiles.Auto,
            MapRenderingProfiles.VoxelGrid,
            MapRenderingProfiles.HeightfieldMesh,
            MapRenderingProfiles.FlatShadedPolys,
            MapRenderingProfiles.BillboardFeatures,
            MapRenderingProfiles.VectorOverlay,
            MapRenderingProfiles.OrthographicTile,
        };

        profiles.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p));
        profiles.Should().OnlyContain(p => p == p.ToLowerInvariant());
        profiles.Should().OnlyContain(p => !p.Contains(' ', StringComparison.Ordinal));
        profiles.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Auto_IsLiteral_auto()
    {
        MapRenderingProfiles.Auto.Should().Be("auto");
    }
}
