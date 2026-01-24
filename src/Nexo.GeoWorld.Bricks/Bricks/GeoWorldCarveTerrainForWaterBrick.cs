using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Projection;
using Nexo.GeoVector.Models;
using Nexo.GeoWorld.Terrain;
using Nexo.Infrastructure.Execution;

namespace Nexo.GeoWorld.Bricks;

/// <summary>
/// Applies deterministic water flattening + shoreline smoothing to an elevation grid.
/// </summary>
public sealed class GeoWorldCarveTerrainForWaterBrick : Brick
{
    private readonly IProviderFactory _llm;
    private readonly ICoordinateProjector _projector;
    private readonly ILogger<GeoWorldCarveTerrainForWaterBrick> _logger;

    public GeoWorldCarveTerrainForWaterBrick(IProviderFactory llm, ILogger<GeoWorldCarveTerrainForWaterBrick> logger)
        : this(llm, EquirectangularProjector.Instance, logger)
    {
    }

    public GeoWorldCarveTerrainForWaterBrick(IProviderFactory llm, ICoordinateProjector projector, ILogger<GeoWorldCarveTerrainForWaterBrick> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geoworld.terrain.carve-water";
        Name = "Carve Terrain For Water";
        Version = "0.1.0";
        Icon = "💧";
        Category = BrickCategory.Transform;
        Description = "Flattens water surfaces and optionally smooths shorelines on an ElevationGrid.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("terrainGrid", "ElevationGrid", "Input terrain grid"),
                new BrickInputDefinition("water", "GeoFeatureSet", "Water polygons", required: false),
                new BrickInputDefinition("origin", "GeoPoint", "Projection origin"),
                new BrickInputDefinition("enabled", "bool", "Enable water carving", required: false, defaultValue: true),
                new BrickInputDefinition("treatNoDataAsZero", "bool", "Treat NaN heights as 0", required: false, defaultValue: true),
                new BrickInputDefinition("shorelineSmoothIterations", "int", "Smoothing iterations near water", required: false, defaultValue: 2),
                new BrickInputDefinition("shorelineSmoothBlend", "float", "Blend factor 0..1", required: false, defaultValue: 0.5f),
                new BrickInputDefinition("waterSurfaceOffsetMeters", "float", "Offset applied to water height", required: false, defaultValue: 0.0f),
                new BrickInputDefinition("forceAgenticFail", "bool", "If true, agentic path throws (for fallback demos)", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("grid", "ElevationGrid", "Carved terrain grid")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "carve",
                Name = "Carve (Deterministic)",
                Description = "Deterministic terrain modification",
                Executor = "WaterTerrainCarver",
                Characteristics = new ImplementationCharacteristics
                {
                    Deterministic = true,
                    RequiresNetwork = false,
                    Latency = "< 2s",
                    ResourceUsage = ResourceUsage.Medium
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "tuned-carve",
                Name = "Tuned Carve (Agentic)",
                Description = "LLM may suggest smoothing parameters; carving stays deterministic",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Suggest shorelineSmoothIterations and shorelineSmoothBlend. Output JSON {\"shorelineSmoothIterations\":2,\"shorelineSmoothBlend\":0.5}.",
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
        var enabled = input.Get("enabled", true);
        var grid = input.Get<ElevationGrid>("terrainGrid");
        var water = input.Get<GeoFeatureSet?>("water", null);
        var origin = input.Get<GeoPoint>("origin");
        var treatNoData = input.Get("treatNoDataAsZero", true);
        var iters = input.Get("shorelineSmoothIterations", 2);
        var blend = input.Get("shorelineSmoothBlend", 0.5f);
        var offset = input.Get("waterSurfaceOffsetMeters", 0.0f);

        if (!enabled)
        {
            return Task.FromResult(new BrickOutput { ["grid"] = grid, Summary = "Carving disabled" });
        }

        var carved = WaterTerrainCarver.Carve(
            grid,
            water,
            origin,
            _projector,
            new WaterTerrainCarverOptions
            {
                TreatNoDataAsZero = treatNoData,
                FlattenWater = true,
                WaterSurfaceOffsetMeters = offset,
                ShorelineSmoothIterations = Math.Max(0, iters),
                ShorelineSmoothBlend = blend
            });

        return Task.FromResult(new BrickOutput
        {
            ["grid"] = carved,
            Summary = $"Carved grid: {carved.Width}x{carved.Height}"
        });
    }

    private async Task<BrickOutput> ExecuteAgenticAsync(BrickInput input, IExecutionContext context, CancellationToken ct)
    {
        if (input.Get("forceAgenticFail", false))
        {
            throw new InvalidOperationException("Forced agentic failure (GeoWorldCarveTerrainForWaterBrick).");
        }

        try
        {
            _ = await _llm.ExecuteLLMAsync(
                context.Provider,
                Implementations.Agentic!.LLMConfig.SystemPrompt,
                "Return JSON {\"shorelineSmoothIterations\":2,\"shorelineSmoothBlend\":0.5}.",
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

