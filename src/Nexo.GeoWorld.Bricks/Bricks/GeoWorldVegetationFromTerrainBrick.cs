using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Projection;
using Nexo.GeoVector.Models;
using Nexo.GeoWorld.Vegetation;
using Nexo.Infrastructure.Execution;

namespace Nexo.GeoWorld.Bricks;

/// <summary>
/// Places vegetation instances using terrain + vector masks (deterministic-first).
/// </summary>
public sealed class GeoWorldVegetationFromTerrainBrick : Brick
{
    private readonly IProviderFactory _llm;
    private readonly ICoordinateProjector _projector;
    private readonly ILogger<GeoWorldVegetationFromTerrainBrick> _logger;

    public GeoWorldVegetationFromTerrainBrick(IProviderFactory llm, ILogger<GeoWorldVegetationFromTerrainBrick> logger)
        : this(llm, EquirectangularProjector.Instance, logger)
    {
    }

    public GeoWorldVegetationFromTerrainBrick(IProviderFactory llm, ICoordinateProjector projector, ILogger<GeoWorldVegetationFromTerrainBrick> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoworld.vegetation.from-terrain";
        Name = "Vegetation Placement";
        Version = "0.1.0";
        Icon = "🌲";
        Category = BrickCategory.Generation;
        Description = "Places vegetation instances constrained by landcover masks and exclusions.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("terrainGrid", "ElevationGrid", "Terrain elevation grid"),
                new BrickInputDefinition("vegetation", "GeoFeatureSet", "Vegetation polygons (include masks)", required: false),
                new BrickInputDefinition("buildings", "GeoFeatureSet", "Building polygons (exclude + distance)", required: false),
                new BrickInputDefinition("roads", "GeoFeatureSet", "Road polylines (distance)", required: false),
                new BrickInputDefinition("water", "GeoFeatureSet", "Water polygons (exclude)", required: false),
                new BrickInputDefinition("origin", "GeoPoint", "Optional origin override for projection", required: false),
                new BrickInputDefinition("seed", "int", "RNG seed", required: false, defaultValue: 12345),
                new BrickInputDefinition("instancesPerSqKm", "float", "Density (instances per square km)", required: false, defaultValue: 250.0f),
                new BrickInputDefinition("minSpacingMeters", "float", "Min distance between instances", required: false, defaultValue: 2.0f),
                new BrickInputDefinition("maxSlopeDegrees", "float", "Reject slopes steeper than this", required: false, defaultValue: 35.0f),
                new BrickInputDefinition("kind", "string", "Instance kind name", required: false, defaultValue: "tree"),
                new BrickInputDefinition("minScale", "float", "Min uniform scale", required: false, defaultValue: 0.8f),
                new BrickInputDefinition("maxScale", "float", "Max uniform scale", required: false, defaultValue: 1.2f),
                new BrickInputDefinition("minDistToRoad", "float", "Min distance to roads", required: false, defaultValue: 2.0f),
                new BrickInputDefinition("minDistToBuilding", "float", "Min distance to buildings", required: false, defaultValue: 1.0f),
                new BrickInputDefinition("forceAgenticFail", "bool", "If true, agentic path throws (for fallback demos)", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("instances", "SceneryInstance[]", "Placed instances")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "place",
                Name = "Place (Deterministic)",
                Description = "Deterministic placement with masks and rejection sampling",
                Executor = "VegetationPlacer",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false,
                    Latency = "< 1s",
                    ResourceUsage = ResourceUsage.Low
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "tuned-place",
                Name = "Tuned Place (Agentic)",
                Description = "LLM may suggest density/spacing; placement stays deterministic",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Suggest instancesPerSqKm and minSpacingMeters. Output JSON {\"instancesPerSqKm\":250,\"minSpacingMeters\":2}.",
                    Temperature = 0.0,
                    MaxTokens = 160
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = false,
                    RequiresNetwork = true,
                    Latency = "< 2s offline; longer on real providers",
                    ResourceUsage = ResourceUsage.Low
                }
            }
        };

        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = [ImplementationType.Agentic, ImplementationType.Deterministic];
    }

    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        return implementation switch
        {
            ImplementationType.Deterministic => await ExecuteDeterministicAsync(input, cancellationToken),
            ImplementationType.Agentic => await ExecuteAgenticAsync(input, context, cancellationToken),
            _ => throw new ArgumentException($"Unsupported implementation: {implementation}")
        };
    }

    private Task<BrickOutput> ExecuteDeterministicAsync(BrickInput input, CancellationToken ct)
    {
        var terrain = input.Get<ElevationGrid>("terrainGrid");
        var vegetation = input.Get<GeoFeatureSet?>("vegetation", null);
        var buildings = input.Get<GeoFeatureSet?>("buildings", null);
        var roads = input.Get<GeoFeatureSet?>("roads", null);
        var water = input.Get<GeoFeatureSet?>("water", null);
        var origin = input.Get<GeoPoint?>("origin", null);

        var seed = input.Get("seed", 12345);
        var density = input.Get("instancesPerSqKm", 250.0f);
        var spacing = input.Get("minSpacingMeters", 2.0f);
        var maxSlope = input.Get("maxSlopeDegrees", 35.0f);
        var kind = input.Get("kind", "tree") ?? "tree";
        var minScale = input.Get("minScale", 0.8f);
        var maxScale = input.Get("maxScale", 1.2f);
        var minRoad = input.Get("minDistToRoad", 2.0f);
        var minBld = input.Get("minDistToBuilding", 1.0f);

        var instances = VegetationPlacer.Place(
            terrain,
            vegetation,
            buildings,
            water,
            roads,
            new VegetationPlacementOptions
            {
                Seed = seed,
                InstancesPerSquareKm = density,
                MinInstanceSpacingMeters = spacing,
                MaxSlopeDegrees = maxSlope,
                Kind = kind,
                MinUniformScale = minScale,
                MaxUniformScale = maxScale,
                MinDistanceToRoadMeters = minRoad,
                MinDistanceToBuildingMeters = minBld
            },
            _projector,
            origin).ToArray();

        return Task.FromResult(new BrickOutput
        {
            ["instances"] = instances,
            Summary = $"Instances: {instances.Length}"
        });
    }

    private async Task<BrickOutput> ExecuteAgenticAsync(BrickInput input, IExecutionContext context, CancellationToken ct)
    {
        if (input.Get("forceAgenticFail", false))
        {
            throw new InvalidOperationException("Forced agentic failure (GeoWorldVegetationFromTerrainBrick).");
        }

        try
        {
            _ = await _llm.ExecuteLLMAsync(
                context.Provider,
                Implementations.Agentic!.LLMConfig.SystemPrompt,
                "Return JSON {\"instancesPerSqKm\":250,\"minSpacingMeters\":2}.",
                Implementations.Agentic.LLMConfig,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LLM tuning failed; proceeding with input defaults.");
        }

        return await ExecuteDeterministicAsync(input, ct);
    }
}

