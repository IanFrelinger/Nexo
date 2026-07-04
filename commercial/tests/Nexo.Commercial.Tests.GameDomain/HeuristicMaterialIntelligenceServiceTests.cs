using FluentAssertions;
using Nexo.Commercial.GameDomain.Aesthetics;
using Nexo.Commercial.GameDomain.Materials;
using Xunit;

namespace Nexo.Commercial.Tests.GameDomain;
/// <summary>Tests for heuristic material intelligence service.</summary>
public sealed class HeuristicMaterialIntelligenceServiceTests
{
    private readonly HeuristicMaterialIntelligenceService _sut = new();

    [Fact]
    public async Task SuggestAsync_IncludesPaletteRoles()
    {
        var pack = new AestheticPack
        {
            Id = "test",
            GeometryStrategy = "voxel",
            MapRenderingProfile = MapRenderingProfiles.VoxelGrid,
            DefaultPaletteColors = ["#111111", "#222222"],
            LodLevels = [new LodLevel(0, 1)]
        };
        var r = await _sut.SuggestAsync(pack, null);
        r.Hints.Should().Contain(h => h.Role == "terrain_base");
        r.Hints.Should().Contain(h => h.Role == "lod_budget");
        r.Summary.Should().Contain("test");
    }

    [Fact]
    public async Task SuggestAsync_Mvt_AddsLayerHint()
    {
        var pack = new AestheticPack { Id = "x", GeometryStrategy = "low_poly" };
        var r = await _sut.SuggestAsync(pack, "mvt");
        r.Hints.Should().Contain(h => h.Role == "vector_layers");
    }
}
