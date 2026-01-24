using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoVector.Models;
using Nexo.GeoWorld.Materials;

namespace Nexo.GeoWorld.Bricks;

/// <summary>
/// Emits materials.json for Unity import (deterministic).
/// </summary>
public sealed class GeoWorldMaterialsJsonBrick : Brick
{
    private readonly ILogger<GeoWorldMaterialsJsonBrick> _logger;

    public GeoWorldMaterialsJsonBrick(ILogger<GeoWorldMaterialsJsonBrick> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoworld.export.materials-json";
        Name = "Materials Export (JSON Text)";
        Version = "0.1.0";
        Icon = "🎨";
        Category = BrickCategory.Output;
        Description = "Infers coarse materials and writes materials.json text.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("buildings", "GeoFeatureSet", "Building features", required: false),
                new BrickInputDefinition("roads", "GeoFeatureSet", "Road features", required: false),
                new BrickInputDefinition("water", "GeoFeatureSet", "Water features", required: false),
                new BrickInputDefinition("buildingsUvMetersPerRepeat", "float", "UV scale for buildings", required: false),
                new BrickInputDefinition("roadsUvMetersPerRepeat", "float", "UV scale for roads", required: false),
                new BrickInputDefinition("waterUvMetersPerRepeat", "float", "UV scale for water", required: false),
                new BrickInputDefinition("buildingsBaseColorTexturePath", "string", "Optional buildings basecolor texture path", required: false),
                new BrickInputDefinition("roadsBaseColorTexturePath", "string", "Optional roads basecolor texture path", required: false),
                new BrickInputDefinition("waterBaseColorTexturePath", "string", "Optional water basecolor texture path", required: false),
                new BrickInputDefinition("terrainBaseColorTexturePath", "string", "Optional terrain basecolor texture path", required: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("jsonText", "string", "JSON text")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "json",
                Name = "Materials JSON",
                Description = "Deterministic JSON generator",
                Executor = "MaterialInference",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false,
                    Latency = "< 100ms",
                    ResourceUsage = ResourceUsage.Low
                }
            }
        };

        DefaultImplementation = ImplementationType.Deterministic;
        FallbackChain = [ImplementationType.Deterministic];
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var buildings = input.Get<GeoFeatureSet?>("buildings", null);
        var roads = input.Get<GeoFeatureSet?>("roads", null);
        var water = input.Get<GeoFeatureSet?>("water", null);

        float? bUv = null;
        float? rUv = null;
        float? wUv = null;
        try { bUv = input.Get<float?>("buildingsUvMetersPerRepeat", null); } catch { /* ignore */ }
        try { rUv = input.Get<float?>("roadsUvMetersPerRepeat", null); } catch { /* ignore */ }
        try { wUv = input.Get<float?>("waterUvMetersPerRepeat", null); } catch { /* ignore */ }

        string? bTex = null;
        string? rTex = null;
        string? wTex = null;
        string? tTex = null;
        try { bTex = input.Get<string?>("buildingsBaseColorTexturePath", null); } catch { /* ignore */ }
        try { rTex = input.Get<string?>("roadsBaseColorTexturePath", null); } catch { /* ignore */ }
        try { wTex = input.Get<string?>("waterBaseColorTexturePath", null); } catch { /* ignore */ }
        try { tTex = input.Get<string?>("terrainBaseColorTexturePath", null); } catch { /* ignore */ }
        if (string.IsNullOrWhiteSpace(bTex)) bTex = null;
        if (string.IsNullOrWhiteSpace(rTex)) rTex = null;
        if (string.IsNullOrWhiteSpace(wTex)) wTex = null;
        if (string.IsNullOrWhiteSpace(tTex)) tTex = null;

        var list = new List<MaterialAssignment>(capacity: 8);
        if (buildings != null) list.AddRange(MaterialInference.InferForBuildings(buildings, bUv, bTex));
        if (roads != null) list.AddRange(MaterialInference.InferForRoads(roads, rUv, rTex));
        if (water != null) list.AddRange(MaterialInference.InferForWater(wUv, wTex));
        // Terrain assignment is optional (Unity importer can special-case terrain); include if texture provided.
        if (!string.IsNullOrWhiteSpace(tTex)) list.AddRange(MaterialInference.InferForTerrain(null, tTex));

        var json = WorldBundleWriters.WriteMaterials(list);
        _logger.LogInformation("materials.json generated: chars={Chars}", json.Length);

        return Task.FromResult(new BrickOutput
        {
            ["jsonText"] = json,
            Summary = $"materials.json chars={json.Length}"
        });
    }
}

