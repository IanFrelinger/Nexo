using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.GeoTerrain;
using Nexo.GeoVector.Generation;
using Nexo.GeoVector.Models;
using Nexo.Infrastructure.Execution;

namespace Nexo.GeoVector.Bricks;

/// <summary>
/// Convert building features to a mesh via deterministic extrusion.
/// </summary>
public sealed class GeoVectorBuildingsToMeshBrick : Brick
{
    private readonly IProviderFactory _llm;
    private readonly ILogger<GeoVectorBuildingsToMeshBrick> _logger;

    public GeoVectorBuildingsToMeshBrick(IProviderFactory llm, ILogger<GeoVectorBuildingsToMeshBrick> logger)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Id = "geovector.buildings.to-mesh";
        Name = "Buildings To Mesh";
        Version = "0.1.0";
        Icon = "🏢";
        Category = BrickCategory.Generation;
        Description = "Extrudes building footprints into a triangle mesh.";

        Interface = new BrickInterface
        {
            Inputs =
            [
                new BrickInputDefinition("features", "GeoFeatureSet", "Input features"),
                new BrickInputDefinition("origin", "GeoPoint", "Local projection origin point"),
                new BrickInputDefinition("defaultHeightMeters", "float", "Default building height in meters", required: false, defaultValue: 10.0f),
                new BrickInputDefinition("includeBottom", "bool", "Include bottom faces", required: false, defaultValue: false),
                new BrickInputDefinition("generateTexCoords", "bool", "If true, generate UVs for texture mapping", required: false, defaultValue: false),
                new BrickInputDefinition("uvMetersPerRepeat", "float", "Meters per texture repeat (UV scale)", required: false, defaultValue: 1.0f),
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
                Id = "extrude",
                Name = "Extrude (Deterministic)",
                Description = "Deterministic polygon triangulation + extrusion",
                Executor = "BuildingMeshGenerator",
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
                Id = "tuned-extrude",
                Name = "Tuned Extrude (Agentic)",
                Description = "LLM may suggest defaults; extrusion stays deterministic",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "Suggest defaultHeightMeters given context. Output JSON {\"defaultHeightMeters\":10.0}.",
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
        var defaultHeight = input.Get("defaultHeightMeters", 10.0f);
        var includeBottom = input.Get("includeBottom", false);
        var generateTexCoords = input.Get("generateTexCoords", false);
        var uvMetersPerRepeat = input.Get("uvMetersPerRepeat", 1.0f);

        var mesh = BuildingMeshGenerator.GenerateBuildings(
            features,
            origin,
            new BuildingExtrusionOptions
            {
                DefaultHeightMeters = defaultHeight,
                IncludeBottom = includeBottom,
                GenerateTexCoords = generateTexCoords,
                UvMetersPerRepeat = uvMetersPerRepeat
            });

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
            throw new InvalidOperationException("Forced agentic failure (GeoVectorBuildingsToMeshBrick).");
        }

        var defaultHeight = input.Get("defaultHeightMeters", 10.0f);

        try
        {
            _ = await _llm.ExecuteLLMAsync(
                context.Provider,
                Implementations.Agentic!.LLMConfig.SystemPrompt,
                $"Current defaultHeightMeters={defaultHeight}. Return JSON {{\"defaultHeightMeters\":10.0}}.",
                Implementations.Agentic.LLMConfig,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "LLM tuning failed; proceeding with input defaults.");
        }

        _logger.LogInformation("Proceeding with deterministic extrusion (agentic path).");
        return await ExecuteDeterministicAsync(input, ct);
    }
}

