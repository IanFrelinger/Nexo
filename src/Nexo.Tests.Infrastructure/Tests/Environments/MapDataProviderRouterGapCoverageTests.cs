using FluentAssertions;
using Nexo.Core.Application.Environments;
using Nexo.Core.Application.Environments.Ports;
using Nexo.Infrastructure.Environments;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Environments;

public sealed class MapDataProviderRouterGapCoverageTests
{
    [Fact]
    public void ResolveVector_trims_kind_before_lookup()
    {
        var router = new MapDataProviderRouter([new StubVector(MapDataSourceKinds.Http)], [], []);

        var provider = router.ResolveVector(new MapDataSourceBinding(Kind: $"  {MapDataSourceKinds.Http}  "));

        provider.Kind.Should().Be(MapDataSourceKinds.Http);
    }

    [Fact]
    public void ResolveVector_throws_for_unknown_kind()
    {
        var router = new MapDataProviderRouter([new StubVector(MapDataSourceKinds.Http)], [], []);

        var act = () => router.ResolveVector(new MapDataSourceBinding(Kind: "missing"));

        act.Should().Throw<KeyNotFoundException>().WithMessage("*vector map*");
    }

    [Fact]
    public void Constructor_rejects_empty_vector_kind()
    {
        var act = () => new MapDataProviderRouter([new StubVector("   ")], [], []);

        act.Should().Throw<InvalidOperationException>().WithMessage("*empty Kind*");
    }

    [Fact]
    public void Constructor_rejects_duplicate_vector_kind()
    {
        var act = () => new MapDataProviderRouter(
            [new StubVector(MapDataSourceKinds.Http), new StubVector(MapDataSourceKinds.Http)],
            [],
            []);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate*");
    }

    [Fact]
    public void ResolveTerrain_and_voxel_use_case_insensitive_kind()
    {
        var terrain = new StubTerrain("dem");
        var voxel = new StubVoxel("chunk-store");
        var router = new MapDataProviderRouter([], [terrain], [voxel]);

        router.ResolveTerrain(new MapDataSourceBinding(Kind: "DEM")).Should().BeSameAs(terrain);
        router.ResolveVoxelStore(new MapDataSourceBinding(Kind: "CHUNK-STORE")).Should().BeSameAs(voxel);
    }

    private sealed class StubVector(string kind) : IVectorMapDataProvider
    {
        public string Kind { get; } = kind;

        public Task<VectorMapDataResult> FetchAsync(
            MapDataGeographicBounds bounds,
            MapDataSourceBinding binding,
            MapDataRequestContext context,
            string formatHint,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VectorMapDataResult(ReadOnlyMemory<byte>.Empty, "application/octet-stream"));
    }

    private sealed class StubTerrain(string kind) : ITerrainMapDataProvider
    {
        public string Kind { get; } = kind;

        public Task<TerrainMapDataResult> FetchAsync(
            MapDataGeographicBounds bounds,
            MapDataSourceBinding binding,
            MapDataRequestContext context,
            double resolutionMetersHint,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new TerrainMapDataResult(ReadOnlyMemory<byte>.Empty, "application/octet-stream"));
    }

    private sealed class StubVoxel(string kind) : IVoxelChunkDataProvider
    {
        public string Kind { get; } = kind;

        public Task<VoxelChunkDataResult> FetchAsync(
            VoxelChunkKey key,
            MapDataSourceBinding binding,
            MapDataRequestContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VoxelChunkDataResult(Found: false));
    }
}
