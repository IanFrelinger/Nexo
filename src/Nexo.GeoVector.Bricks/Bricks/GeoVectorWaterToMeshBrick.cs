using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.GeoTerrain.Projection;
using Nexo.GeoVector.Generation;
using Nexo.GeoVector.Models;
using Nexo.Infrastructure.Execution;

namespace Nexo.GeoVector.Bricks;

/// <summary>
/// Convert water polygon features to a surface mesh (deterministic).
/// </summary>
public sealed class GeoVectorWaterToMeshBrick : Brick
{
    private readonly IProviderFactory _llm;
    private readonly ICoordinateProjector _projector;
    private readonly ILogger<GeoVectorWaterToMeshBrick> _logger;

    public GeoVectorWaterToMeshBrick(IProviderFactory llm, ILogger<GeoVectorWaterToMeshBrick> logger)
        : this(llm, EquirectangularProjector.Instance, logger)
    {
    }

    public GeoVectorWaterToMeshBrick(IProviderFactory llm, ICoordinateProjector projector, ILogger<GeoVectorWaterToMeshBrick> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geovector.water.to-mesh";
        Name = "Water To Mesh";
        Version = "0.1.0";
        Icon = "🌊";
        Category = BrickCategory.Generation;
        Description = "Triangulates water polygons into a surface mesh.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("features", "GeoFeatureSet", "Input water features"),
                new BrickInputDefinition("origin", "GeoPoint", "Local projection origin point"),
                new BrickInputDefinition("generateTexCoords", "bool", "If true, generate UVs", required: false, defaultValue: true),
                new BrickInputDefinition("uvMetersPerRepeat", "float", "Meters per texture repeat", required: false, defaultValue: 5.0f),
                new BrickInputDefinition("conformToTerrain", "bool", "If true, sample terrain height", required: false, defaultValue: false),
                new BrickInputDefinition("flattenToTerrain", "bool", "If true, flatten polygon to a single sampled terrain height", required: false, defaultValue: false),
                new BrickInputDefinition("surfaceOffsetMeters", "float", "Offset surface when conforming", required: false, defaultValue: 0.0f),
                new BrickInputDefinition("terrainGrid", "ElevationGrid", "Optional terrain grid for conformance", required: false),
                new BrickInputDefinition("terrainTreatNoDataAsZero", "bool", "If true, NaN terrain samples become 0m", required: false, defaultValue: true),
                new BrickInputDefinition("forceAgenticFail", "bool", "If true, agentic path throws (for fallback demos)", required: false, defaultValue: false)
            ],
            Outputs =
            [
                new BrickOutputDefinition("mesh", "MeshData", "Generated mesh")
            ]
        };

        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "triangulate",
                Name = "Triangulate (Deterministic)",
                Description = "Deterministic ear-clipped polygon triangulation",
                Executor = "WaterMeshGenerator",
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
                Id = "tuned-water",
                Name = "Tuned Water (Agentic)",
                Description = "LLM may suggest UV scale; generation stays deterministic",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Suggest uvMetersPerRepeat for water. Output JSON {\"uvMetersPerRepeat\":5.0}.",
                    Temperature = 0.0,
                    MaxTokens = 120
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
        var features = input.Get<GeoFeatureSet>("features");
        var origin = input.Get<GeoPoint>("origin");
        var generateTexCoords = input.Get("generateTexCoords", true);
        var uvMetersPerRepeat = input.Get("uvMetersPerRepeat", 5.0f);
        var conform = input.Get("conformToTerrain", false);
        var flatten = input.Get("flattenToTerrain", false);
        var offset = input.Get("surfaceOffsetMeters", 0.0f);
        var terrainGrid = input.Get<ElevationGrid?>("terrainGrid", null);
        var treatNoData = input.Get("terrainTreatNoDataAsZero", true);

        var mesh = WaterMeshGenerator.GenerateWater(
            features,
            origin,
            conform ? terrainGrid : null,
            new WaterMeshGenerationOptions
            {
                GenerateTexCoords = generateTexCoords,
                UvMetersPerRepeat = uvMetersPerRepeat,
                ConformToTerrain = conform,
                FlattenToTerrain = flatten,
                SurfaceOffsetMeters = offset,
                TreatNoDataAsZero = treatNoData
            },
            _projector);

        return Task.FromResult(new BrickOutput
        {
            ["mesh"] = mesh,
            Summary = $"Mesh: {mesh.Vertices.Count} verts, {mesh.Indices.Count / 3} tris"
        });
    }

    private async Task<BrickOutput> ExecuteAgenticAsync(BrickInput input, IExecutionContext context, CancellationToken ct)
    {
        if (input.Get("forceAgenticFail", false))
        {
            throw new InvalidOperationException("Forced agentic failure (GeoVectorWaterToMeshBrick).");
        }

        var uv = input.Get("uvMetersPerRepeat", 5.0f);
        try
        {
            _ = await _llm.ExecuteLLMAsync(
                context.Provider,
                Implementations.Agentic!.LLMConfig.SystemPrompt,
                $"Current uvMetersPerRepeat={uv}. Return JSON {{\"uvMetersPerRepeat\":5.0}}.",
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

