using System.Text.Json;
using FluentAssertions;
using Nexo.Core.Application.Environments;
using Nexo.GameDomain.Environments;

namespace Nexo.Tests.GameDomain;

public sealed class VoxelEnvironmentManifestTests
{
    [Fact]
    public void VoxelEnvironmentMath_counts_voxels_per_chunk()
    {
        var tier = new VoxelLodTier(0, 1.0, VoxelSizeMeters: 0.5, ChunkEdgeVoxels: 16);
        VoxelEnvironmentMath.ApproximateVoxelsPerChunk(tier).Should().Be(4096);
        VoxelEnvironmentMath.ChunkExtentMeters(tier).Should().Be(8.0);
        VoxelEnvironmentMath.VoxelsPerMeter(tier).Should().Be(2.0);
    }

    [Fact]
    public void Manifest_serializes_roundtrip_with_lod_pyramid()
    {
        var manifest = new VoxelEnvironmentManifest
        {
            Id = "demo-city",
            DisplayName = "Demo city blocks",
            SpatialReferenceId = "EPSG:4326",
            OriginLatitude = 51.0,
            OriginLongitude = 7.0,
            LodTiers =
            [
                new VoxelLodTier(0, 1.0, 0.25, 64, "lod0", "{layer}/{cx}/{cy}/{cz}.bin"),
                new VoxelLodTier(1, 0.5, 0.5, 64, "lod1", "{layer}/{cx}/{cy}/{cz}.bin"),
                new VoxelLodTier(2, 0.25, 1.0, 32, "lod2", "{layer}/{cx}/{cy}/{cz}.bin")
            ],
            AdaptationHints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["unity.streaming_root"] = "Assets/StreamingAssets/Voxels/demo-city"
            },
            VectorData = new MapDataSourceBinding(
                Kind: MapDataSourceKinds.Overpass,
                BaseUrl: "https://overpass.mycompany.internal/api/interpreter",
                Headers: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Authorization"] = "Bearer ***"
                },
                Options: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["timeout_seconds"] = "120"
                }),
            TerrainData = new MapDataSourceBinding(
                Kind: MapDataSourceKinds.TerrainTiles,
                BaseUrl: "https://terrain.mycompany.internal"),
            VoxelChunkStore = new MapDataSourceBinding(
                Kind: MapDataSourceKinds.VoxelStore,
                Options: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["local_root"] = "/var/lib/nexo/voxels/demo-city"
                }),
            Intelligence = new MapIntelligenceOptions
            {
                RefineVectorWithEmbeddedAi = true,
                SuggestMaterialsWithEmbeddedAi = true,
                VerifyPerLodTier = true,
                TilingDownVerification = true
            }
        };

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        var json = JsonSerializer.Serialize(manifest, options);
        var restored = JsonSerializer.Deserialize<VoxelEnvironmentManifest>(json, options);

        restored.Should().NotBeNull();
        restored!.Id.Should().Be("demo-city");
        restored.LodTiers.Should().HaveCount(3);
        restored.LodTiers[0].TierIndex.Should().Be(0);
        restored.LodTiers[0].VoxelSizeMeters.Should().Be(0.25);
        restored.LodTiers[2].ChunkEdgeVoxels.Should().Be(32);
        restored.AdaptationHints["unity.streaming_root"].Should().Contain("demo-city");
        restored.VectorData.Should().NotBeNull();
        restored.VectorData!.Kind.Should().Be(MapDataSourceKinds.Overpass);
        restored.VectorData.BaseUrl.Should().Contain("overpass.mycompany");
        restored.TerrainData!.Kind.Should().Be(MapDataSourceKinds.TerrainTiles);
        restored.VoxelChunkStore!.Options.Should().ContainKey("local_root");
        restored.Intelligence.Should().NotBeNull();
        restored.Intelligence!.RefineVectorWithEmbeddedAi.Should().BeTrue();
        restored.Intelligence.TilingDownVerification.Should().BeTrue();
    }
}
