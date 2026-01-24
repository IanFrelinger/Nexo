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
/// Convert road polyline features to a ribbon mesh (deterministic).
/// </summary>
public sealed class GeoVectorRoadsToMeshBrick : Brick
{
    private readonly IProviderFactory _llm;
    private readonly ICoordinateProjector _projector;
    private readonly ILogger<GeoVectorRoadsToMeshBrick> _logger;

    public GeoVectorRoadsToMeshBrick(IProviderFactory llm, ILogger<GeoVectorRoadsToMeshBrick> logger)
        : this(llm, EquirectangularProjector.Instance, logger)
    {
    }

    public GeoVectorRoadsToMeshBrick(IProviderFactory llm, ICoordinateProjector projector, ILogger<GeoVectorRoadsToMeshBrick> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geovector.roads.to-mesh";
        Name = "Roads To Mesh";
        Version = "0.1.0";
        Icon = "🛣️";
        Category = BrickCategory.Generation;
        Description = "Generates a ribbon mesh from road polylines.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("features", "GeoFeatureSet", "Input road features"),
                new BrickInputDefinition("origin", "GeoPoint", "Local projection origin point"),
                new BrickInputDefinition("widthMeters", "float", "Road width in meters", required: false, defaultValue: 4.0f),
                new BrickInputDefinition("generateTexCoords", "bool", "If true, generate UVs", required: false, defaultValue: true),
                new BrickInputDefinition("uvMetersPerRepeat", "float", "Meters per texture repeat", required: false, defaultValue: 1.0f),
                new BrickInputDefinition("conformToTerrain", "bool", "If true, sample terrain height", required: false, defaultValue: false),
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
                Id = "ribbon",
                Name = "Ribbon (Deterministic)",
                Description = "Deterministic ribbon mesh generator",
                Executor = "RoadMeshGenerator",
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
                Id = "tuned-ribbon",
                Name = "Tuned Ribbon (Agentic)",
                Description = "LLM may suggest width; generation stays deterministic",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Suggest a road width in meters. Output JSON {\"widthMeters\":4.0}.",
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
        var width = input.Get("widthMeters", 4.0f);
        var generateTexCoords = input.Get("generateTexCoords", true);
        var uvMetersPerRepeat = input.Get("uvMetersPerRepeat", 1.0f);
        var conform = input.Get("conformToTerrain", false);
        var terrainGrid = input.Get<ElevationGrid?>("terrainGrid", null);
        var treatNoData = input.Get("terrainTreatNoDataAsZero", true);

        var mesh = RoadMeshGenerator.GenerateRoads(
            features,
            origin,
            conform ? terrainGrid : null,
            new RoadMeshGenerationOptions
            {
                DefaultWidthMeters = width,
                GenerateTexCoords = generateTexCoords,
                UvMetersPerRepeat = uvMetersPerRepeat,
                ConformToTerrain = conform,
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
            throw new InvalidOperationException("Forced agentic failure (GeoVectorRoadsToMeshBrick).");
        }

        var width = input.Get("widthMeters", 4.0f);
        try
        {
            _ = await _llm.ExecuteLLMAsync(
                context.Provider,
                Implementations.Agentic!.LLMConfig.SystemPrompt,
                $"Current widthMeters={width}. Return JSON {{\"widthMeters\":4.0}}.",
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

